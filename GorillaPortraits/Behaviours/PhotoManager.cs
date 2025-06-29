using GorillaNetworking;
using GorillaPortraits.Models;
using GorillaPortraits.Tools;
using GorillaPortraits.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace GorillaPortraits.Behaviours
{
    internal class PhotoManager : Singleton<PhotoManager>
    {
        /// <summary>
        /// Called when photos have been defined
        /// </summary>
        public static Action<bool, List<Photo>> OnPhotosRecieved;

        private readonly List<PhotoCollection> photoCollections = [];

        private readonly Dictionary<PhotoCollection, List<Photo>> photoListCache = [];

        private readonly ConcurrentQueue<Action> unityThreadQueue = [];

        private readonly string[] filters = [".png", ".jpg", ".jpeg", ".jfif"];

        public override async void Initialize()
        {
            base.Initialize();

            string modPath = Path.GetDirectoryName(typeof(Plugin).Assembly.Location);

            string photosPath = Path.Combine(modPath, "Pictures");

            if (!Directory.Exists(photosPath))
            {
                Directory.CreateDirectory(photosPath);
                await DownloadZip("https://github.com/developer9998/GorillaPortraits/raw/refs/heads/main/Pictures.zip", Path.Combine(photosPath, "Pictures.zip"), photosPath);
            }

            photoCollections.Add(new PhotoCollection(photosPath));

            /*
            string nativePicturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            if (nativePicturesPath != null && nativePicturesPath.Length > 0) photoCollections.Add(new(nativePicturesPath));
            */

            LoadPhotos(() =>
            {
                OnPhotosRecieved?.Invoke(true, GetPhotos());
            });
        }

        public List<Photo> GetPhotos()
        {
            List<Photo> totalPhotos = [];

            foreach (PhotoCollection collection in photoCollections)
            {
                if (photoListCache.TryGetValue(collection, out List<Photo> collectedPhotos))
                {
                    totalPhotos.AddRange(collectedPhotos);
                }
            }

            return totalPhotos;
        }

        public void ReloadPhotos() => LoadPhotos(delegate ()
        {
            OnPhotosRecieved?.Invoke(false, GetPhotos());
        });

        private async void LoadPhotos(Action callback)
        {
            IEnumerable<Task> tasks = photoCollections.Select(collection => collection.LoadPhotos());
            await Task.WhenAll(tasks: tasks);
            photoCollections.ForEach(collection => photoListCache.AddOrUpdate(collection, collection.Photos));
            callback?.Invoke();
        }

        public void Update()
        {
            while (unityThreadQueue.TryDequeue(out Action action))
            {
                action?.Invoke();
            }
        }

        public async Task DownloadZip(string url, string zipPath, string extractPath)
        {
            Logging.Info($"Downloading zip file at {url}");

            UnityWebRequest request = new(url)
            {
                downloadHandler = new DownloadHandlerFile(zipPath)
            };

            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            await TaskYieldUtils.Yield(operation);

            if (request.result != UnityWebRequest.Result.Success)
            {
                Logging.Error($"Failed to download zip: {request.error}");
                return;
            }
            request.Dispose();

            Logging.Info($"Extracting zip file to {extractPath}");

            ZipFile.ExtractToDirectory(zipPath, extractPath);
            File.Delete(zipPath);
        }

        public class PhotoCollection(string directoryPath)
        {
            public DirectoryInfo Directory = new(directoryPath);

            public List<Photo> Photos = [];

            private readonly Dictionary<string, (Photo photo, DateTime dateTime)> photoCache = [];

            public async Task LoadPhotos()
            {
                FileInfo[] files = Directory.GetFiles("*.*", SearchOption.AllDirectories);

                Photos.Clear();

                foreach (FileInfo file in files)
                {
                    string extension = file.Extension;

                    if (extension is null || extension.Length == 0 || !Instance.filters.Contains(extension))
                        continue;

                    string filePath = file.FullName;
                    DateTime dateTime = file.LastWriteTime;

                    if (photoCache.TryGetValue(filePath, out (Photo photo, DateTime dateTime) cache) && cache.dateTime == dateTime)
                    {
                        Photos.Add(cache.photo);
                        continue;
                    }

                    byte[] bytes = await File.ReadAllBytesAsync(filePath);

                    TaskCompletionSource<Texture2D> completionSource = new();

                    Instance.unityThreadQueue.Enqueue(delegate ()
                    {
                        Texture2D texture = new(2, 2)
                        {
                            name = file.Name,
                            filterMode = FilterMode.Point
                        };

                        bool hasLoadedImage = texture.LoadImage(bytes);
                        if (hasLoadedImage)
                        {
                            completionSource.SetResult(texture);
                            return;
                        }
                        Destroy(texture);
                        completionSource.SetResult(null);
                    });

                    Texture2D texture = await completionSource.Task;

                    if (texture)
                    {
                        Photo photo = new(file, texture);
                        photoCache.AddOrUpdate(filePath, (photo, dateTime));
                        Photos.Add(photo);
                        continue;
                    }

                    Logging.Warning("Texture not could be loaded");
                    Logging.Warning(filePath);
                }
            }
        }
    }
}
