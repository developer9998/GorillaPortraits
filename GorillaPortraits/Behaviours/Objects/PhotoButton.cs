using UnityEngine.UI;

#if PLUGIN
using GorillaPortraits.Models;
#endif

namespace GorillaPortraits.Behaviours.Objects
{
    public class PhotoButton : PushButton
    {

        public Image backgroundImage, photoImage;

#if PLUGIN

        public Photo Photo { get; private set; }

        public void SetPhoto(Photo photo)
        {
            if (photo is null || photo.Sprite is null)
            {
                Photo = null;
                gameObject.SetActive(false);
                return;
            }

            Photo = photo;
            gameObject.SetActive(true);
            backgroundImage.color = photo.BackgroundColour;
            photoImage.sprite = photo.Sprite;
        }
#endif
    }
}