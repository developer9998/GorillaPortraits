using System;
using System.IO;
using UnityEngine;

namespace GorillaPortraits.Models
{
    [Serializable]
    public class Photo
    {
        public Texture2D Texture => Sprite.texture;

        public Sprite Sprite;

        public FileInfo File;

        public Color Colour = Color.white;

        public Color BackgroundColour = Color.grey;

        public Photo(FileInfo info, Sprite sprite)
        {
            File = info;
            Sprite = sprite;
        }

        public Photo(FileInfo info, Texture2D texture)
        {
            File = info;
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
            sprite.name = texture.name;
            Sprite = sprite;
        }

        public override string ToString() => (Sprite is not null && Sprite) ? Sprite.name : "bad photo";
    }
}