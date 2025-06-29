using GorillaNetworking;
using GorillaPortraits.Models;
using GorillaPortraits.Tools;
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

#nullable enable
        public Photo? lastSelectedPhoto = null;
#nullable disable

        private readonly List<PhotoCollection> photoCollections = [];

        private readonly Dictionary<PhotoCollection, List<Photo>> photoListCache = [];

        private readonly ConcurrentQueue<Action> unityThreadQueue = [];

        private readonly string[] filters = [".png", ".jpg", ".jpeg", ".jfif"];

        private string modDirectory, photoDirectory;

        public override void Initialize()
        {
            base.Initialize();

            modDirectory = Path.GetDirectoryName(typeof(Plugin).Assembly.Location);

            photoDirectory = Path.Combine(modDirectory, "Pictures");
            if (!Directory.Exists(photoDirectory)) Directory.CreateDirectory(photoDirectory);

            photoCollections.Add(new PhotoCollection(photoDirectory));

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
            await Task.WhenAll(photoCollections.Select(collection => collection.LoadPhotos()));
            photoCollections.ForEach(collection => photoListCache.AddOrUpdate(collection, collection.Photos));

            if (photoListCache.Sum(cache => cache.Key.Photos.Count) == 0)
            {
                Logging.Warning("No photos loaded - downloading zip from GitHub");
                await DownloadZip("https://github.com/developer9998/GorillaPortraits/raw/refs/heads/main/Pictures.zip", Path.Combine(photoDirectory, "Pictures.zip"), photoDirectory);
                LoadPhotos(callback);
                return;
            }

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
            Logging.Info($"Downloading zip file from {url}");

            using UnityWebRequest request = new(url)
            {
                downloadHandler = new DownloadHandlerFile(zipPath)
            };

            TaskCompletionSource<UnityWebRequest> completionSource = new();

            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            operation.completed += _ => completionSource.SetResult(operation.webRequest);

            await completionSource.Task;

            if (request.result > UnityWebRequest.Result.Success)
            {
                Logging.Error($"Failed to download zip with result {request.result}: {request.error}");
                return;
            }

            Logging.Info($"Extracting zip file to {extractPath}");

            ZipFile.ExtractToDirectory(zipPath, extractPath);
            File.Delete(zipPath);
        }

        public class PhotoCollection(string path)
        {
            public DirectoryInfo Directory = new(path);

            public List<Photo> Photos = [];

            private readonly Dictionary<string, (Photo photo, DateTime writeTime)> photoCache = [];

            public async Task LoadPhotos()
            {
                FileInfo[] files = Directory.GetFiles("*.*", SearchOption.AllDirectories);

                Photos.Clear();

                foreach (FileInfo file in files)
                {
                    string extension = file.Extension;

                    if (extension is null || extension.Length == 0 || !Instance.filters.Contains(extension))
                        continue;

                    string path = file.FullName;
                    DateTime writeTime = file.LastWriteTime;

                    if (photoCache.TryGetValue(path, out (Photo photo, DateTime writeTime) cache) && cache.writeTime == writeTime)
                    {
                        Photos.Add(cache.photo);
                        continue;
                    }

                    byte[] bytes = await File.ReadAllBytesAsync(path);

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
                        string relativePath = path.RemoveStart(Instance.modDirectory).TrimStart('/').TrimStart('\\');
                        Photo photo = new(relativePath, texture);
                        photoCache.AddOrUpdate(path, (photo, writeTime));
                        Photos.Add(photo);
                        continue;
                    }

                    Logging.Warning("Texture not could be loaded");
                    Logging.Warning(path);
                }
            }
        }
    }
}
