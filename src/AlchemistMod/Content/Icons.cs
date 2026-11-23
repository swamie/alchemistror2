using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace AlchemistMod.Content
{
    /// <summary>
    /// Loads the icon PNGs embedded in the dll (src/AlchemistMod/Assets/Icons, made by art/make_icons.py).
    /// </summary>
    internal static class Icons
    {
        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static Sprite Get(string name)
        {
            if (cache.TryGetValue(name, out Sprite sprite)) return sprite;

            Assembly assembly = typeof(Icons).Assembly;
            using (Stream stream = assembly.GetManifestResourceStream("Icons." + name + ".png"))
            {
                if (stream == null)
                {
                    AlchemistPlugin.Log.LogWarning($"Missing embedded icon {name}");
                    cache[name] = null;
                    return null;
                }

                var bytes = new byte[stream.Length];
                int read = 0;
                while (read < bytes.Length) read += stream.Read(bytes, read, bytes.Length - read);

                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                texture.LoadImage(bytes);
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Bilinear;
                texture.name = "texAlchemist_" + name;

                sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                sprite.name = name;
                cache[name] = sprite;
                return sprite;
            }
        }
    }
}
