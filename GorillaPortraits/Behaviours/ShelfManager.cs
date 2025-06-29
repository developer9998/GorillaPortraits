using GorillaPortraits.Behaviours.Objects;
using GorillaPortraits.Models;
using GorillaPortraits.Models.StateMachine;
using GorillaPortraits.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace GorillaPortraits.Behaviours
{
    internal class ShelfManager : Singleton<ShelfManager>
    {
        /// <summary>
        /// Called when the portrait shelves are ready, so loaded and instantiated
        /// </summary>
        public static Action<Shelf[]> OnShelvesReady;

        /// <summary>
        /// Called when a portrait is placed on its shelf
        /// </summary>
        public static Action<Portrait> OnPortraitAdd;

        /// <summary>
        /// Called when a portrait is taken off its shelf
        /// </summary>
        public static Action<Portrait> OnPortraitRemove;

        /// <summary>
        /// Called when a portrait is returned to its basket
        /// </summary>
        public static Action<Portrait> OnPortraitReturned;

        private GameObject shelves = null;

        public override void Initialize()
        {
            base.Initialize();

            PhotoManager.OnPhotosRecieved += OnPhotosRecieved;
        }

        public async void OnPhotosRecieved(bool isInitialList, List<Photo> photos)
        {
            if (isInitialList && (shelves is null || !shelves))
            {
                PhotoManager.OnPhotosRecieved -= OnPhotosRecieved;

                shelves = Instantiate(await AssetLoader.LoadAsset<GameObject>("Shelves"));
                DontDestroyOnLoad(shelves);
                shelves.transform.SetParent(transform);
                await Task.Yield();
                OnShelvesReady?.Invoke(shelves.GetComponentsInChildren<Shelf>());
            }
        }

        public void NewPortrait(GameObject prefab, Shelf shelf, bool isLeftHand)
        {
            GameObject gameObject = Instantiate(prefab, null);
            Portrait portrait = gameObject.GetComponent<Portrait>();
            portrait.currentPhoto = PhotoManager.Instance.GetPhotos().First();
            portrait.shelf = shelf;
            portrait.wasSwappedLeft = !isLeftHand;
            portrait.portraitState.SwitchState(new PortraitState_InHand(portrait, isLeftHand, true));
        }
    }
}
