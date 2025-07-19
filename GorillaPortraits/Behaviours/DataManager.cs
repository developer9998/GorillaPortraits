using GorillaNetworking;
using GorillaPortraits.Behaviours.Objects;
using GorillaPortraits.Models;
using GorillaPortraits.Models.StateMachine;
using GorillaPortraits.Tools;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace GorillaPortraits.Behaviours
{
    public class DataManager : Singleton<DataManager>
    {
        /// <summary>
        /// Called when portrait data is initially loaded
        /// </summary>
        public static event Action OnDataLoaded;

        private readonly Dictionary<Portrait, PhotoData> dataCache = [];

        private Dictionary<string, List<PhotoData>> perShelfData = null;

        private List<Photo> initialPhotoList = null;

        private string modDirectory, dataDirectory;

        private JsonSerializerSettings serializeSettings, deserializeSettings;

        public override void Initialize()
        {
            base.Initialize();

            modDirectory = Path.GetDirectoryName(typeof(Plugin).Assembly.Location);
            dataDirectory = Path.Combine(modDirectory, "Data");

            serializeSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                CheckAdditionalContent = true,
                Formatting = Formatting.Indented
            };
            serializeSettings.Converters.Add(new Vector3Converter());

            deserializeSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            };
            deserializeSettings.Converters.Add(new Vector3Converter());

            PhotoManager.OnPhotosRecieved += OnPhotosRecieved;
            ShelfManager.OnShelvesReady += OnShelvesLoaded;
        }

        public void OnPhotosRecieved(bool isInitialList, List<Photo> photos)
        {
            if (isInitialList && initialPhotoList is null)
            {
                initialPhotoList = photos;
                Logging.Info($"Data recieved {photos.Count} photos");
            }
        }

        public async void OnShelvesLoaded(Shelf[] shelves)
        {
            if (perShelfData is not null) return;

            perShelfData = shelves.ToDictionary(shelf => shelf.displayName, shelf => Enumerable.Empty<PhotoData>().ToList());

            foreach (Shelf shelf in shelves)
            {
                string displayName = shelf.displayName;

                if (!Directory.Exists(dataDirectory))
                {
                    Logging.Info("Creating data directory - skipping load");
                    Directory.CreateDirectory(dataDirectory);
                    break;
                }

                string dataEntry = Path.Combine(dataDirectory, $"{displayName}.json");
                if (!File.Exists(dataEntry))
                {
                    Logging.Warning($"Data file for {displayName} not found");
                    continue;
                }

                List<PhotoData> photoDataCollection = JsonConvert.DeserializeObject<List<PhotoData>>(await File.ReadAllTextAsync(dataEntry), deserializeSettings);
                if (photoDataCollection is null)
                {
                    Logging.Warning($"Data not identified for {displayName} - removing file");
                    File.Delete(dataEntry);
                    continue;
                }

                perShelfData[displayName] = photoDataCollection;

                GameObject prefab = shelf.basket.portraitPrefab;

                for (int i = 0; i < photoDataCollection.Count; i++)
                {
                    PhotoData data = photoDataCollection[i];

                    if (data is null) continue;

                    if (File.Exists(data.Path))
                    {
                        Logging.Warning(data.Path);
                        data.Path = data.Path.RemoveStart(modDirectory).TrimStart('/').TrimStart('\\');
                    }

                    Logging.Info(data.Path);

                    if (initialPhotoList.Find(photo => photo.RelativePath == data.Path) is Photo photo)
                    {
                        Logging.Info($"Image found: {photo}");

                        Vector3 worldPosition = shelf.transform.TransformPoint(data.Position);

                        if (shelf.GetPlacementArea(worldPosition, 0.2f) is BoxCollider sensor && sensor)
                        {
                            Logging.Info($"Sensor found: {sensor.name}");

                            GameObject gameObject = Instantiate(prefab, null);
                            Transform transform = gameObject.transform;
                            transform.SetParent(shelf.transform);
                            transform.localPosition = data.Position;
                            transform.localEulerAngles = data.Rotation;

                            Portrait portrait = gameObject.GetComponent<Portrait>();
                            portrait.currentPhoto = photo;
                            portrait.shelf = shelf;
                            portrait.portraitState.SwitchState(new PortraitState_OnDisplay(portrait, sensor));
                            portrait.portraitMenu.SwitchState(portrait.portraitMainDisplay);

                            dataCache.AddOrUpdate(portrait, data);
                            continue;
                        }

                        Logging.Warning("Sensor not found");

                        GameObject primative = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        primative.GetComponent<Renderer>().material = new Material(Shader.Find("GorillaTag/UberShader"));
                        primative.GetComponent<Collider>().enabled = false;
                        primative.transform.localScale = Vector3.one * 0.2f;
                        primative.transform.SetParent(shelf.transform);
                        primative.transform.localPosition = data.Position;

                        continue;
                    }

                    Logging.Warning("Image not found at path");

                    photoDataCollection.RemoveAt(i);
                    i--;
                }
            }

            ShelfManager.OnPortraitAdd += OnPortraitAdd;
            ShelfManager.OnPortraitRemove += OnPortraitRemove;
            ShelfManager.OnPortraitReturned += OnPrePortraitReserve;

            SaveData();
            OnDataLoaded?.Invoke();
        }

        public void OnPortraitAdd(Portrait portrait)
        {
            if (!dataCache.TryGetValue(portrait, out PhotoData data))
            {
                data = new()
                {
                    Path = portrait.currentPhoto.RelativePath,
                    Position = portrait.transform.localPosition,
                    Rotation = portrait.transform.localEulerAngles
                };
                dataCache.Add(portrait, data);
            }

            if (perShelfData.TryGetValue(portrait.shelf.displayName, out List<PhotoData> list) && !list.Contains(data))
            {
                list.Add(data);
                SaveData();
            }
        }

        public void OnPortraitRemove(Portrait portrait)
        {
            if (dataCache.TryGetValue(portrait, out PhotoData data) && perShelfData.TryGetValue(portrait.shelf.displayName, out List<PhotoData> list) && list.Contains(data))
            {
                list.Remove(data);
                dataCache.Remove(portrait);
                SaveData();
            }
        }

        public void OnPrePortraitReserve(Portrait portrait)
        {
            if (dataCache.TryGetValue(portrait, out PhotoData data))
            {
                dataCache.Remove(portrait);

                bool doSave = false;

                for (int i = 0; i < perShelfData.Count; i++)
                {
                    List<PhotoData> list = perShelfData.Values.ElementAtOrDefault(i);
                    if (list is not null && list.Contains(data))
                    {
                        doSave = true;
                        list.Remove(data);
                    }
                }

                if (doSave) SaveData();
            }
        }

        public void SaveData()
        {
            if (perShelfData is null || perShelfData.Count == 0)
            {
                Logging.Warning("perShelfData is null/empty");
                return;
            }

            for (int i = 0; i < perShelfData.Count; i++)
            {
                string displayName = perShelfData.Keys.ElementAtOrDefault(i);
                SaveData(displayName);
            }
        }

        public void SaveData(string displayName)
        {
            if (displayName is null || !perShelfData.TryGetValue(displayName, out List<PhotoData> list))
            {
                Logging.Warning($"perShelfData for {(displayName is null ? "NULL!!" : displayName)} is NULL!!");
                return;
            }

            Logging.Info(displayName);

            if (!Directory.Exists(dataDirectory)) Directory.CreateDirectory(dataDirectory);

            string serialization = JsonConvert.SerializeObject(list, serializeSettings);
            Logging.Info(serialization);

            File.WriteAllText(Path.Combine(dataDirectory, $"{displayName}.json"), serialization);
        }

        [Serializable]
        public class PhotoData
        {
            public string Path;

            public Vector3 Position;

            public Vector3 Rotation;
        }
    }
}
