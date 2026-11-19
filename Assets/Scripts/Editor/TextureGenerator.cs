// TextureGenerator: paints textures pixel by pixel in code and saves them as PNG files in
// Assets/Generated/Textures, then sets the import settings (wrap, transparency, normal map...).
// Phase 1 only needs the soft particle (for the torch flame); Phase 2 adds stone, moss, glyphs etc.
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LastTorch.EditorTools
{
    public static class TextureGenerator
    {
        public const string Folder = "Assets/Generated/Textures";

        /// <summary>White radial blob with soft edges, used for fire, embers and dust particles.</summary>
        public static Texture2D SoftParticle(int size = 128)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            float half = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - half) / half, dy = (y - half) / half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a * (3f - 2f * a); // smoothstep falloff
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return SavePng(tex, "soft_particle", clamp: true);
        }

        /// <summary>Writes the texture to Folder/name.png, imports it with the right settings and returns the asset.</summary>
        public static Texture2D SavePng(Texture2D tex, string name, bool clamp, bool isNormalMap = false)
        {
            EditorUtil.EnsureFolder(Folder);
            string assetPath = $"{Folder}/{name}.png";
            string fullPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), assetPath);
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            importer.textureType = isNormalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !isNormalMap;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = !isNormalMap;
            importer.wrapMode = clamp ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }
    }
}
