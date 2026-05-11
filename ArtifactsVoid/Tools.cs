using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace ArtifactsOfTheVoid
{
    public static class Tools
    {
        public static byte[] ExtractEmbeddedResource(String filePath)
        {
            filePath = filePath.Replace("/", ".");
            filePath = filePath.Replace("\\", ".");
            var baseAssembly = Assembly.GetCallingAssembly();
            using (Stream resFilestream = baseAssembly.GetManifestResourceStream(filePath))
            {
                if (resFilestream == null)
                {
                    return null;
                }
                byte[] ba = new byte[resFilestream.Length];
                resFilestream.Read(ba, 0, ba.Length);
                return ba;
            }
        }
        public static Texture2D GetTextureFromResource(string resourceName)
        {
            string file = resourceName;
            byte[] bytes = ExtractEmbeddedResource(file);
            if (bytes == null)
            {
                //Tools.PrintError("No bytes found in " + file);
                return null;
            }

            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            ImageConversion.LoadImage(texture, bytes);
            //FlipTexture(ref texture);

            texture.filterMode = FilterMode.Point;

            string name = file.Substring(0, file.LastIndexOf('.'));
            if (name.LastIndexOf('.') >= 0)
            {
                name = name.Substring(name.LastIndexOf('.') + 1);
            }
            texture.name = name;

            return texture;
        }
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                var rng = new System.Random();
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
