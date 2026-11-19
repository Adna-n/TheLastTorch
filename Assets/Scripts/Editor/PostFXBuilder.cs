// PostFXBuilder: creates the two URP post-processing profiles in Assets/Generated/PostFX.
// GlobalProfile = the normal look (bloom so glowing things shine, vignette, slightly warm colours,
// light film grain). CollapseProfile = the escape look (red tint, heavy vignette, chromatic
// aberration); its Volume starts at weight 0 and CollapseSequence raises it to 1.
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LastTorch.EditorTools
{
    public static class PostFXBuilder
    {
        public const string Folder = "Assets/Generated/PostFX";
        public const string GlobalPath = Folder + "/GlobalProfile.asset";
        public const string CollapsePath = Folder + "/CollapseProfile.asset";

        public static void GenerateAll()
        {
            var global = GetOrCreateEmptyProfile(GlobalPath);
            var bloom = Add<Bloom>(global);
            bloom.threshold.Override(0.9f);
            bloom.intensity.Override(0.8f);
            bloom.scatter.Override(0.7f);
            var vignette = Add<Vignette>(global);
            vignette.intensity.Override(0.35f);
            vignette.smoothness.Override(0.4f);
            var color = Add<ColorAdjustments>(global);
            color.colorFilter.Override(new Color(1f, 0.94f, 0.86f));
            color.contrast.Override(10f);
            var grain = Add<FilmGrain>(global);
            grain.type.Override(FilmGrainLookup.Medium1);
            grain.intensity.Override(0.15f);
            EditorUtility.SetDirty(global);

            var collapse = GetOrCreateEmptyProfile(CollapsePath);
            var cColor = Add<ColorAdjustments>(collapse);
            cColor.colorFilter.Override(new Color(1f, 0.45f, 0.4f));
            cColor.saturation.Override(-10f);
            var cVignette = Add<Vignette>(collapse);
            cVignette.intensity.Override(0.55f);
            cVignette.color.Override(new Color(0.25f, 0f, 0f));
            var chroma = Add<ChromaticAberration>(collapse);
            chroma.intensity.Override(0.6f);
            EditorUtility.SetDirty(collapse);

            AssetDatabase.SaveAssets();
        }

        public static VolumeProfile LoadGlobal() => AssetDatabase.LoadAssetAtPath<VolumeProfile>(GlobalPath);
        public static VolumeProfile LoadCollapse() => AssetDatabase.LoadAssetAtPath<VolumeProfile>(CollapsePath);

        /// <summary>Loads the profile (clearing its effects) or creates it. Keeps the same file so scene links survive.</summary>
        static VolumeProfile GetOrCreateEmptyProfile(string path)
        {
            EditorUtil.EnsureFolder(Folder);
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
                return profile;
            }
            foreach (var component in profile.components)
                if (component != null) Object.DestroyImmediate(component, true);
            profile.components.Clear();
            return profile;
        }

        static T Add<T>(VolumeProfile profile) where T : VolumeComponent
        {
            var component = profile.Add<T>();
            component.name = typeof(T).Name;
            component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }

        /// <summary>Adds a global post-processing Volume to the open scene.</summary>
        public static Volume CreateVolume(string name, VolumeProfile profile, float weight, float priority)
        {
            var go = new GameObject(name);
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
            volume.weight = weight;
            volume.priority = priority;
            return volume;
        }
    }
}
