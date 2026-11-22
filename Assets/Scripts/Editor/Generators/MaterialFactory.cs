// MaterialFactory: creates every material as a .mat asset in Assets/Generated/Materials using URP
// shaders (Lit for solid objects, Particles/Unlit for fire and dust). Shaders are only looked up
// here in the editor, never at runtime, so they are not stripped from the build.
// GenerateAll() makes the full set (stone, moss, clay, wood, metal, glowing gems/keys/Heart, particles...).
// Re-running overwrites the same files (idempotent).
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static LastTorch.EditorTools.EditorUtil;

namespace LastTorch.EditorTools
{
    public static class MaterialFactory
    {
        public const string Folder = "Assets/Generated/Materials";

        const string LitShaderName = "Universal Render Pipeline/Lit";
        const string ParticleShaderName = "Universal Render Pipeline/Particles/Unlit";

        public static string PathOf(string name) => $"{Folder}/{AssetCategories.Material(name)}/{name}.mat";
        public static Material Load(string name) => AssetDatabase.LoadAssetAtPath<Material>(PathOf(name));

        public static void GenerateAll()
        {
            var wall = TextureGenerator.Load("stone_wall");
            var wallN = TextureGenerator.Load("stone_wall_normal");
            var floor = TextureGenerator.Load("stone_floor");
            var floorN = TextureGenerator.Load("stone_floor_normal");
            var soft = TextureGenerator.Load("soft_particle");

            // Stone family
            Lit("Stone_Wall", Color.white, 0.12f, 0f, wall, wallN);
            Lit("Stone_Floor", Color.white, 0.2f, 0f, floor, floorN);
            Lit("Stone_Dark", Hex("8A8F84"), 0.15f, 0f, wall, wallN);
            Lit("Rock", Hex("9A968A"), 0.1f, 0f, floor, floorN);
            Lit("Sand", Hex("D8C49A"), 0.05f, 0f, floor, floorN);
            Lit("Hole", Hex("050505"), 0f);
            AlphaClipped("Moss", TextureGenerator.Load("moss"), 0.5f);

            // Props
            LitEmissive("Clay", Color.white, Hex("8A4A24") * 0.3f, 0.3f, TextureGenerator.Load("clay"));
            Wood();
            Lit("Metal", Hex("7A7F86"), 0.65f, 0.85f);
            LitEmissive("Gem", Hex("BFFFF5"), Hex("5FE8D8") * 2f, 0.9f);
            LitEmissive("Heart", Hex("2EE6D6"), Hex("2EE6D6") * 6f, 0.95f);
            LitEmissive("Daylight", Hex("FFF4DC"), Hex("FFE9C0") * 5f, 0f);

            // Key glyphs: dark stone with a glowing symbol (glyph texture is used as the emission map)
            LitEmissive("Key_Sun", Hex("3A3A36"), Hex("FF9A3C") * 4f, 0.3f, wall, TextureGenerator.Load("glyph_sun"));
            LitEmissive("Key_Moon", Hex("3A3A36"), Hex("BFD8FF") * 3.5f, 0.3f, wall, TextureGenerator.Load("glyph_moon"));
            LitEmissive("Key_Serpent", Hex("3A3A36"), Hex("5CFF7A") * 3.5f, 0.3f, wall, TextureGenerator.Load("glyph_serpent"));

            // Faint carved glyphs for wall decoration
            for (int i = 1; i <= 4; i++)
                LitEmissive("Glyph_Wall_" + i, Hex("2A2E2A"), Hex("6FD3C0") * 0.8f, 0.2f, wall, TextureGenerator.Load("glyph_random_" + i));

            // Sanctum sockets
            Lit("Socket_Off", Hex("2C2C2A"), 0.4f);
            LitEmissive("Socket_On", Hex("FFD27A"), Hex("FFB040") * 4f, 0.6f);

            // Particles
            Particle("Particle_Fire", soft, new Color(1f, 0.55f, 0.2f, 1f), true);
            Particle("Particle_Ember", soft, new Color(1f, 0.7f, 0.3f, 1f), true);
            Particle("Particle_Dust", soft, new Color(0.55f, 0.5f, 0.42f, 0.35f), false);
            Particle("Particle_Warning", soft, new Color(1f, 0.1f, 0.05f, 0.8f), true);

            // Explorer
            ExplorerBuilder.CreateMaterials();
        }

