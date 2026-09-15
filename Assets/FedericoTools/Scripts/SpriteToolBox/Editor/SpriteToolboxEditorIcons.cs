using System.Collections.Generic;
using UnityEngine;

namespace FedericoTools.SpriteToolBox.Editor
{
    internal static class SpriteToolboxEditorIcons
    {
        private const string ResourceKeyPrefix = "SpriteToolbox_";
        private static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();

        public static Texture2D Get(string name)
        {
            if (Cache.TryGetValue(name, out Texture2D icon) && icon != null)
            {
                return icon;
            }

            string resourceKey = ResourceKeyPrefix + name;
            Texture2D loadedIcon = Resources.Load<Texture2D>(resourceKey);
            if (loadedIcon == null)
            {
                Sprite loadedSprite = Resources.Load<Sprite>(resourceKey);
                loadedIcon = loadedSprite == null ? null : loadedSprite.texture;
            }
            Cache[name] = loadedIcon;
            return loadedIcon;
        }
    }
}
