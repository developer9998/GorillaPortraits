using GorillaPortraits.Behaviours;
using GorillaPortraits.Behaviours.Objects;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GorillaPortraits.Models.StateMachine
{
    public class PortraitMenu_Selection(Portrait portrait, int pageCapacity, List<Photo> photos) : PortraitMenu_Base(portrait)
    {
        private int PageCount => Mathf.CeilToInt(photos.Count / (float)pageCapacity);

        protected List<Photo> photos = photos;

        public override void Enter()
        {
            base.Enter();

            portrait.applyPanel.SetActive(true);

            portrait.nextPage.OnPressed += HandleSelectNext;
            portrait.prevPage.OnPressed += HandleSelectPrevious;
            portrait.selection.ForEach(button => button.OnPressed += HandleSelectPortrait);

            RefreshMenu();
        }

        public override void Initialize()
        {
            base.Initialize();

            int indexOf = photos.FindIndex(photo => photo == portrait.currentPhoto);
            portrait.currentPage = indexOf != -1 ? Mathf.FloorToInt(indexOf / (float)pageCapacity) : 0;
        }

        public override void Resume()
        {
            base.Resume();

            portrait.currentPage = Mathf.Clamp(portrait.currentPage, 0, PageCount - 1);
        }

        public override void Exit()
        {
            base.Exit();

            portrait.applyPanel.SetActive(false);

            portrait.nextPage.OnPressed -= HandleSelectNext;
            portrait.prevPage.OnPressed -= HandleSelectPrevious;
            portrait.selection.ForEach(button => button.OnPressed -= HandleSelectPortrait);
        }

        public void HandleSelectPortrait(PushButton button, bool isLeftHand)
        {
            if (button is PhotoButton photoButton)
            {
                PhotoManager.Instance.lastSelectedPhoto = photoButton.Photo;
                portrait.currentPhoto = photoButton.Photo;
                RefreshMenu();
            }
        }

        public void HandleSelectNext(PushButton button, bool isLeftHand)
        {
            portrait.currentPage = (portrait.currentPage + 1) % PageCount;
            RefreshMenu();
        }

        public void HandleSelectPrevious(PushButton button, bool isLeftHand)
        {
            portrait.currentPage = (portrait.currentPage <= 0 ? PageCount : portrait.currentPage) - 1;
            RefreshMenu();
        }

        public void RefreshMenu()
        {
            int offset = portrait.currentPage * pageCapacity;

            for (int i = 0; i < pageCapacity; i++)
            {
                if (portrait.selection.ElementAtOrDefault(i) is PhotoButton button && button)
                {
                    Photo photo = photos.ElementAtOrDefault(i + offset) ?? null;
                    button.SetPhoto(photo);
                    button.Activated = portrait.currentPhoto == photo;
                    button.UpdateAppearence();
                }
            }

            portrait.pageText.text = string.Format("{0}/{1}", portrait.currentPage + 1, PageCount);
        }
    }
}
