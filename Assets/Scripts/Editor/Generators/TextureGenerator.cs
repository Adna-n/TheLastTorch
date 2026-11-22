// TextureGenerator: paints every texture pixel by pixel in code with seeded, tileable noise
// (fBm value noise + Voronoi cells) and saves them as PNG files in Assets/Generated/Textures:
// stone wall/floor (+ normal maps made from a height map), moss, key glyphs, random wall glyphs,
// clay, wood, a soft particle, and UI sprites (vignette, rounded bar, heart). Fixed seeds = same result every run.
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LastTorch.EditorTools
{
    public static class TextureGenerator
    {
        public const string Folder = "Assets/Generated/Textures";
        const int Size = 512;
        const int GlyphSize = 256;

        public static void GenerateAll()
        {
            StoneWall();
            StoneFloor();
            Moss();
            GlyphSun();
            GlyphMoon();
            GlyphSerpent();
            for (int i = 1; i <= 4; i++) RandomGlyph(i);
            Clay();
            Wood();
            SoftParticle();
            UiVignette();
            UiBar();
            UiHeart();
        }

        public static string PathOf(string name) => $"{Folder}/{AssetCategories.Texture(name)}/{name}.png";
        public static Texture2D Load(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>(PathOf(name));
        public static Sprite LoadSprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(PathOf(name));

        // ------------------------------------------------------------------ noise

        /// <summary>Seeded, tileable noise. u,v are in 0..1 and wrap around.</summary>
        public class TileNoise
        {
            readonly uint seed;
            public TileNoise(int seed) { this.seed = (uint)seed; }

            public float Hash(int x, int y)
            {
                unchecked
                {
                    uint h = seed * 0x9E3779B1u;
                    h ^= (uint)x * 0x85EBCA6Bu;
                    h = (h << 13) | (h >> 19);
                    h ^= (uint)y * 0xC2B2AE35u;
                    h *= 0x27D4EB2Fu;
                    h ^= h >> 15;
                    h *= 0x165667B1u;
                    h ^= h >> 13;
                    return (h & 0xFFFFFF) / 16777215f;
                }
            }

            public static int Wrap(int v, int p) { int m = v % p; return m < 0 ? m + p : m; }

            public float Value(float x, float y, int px, int py)
            {
                int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
                float fx = x - x0, fy = y - y0;
                fx = fx * fx * (3f - 2f * fx);
                fy = fy * fy * (3f - 2f * fy);
                int xa = Wrap(x0, px), xb = Wrap(x0 + 1, px), ya = Wrap(y0, py), yb = Wrap(y0 + 1, py);
                float a = Hash(xa, ya), b = Hash(xb, ya), c = Hash(xa, yb), d = Hash(xb, yb);
                return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
            }

            /// <summary>Fractal noise (several octaves), 0..1.</summary>
            public float Fbm(float u, float v, int periodX, int periodY, int octaves, float gain = 0.5f)
            {
                float sum = 0f, amp = 1f, norm = 0f;
                int px = periodX, py = periodY;
                for (int o = 0; o < octaves; o++)
                {
                    sum += amp * Value(u * px, v * py, px, py);
                    norm += amp;
                    amp *= gain;
                    px *= 2; py *= 2;
                }
                return sum / norm;
            }

            public float Fbm(float u, float v, int period, int octaves, float gain = 0.5f) => Fbm(u, v, period, period, octaves, gain);

            /// <summary>Tileable Voronoi with n cells per side. f1/f2 = nearest/second-nearest distance in cell units.</summary>
            public void Voronoi(float u, float v, int n, out float f1, out float f2, out int id)
            {
                float x = u * n, y = v * n;
                int cx = Mathf.FloorToInt(x), cy = Mathf.FloorToInt(y);
                f1 = f2 = 99f; id = 0;
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int gx = cx + dx, gy = cy + dy;
                        int wx = Wrap(gx, n), wy = Wrap(gy, n);
                        float fx = gx + 0.15f + 0.7f * Hash(wx, wy + 1000);
                        float fy = gy + 0.15f + 0.7f * Hash(wx + 1000, wy);
                        float d = Mathf.Sqrt((x - fx) * (x - fx) + (y - fy) * (y - fy));
                        if (d < f1) { f2 = f1; f1 = d; id = wx + wy * n; }
                        else if (d < f2) f2 = d;
                    }
                }
            }
        }

        static float Smooth(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        // ------------------------------------------------------------------ stone

        public static void StoneWall()
        {
            var tone = new TileNoise(101);
            var warp = new TileNoise(202);
            var crack = new TileNoise(303);
            Color colA = EditorUtil.Hex("4F5D50"), colB = EditorUtil.Hex("6E7867");
            Color mortar = EditorUtil.Hex("2B302A"), dark = EditorUtil.Hex("1A1E19");
            const int rows = 4, cols = 2;
            const float brickH = Size / (float)rows, brickW = Size / (float)cols;
            const float mortarW = 5f, bevel = 12f;

            var pixels = new Color[Size * Size];
            var height = new float[Size * Size];

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float u = x / (float)Size, v = y / (float)Size;
                    float wx = x + (warp.Fbm(u, v, 8, 3) - 0.5f) * 14f;
                    float wy = y + (warp.Fbm(u + 0.37f, v + 0.11f, 8, 3) - 0.5f) * 10f;

                    int row = Mathf.FloorToInt(wy / brickH);
                    float offset = TileNoise.Wrap(row, 2) == 1 ? brickW * 0.5f : 0f;
                    float bxF = wx + offset;
                    int col = Mathf.FloorToInt(bxF / brickW);
                    float bx = bxF - col * brickW, by = wy - row * brickH;
                    float edge = Mathf.Min(Mathf.Min(bx, brickW - bx), Mathf.Min(by, brickH - by));
                    int brickId = TileNoise.Wrap(col, cols) + TileNoise.Wrap(row, rows) * cols;

                    float brickTone = tone.Hash(brickId, 7);
                    float n = tone.Fbm(u, v, 16, 5);
                    float fine = tone.Fbm(u, v, 64, 3);

                    Color c = Color.Lerp(colA, colB, Mathf.Clamp01(n * 0.9f + brickTone * 0.5f - 0.2f));
                    c *= 0.85f + fine * 0.3f;
                    float h;

                    if (edge < mortarW)
                    {
                        c = Color.Lerp(mortar, dark, fine);
                        h = 0.1f * fine;
                    }
                    else
                    {
                        float t = Smooth(0f, 1f, (edge - mortarW) / bevel);
                        h = 0.6f + 0.4f * t + (n - 0.5f) * 0.3f;
                        c *= Mathf.Lerp(0.7f, 1f, t);

                        crack.Voronoi(u, v, 6, out float f1, out float f2, out _);
                        float line = f2 - f1;
                        const float crackWidth = 0.025f;
                        if (line < crackWidth && crack.Fbm(u, v, 4, 2) > 0.64f)
                        {
                            float k = 1f - line / crackWidth;
                            c = Color.Lerp(c, dark, k * 0.8f);
                            h -= 0.4f * k;
                        }
                    }

                    c.a = 1f;
                    pixels[y * Size + x] = c;
                    height[y * Size + x] = h;
                }
            }

            SavePixels("stone_wall", pixels, Size, false);
            SavePixels("stone_wall_normal", NormalFromHeight(height, Size, 3f), Size, false, isNormalMap: true);
        }

        public static void StoneFloor()
        {
            var cells = new TileNoise(404);
            var warp = new TileNoise(505);
            var detail = new TileNoise(606);
            Color stoneA = EditorUtil.Hex("6B675C"), stoneB = EditorUtil.Hex("55534C");
            Color grout = EditorUtil.Hex("2A2824"), sand = EditorUtil.Hex("8C7B5A");
            const float groutW = 0.06f, bevel = 0.09f;

            var pixels = new Color[Size * Size];
            var height = new float[Size * Size];

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float u = x / (float)Size, v = y / (float)Size;
                    float wu = u + (warp.Fbm(u, v, 6, 3) - 0.5f) * 0.04f;
                    float wv = v + (warp.Fbm(u + 0.5f, v + 0.5f, 6, 3) - 0.5f) * 0.04f;
                    cells.Voronoi(wu, wv, 5, out float f1, out float f2, out int id);
                    float edge = f2 - f1;

                    float n = detail.Fbm(u, v, 16, 4);
                    float fine = detail.Fbm(u, v, 64, 3);
                    float dirt = detail.Fbm(u + 0.3f, v + 0.7f, 4, 4);

                    Color c;
                    float h;
                    if (edge < groutW)
                    {
                        c = Color.Lerp(grout, sand * 0.6f, fine * 0.5f);
                        h = 0.05f * fine;
                    }
                    else
                    {
                        float t = Smooth(0f, 1f, (edge - groutW) / bevel);
                        c = Color.Lerp(stoneA, stoneB, cells.Hash(id, 3)) * (0.85f + 0.3f * fine);
                        c *= Mathf.Lerp(0.75f, 1f, t);
                        h = 0.5f + 0.5f * t + (n - 0.5f) * 0.25f;
                    }

                    float dirtAmount = Smooth(0.58f, 0.74f, dirt) * 0.7f;
                    c = Color.Lerp(c, sand * (0.8f + 0.4f * fine), dirtAmount);
                    h = Mathf.Lerp(h, 0.45f + fine * 0.1f, dirtAmount * 0.6f);

                    c.a = 1f;
                    pixels[y * Size + x] = c;
                    height[y * Size + x] = h;
                }
            }

            SavePixels("stone_floor", pixels, Size, false);
            SavePixels("stone_floor_normal", NormalFromHeight(height, Size, 2.5f), Size, false, isNormalMap: true);
        }

        static Color[] NormalFromHeight(float[] h, int size, float strength)
        {
            var result = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float l = h[y * size + TileNoise.Wrap(x - 1, size)];
                    float r = h[y * size + TileNoise.Wrap(x + 1, size)];
                    float d = h[TileNoise.Wrap(y - 1, size) * size + x];
                    float u = h[TileNoise.Wrap(y + 1, size) * size + x];
                    var n = new Vector3(-(r - l) * strength, -(u - d) * strength, 1f).normalized;
                    result[y * size + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
                }
            }
            return result;
        }

        // ------------------------------------------------------------------ organic

        public static void Moss()
        {
            var blot = new TileNoise(707);
            var col = new TileNoise(808);
            Color a = EditorUtil.Hex("1E3B2A"), b = EditorUtil.Hex("3F7A44");
            var pixels = new Color[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float u = x / (float)Size, v = y / (float)Size;
                    float blotch = Smooth(0.48f, 0.6f, blot.Fbm(u, v, 4, 5));
                    float strands = Smooth(0.6f, 0.7f, blot.Fbm(u + 0.2f, v, 24, 2, 3));
                    float detail = col.Fbm(u, v, 32, 3);
                    float alpha = Mathf.Clamp01(Mathf.Max(blotch, strands) * (0.6f + 0.7f * detail));
                    Color c = Color.Lerp(a, b, col.Fbm(u, v, 16, 3)) * (0.8f + 0.4f * detail);
                    c.a = alpha;
                    pixels[y * Size + x] = c;
                }
            }
            SavePixels("moss", pixels, Size, false);
        }

        public static void Clay()
        {
            var noise = new TileNoise(909);
            Color baseCol = EditorUtil.Hex("8A4A24");
            var pixels = new Color[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float u = x / (float)Size, v = y / (float)Size;
                    float n = noise.Fbm(u, v, 8, 5);
                    float rings = Mathf.Sin((v * 14f + (n - 0.5f) * 0.5f) * Mathf.PI * 2f) * 0.5f + 0.5f;
                    Color c = baseCol * (0.8f + 0.35f * n);
                    c = Color.Lerp(c, c * 0.7f, Smooth(0.75f, 1f, rings));
                    if (noise.Hash(x, y) < 0.015f) c *= 0.75f; // speckles
                    c.a = 1f;
                    pixels[y * Size + x] = c;
                }
            }
            SavePixels("clay", pixels, Size, false);
        }

        public static void Wood()
        {
            var noise = new TileNoise(1001);
            Color dark = EditorUtil.Hex("4E3220"), light = EditorUtil.Hex("8A5E36");
            var pixels = new Color[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float u = x / (float)Size, v = y / (float)Size;
                    float n = noise.Fbm(u, v, 16, 2, 4); // stretched along v = grain
                    float grain = Mathf.Sin((u * 12f + n * 1.5f) * Mathf.PI * 2f) * 0.5f + 0.5f;
                    Color c = Color.Lerp(dark, light, grain * 0.6f + n * 0.4f);
                    c.a = 1f;
                    pixels[y * Size + x] = c;
                }
            }
            SavePixels("wood", pixels, Size, false);
        }

        // ------------------------------------------------------------------ glyphs (signed distance drawing)

        static float Circle(Vector2 p, Vector2 c, float r) => (p - c).magnitude - r;
        static float Ring(Vector2 p, Vector2 c, float r, float w) => Mathf.Abs((p - c).magnitude - r) - w;

        static float Segment(Vector2 p, Vector2 a, Vector2 b, float w)
        {
            Vector2 pa = p - a, ba = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(pa, ba) / Mathf.Max(0.0001f, ba.sqrMagnitude));
            return (pa - ba * t).magnitude - w;
        }

        delegate float Sdf(Vector2 p);

        /// <summary>Renders an SDF to a white-on-black glyph (RGB = alpha, so it works as an emission map).</summary>
        static void SaveGlyph(string name, Sdf sdf)
        {
            var pixels = new Color[GlyphSize * GlyphSize];
            for (int y = 0; y < GlyphSize; y++)
            {
                for (int x = 0; x < GlyphSize; x++)
                {
                    float d = sdf(new Vector2(x + 0.5f, y + 0.5f));
                    float a = Mathf.Clamp01(0.5f - d / 1.5f);
                    pixels[y * GlyphSize + x] = new Color(a, a, a, a);
                }
            }
            SavePixels(name, pixels, GlyphSize, true);
        }

        public static void GlyphSun()
        {
            var c = new Vector2(128f, 128f);
            SaveGlyph("glyph_sun", p =>
            {
                float d = Mathf.Min(Ring(p, c, 56f, 5f), Circle(p, c, 24f));
                for (int i = 0; i < 12; i++)
                {
                    float ang = i * Mathf.PI * 2f / 12f;
                    var dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                    float inner = i % 2 == 0 ? 72f : 76f, outer = i % 2 == 0 ? 112f : 98f;
                    d = Mathf.Min(d, Segment(p, c + dir * inner, c + dir * outer, 5f));
                }
                return d;
            });
        }

        public static void GlyphMoon()
        {
            SaveGlyph("glyph_moon", p =>
            {
                float crescent = Mathf.Max(Circle(p, new Vector2(118f, 128f), 82f), -Circle(p, new Vector2(150f, 140f), 70f));
                float d = crescent;
                d = Mathf.Min(d, Circle(p, new Vector2(196f, 70f), 7f));
                d = Mathf.Min(d, Circle(p, new Vector2(214f, 112f), 5f));
                d = Mathf.Min(d, Circle(p, new Vector2(200f, 196f), 6f));
                return d;
            });
        }

        public static void GlyphSerpent()
        {
            const int samples = 120;
            var pts = new Vector2[samples + 1];
            for (int i = 0; i <= samples; i++)
            {
                float t = i / (float)samples;
                pts[i] = new Vector2(36f + t * 170f, 128f + 46f * Mathf.Sin(t * Mathf.PI * 2f * 1.75f));
            }
            Vector2 head = pts[samples] + new Vector2(10f, 4f);
            SaveGlyph("glyph_serpent", p =>
            {
                float d = 999f;
                for (int i = 0; i < samples; i++)
                {
                    float w = Mathf.Lerp(3f, 9f, i / (float)samples); // tail thin, neck thick
                    d = Mathf.Min(d, Segment(p, pts[i], pts[i + 1], w));
                }
                d = Mathf.Min(d, Circle(p, head, 16f));
                d = Mathf.Max(d, -Circle(p, head + new Vector2(4f, 6f), 4f)); // eye
                d = Mathf.Min(d, Segment(p, head + new Vector2(14f, 0f), head + new Vector2(32f, 6f), 2f));
                d = Mathf.Min(d, Segment(p, head + new Vector2(14f, 0f), head + new Vector2(32f, -6f), 2f));
                return d;
            });
        }

        public static void RandomGlyph(int index)
        {
            var rnd = new System.Random(500 + index);
            Vector2 GridPoint() => new Vector2(48f + rnd.Next(5) * 40f, 48f + rnd.Next(5) * 40f);
            int strokes = 3 + rnd.Next(3);
            var a = new Vector2[strokes];
            var b = new Vector2[strokes];
            for (int i = 0; i < strokes; i++) { a[i] = GridPoint(); b[i] = GridPoint(); }
            bool frameCircle = rnd.Next(2) == 0;
            Vector2 dot = GridPoint();
            SaveGlyph("glyph_random_" + index, p =>
            {
                var c = new Vector2(128f, 128f);
                float d = frameCircle
                    ? Ring(p, c, 112f, 4f)
                    : Mathf.Abs(Mathf.Max(Mathf.Abs(p.x - 128f), Mathf.Abs(p.y - 128f)) - 110f) - 4f;
                for (int i = 0; i < strokes; i++) d = Mathf.Min(d, Segment(p, a[i], b[i], 6f));
                d = Mathf.Min(d, Circle(p, dot, 10f));
                return d;
            });
        }

        // ------------------------------------------------------------------ particles + UI

        /// <summary>White radial blob with soft edges, used for fire, embers and dust particles.</summary>
        public static Texture2D SoftParticle(int size = 128)
        {
            var pixels = new Color[size * size];
            float half = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - half) / half, dy = (y - half) / half;
                    float a = Smooth(0f, 1f, 1f - Mathf.Sqrt(dx * dx + dy * dy));
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            return SavePixels("soft_particle", pixels, size, true);
        }

        public static void UiVignette()
        {
            const int size = 256;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x / (size - 1f) - 0.5f) * 2f, dy = (y / (size - 1f) - 0.5f) * 2f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / 1.4142f;
                    pixels[y * size + x] = new Color(0f, 0f, 0f, Smooth(0.3f, 1f, d) * 0.95f);
                }
            }
            SavePixels("ui_vignette", pixels, size, true, sprite: true, spriteBorder: Vector4.zero);
        }

        /// <summary>White rounded rectangle, 9-sliced so buttons and bars of any size keep round corners.</summary>
        public static void UiBar()
        {
            const int size = 64;
            const float radius = 16f;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = Mathf.Abs(x + 0.5f - size * 0.5f) - (size * 0.5f - radius);
                    float py = Mathf.Abs(y + 0.5f - size * 0.5f) - (size * 0.5f - radius);
                    float outside = new Vector2(Mathf.Max(px, 0f), Mathf.Max(py, 0f)).magnitude + Mathf.Min(Mathf.Max(px, py), 0f) - radius;
                    pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - outside));
                }
            }
            SavePixels("ui_bar", pixels, size, true, sprite: true, spriteBorder: new Vector4(18f, 18f, 18f, 18f));
        }

        public static void UiHeart()
        {
            const int size = 128;
            const int ss = 4; // supersampling for smooth edges
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int inside = 0;
                    for (int sy = 0; sy < ss; sy++)
                    {
                        for (int sx = 0; sx < ss; sx++)
                        {
                            float fx = ((x + (sx + 0.5f) / ss) / size - 0.5f) * 2.6f;
                            float fy = ((y + (sy + 0.5f) / ss) / size - 0.45f) * 2.6f;
                            float a = fx * fx + fy * fy - 1f;
                            if (a * a * a - fx * fx * fy * fy * fy <= 0f) inside++;
                        }
                    }
                    pixels[y * size + x] = new Color(1f, 1f, 1f, inside / (float)(ss * ss));
                }
            }
            SavePixels("ui_heart", pixels, size, true, sprite: true, spriteBorder: Vector4.zero);
        }

        // ------------------------------------------------------------------ saving

        static Texture2D SavePixels(string name, Color[] pixels, int size, bool clamp, bool isNormalMap = false,
            bool sprite = false, Vector4 spriteBorder = default)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.SetPixels(pixels);
            tex.Apply();
            return SavePng(tex, name, clamp, isNormalMap, sprite, spriteBorder);
        }

        /// <summary>Writes the texture to Folder/name.png, imports it with the right settings and returns the asset.</summary>
        public static Texture2D SavePng(Texture2D tex, string name, bool clamp, bool isNormalMap = false,
            bool sprite = false, Vector4 spriteBorder = default)
        {
            EditorUtil.EnsureFolder($"{Folder}/{AssetCategories.Texture(name)}");
            string assetPath = PathOf(name);
            string fullPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), assetPath);
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            if (sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                var ts = new TextureImporterSettings();
                importer.ReadTextureSettings(ts);
                ts.spriteMeshType = SpriteMeshType.FullRect;
                ts.spriteBorder = spriteBorder;
                importer.SetTextureSettings(ts);
            }
            else
            {
                importer.textureType = isNormalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.mipmapEnabled = true;
            }
            importer.sRGBTexture = !isNormalMap;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = !isNormalMap;
            importer.wrapMode = clamp ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }
    }
}
