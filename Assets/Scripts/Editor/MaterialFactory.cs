// MaterialFactory: creates every material as a .mat asset in Assets/Generated/Materials using URP
// shaders (Lit for solid objects, Particles/Unlit for fire and dust). Shaders are only looked up
// here in the editor, never at runtime, so they are not stripped from the build.
// Re-running overwrites the same files (idempotent). Phase 2 adds the full textured material set.
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastTorch.EditorTools
{
    public static class MaterialFactory
    {
        public const string Folder = "Assets/Generated/Materials";

        const string LitShaderName = "Universal Render Pipeline/Lit";
        const string ParticleShaderName = "Universal Render Pipeline/Particles/Unlit";

        static Shader FindShader(string name)
        {
            var shader = Shader.Find(name);
            if (shader == null) throw new System.Exception("Shader not found: " + name + " (is URP installed?)");
            return shader;
        }

        /// <summary>Loads Folder/name.mat or creates it with the given shader.</summary>
        public static Material GetOrCreate(string name, string shaderName)
        {
            EditorUtil.EnsureFolder(Folder);
            var shader = FindShader(shaderName);
            string path = $"{Folder}/{name}.mat";
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

            mat.DisableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", Color.black);
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
