﻿using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace GorillaPortraits.Tools
{
    public class AssetLoader
    {
        private static AssetBundle loadedBundle;
        private static readonly Dictionary<string, Object> loadedAssets = [];

        private static Task bundleLoadTask;

        public static async Task<T> LoadAsset<T>(string assetName) where T : Object
        {
            if (loadedAssets.TryGetValue(assetName, out Object asset) && asset is T) return (T)asset;

            if (loadedBundle is null)
            {
                bundleLoadTask ??= LoadAssetBundle();
                await bundleLoadTask;
            }

            TaskCompletionSource<T> completionSource = new();

            AssetBundleRequest request = loadedBundle.LoadAssetAsync<T>(assetName);
            request.completed += _ => completionSource.SetResult(request.asset is Object asset ? (T)asset : null);

            T result = await completionSource.Task;
            loadedAssets.Add(assetName, result);
            return result;
        }

        private static async Task LoadAssetBundle()
        {
            TaskCompletionSource<AssetBundle> completionSource = new();

            Stream stream = typeof(Plugin).Assembly.GetManifestResourceStream("GorillaPortraits.Content.shelfbundle");

            AssetBundleCreateRequest request = AssetBundle.LoadFromStreamAsync(stream);
            request.completed += _ => completionSource.SetResult(request.assetBundle);

            loadedBundle = await completionSource.Task;
        }
    }
}