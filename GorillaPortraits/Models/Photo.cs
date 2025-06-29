using System;
using UnityEngine;

namespace GorillaPortraits.Models
{
    [Serializable]
    public class Photo
    {
        public Texture2D Texture => Sprite.texture;

        public Sprite Sprite;

        public string RelativePath;

        public Color Colour = Color.white;

        public Color BackgroundColour = Color.grey;

        public Photo(string relativePath, Texture2D texture)
        {
            RelativePath = relativePath;

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
            sprite.name = texture.name;
            Sprite = sprite;
        }

        public override string ToString() => (Sprite is not null && Sprite) ? Sprite.name : "bad photo";
    }
}