        /// <summary>Wood material, textured if the wood texture has been generated.</summary>
        public static Material Wood()
        {
            var tex = TextureGenerator.Load("wood");
            return Lit("Wood", tex != null ? Color.white : Hex("7A5230"), 0.2f, 0f, tex);
        }

        static Shader FindShader(string name)
        {
            var shader = Shader.Find(name);
            if (shader == null) throw new System.Exception("Shader not found: " + name + " (is URP installed?)");
            return shader;
        }

        /// <summary>Loads Folder/name.mat or creates it with the given shader.</summary>
        public static Material GetOrCreate(string name, string shaderName)
        {
            EnsureFolder($"{Folder}/{AssetCategories.Material(name)}");
            var shader = FindShader(shaderName);
            string path = PathOf(name);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.shader != shader)
            {
                mat.shader = shader;
            }
            return mat;
        }

        /// <summary>Opaque URP/Lit material.</summary>
        public static Material Lit(string name, Color color, float smoothness = 0.2f, float metallic = 0f,
            Texture2D baseMap = null, Texture2D normalMap = null)
        {
            var mat = GetOrCreate(name, LitShaderName);
            mat.SetColor("_BaseColor", color);
            mat.SetTexture("_BaseMap", baseMap);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", metallic);

            mat.SetTexture("_BumpMap", normalMap);
            if (normalMap != null) mat.EnableKeyword("_NORMALMAP");
            else mat.DisableKeyword("_NORMALMAP");

            // Opaque defaults (in case the material was something else before)
            mat.SetFloat("_Surface", 0f);
            mat.SetFloat("_AlphaClip", 0f);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.SetFloat("_Cull", (float)CullMode.Back);
            mat.SetOverrideTag("RenderType", "Opaque");
            mat.renderQueue = -1;

            mat.DisableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", Color.black);
            mat.SetTexture("_EmissionMap", null);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>URP/Lit material that glows (emission is HDR: values above 1 bloom).</summary>
        public static Material LitEmissive(string name, Color color, Color emission, float smoothness = 0.5f,
            Texture2D baseMap = null, Texture2D emissionMap = null)
        {
            var mat = Lit(name, color, smoothness, 0f, baseMap);
            SetEmission(mat, emission, emissionMap);
            return mat;
        }

        public static void SetEmission(Material mat, Color emission, Texture2D emissionMap = null)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emission);
            mat.SetTexture("_EmissionMap", emissionMap);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(mat);
        }

        /// <summary>Lit material with cut-out transparency (moss, vines), visible from both sides.</summary>
        public static Material AlphaClipped(string name, Texture2D texture, float cutoff)
        {
            var mat = Lit(name, Color.white, 0.1f, 0f, texture);
            mat.SetFloat("_AlphaClip", 1f);
            mat.SetFloat("_Cutoff", cutoff);
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.SetFloat("_Cull", (float)CullMode.Off);
            mat.doubleSidedGI = true;
            mat.SetOverrideTag("RenderType", "TransparentCutout");
            mat.renderQueue = (int)RenderQueue.AlphaTest;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>Transparent particle material (additive for fire/embers, alpha-blended for dust).</summary>
        public static Material Particle(string name, Texture2D texture, Color tint, bool additive)
        {
            var mat = GetOrCreate(name, ParticleShaderName);
            mat.SetTexture("_BaseMap", texture);
            mat.SetColor("_BaseColor", tint);

            // Render states have to be set by hand when not using the material inspector
            mat.SetFloat("_Surface", 1f);                 // Transparent
            mat.SetFloat("_Blend", additive ? 2f : 0f);    // 2 = Additive, 0 = Alpha
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            mat.SetFloat("_DstBlendAlpha", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
