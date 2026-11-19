// EditorUtil: small shared helpers for the generator scripts (create asset folders,
// convert hex colours, make primitive parts without colliders).
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastTorch.EditorTools
{
    public static class EditorUtil
    {
        /// <summary>Creates a folder like "Assets/Generated/Textures" (and its parents) if missing.</summary>
        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            string name = path.Substring(slash + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        public static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex.StartsWith("#") ? hex : "#" + hex, out var c) ? c : Color.magenta;
        }

        public static GameObject Empty(string name, Transform parent, Vector3 localPosition)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            return go;
        }

        /// <summary>A decorative primitive: collider removed, material set.</summary>
        public static GameObject Part(PrimitiveType type, string name, Transform parent, Vector3 localPosition,
            Vector3 localScale, Material material, Vector3 localEuler = default, bool castShadows = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(localEuler);
            go.transform.localScale = localScale;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return go;
        }
    }
}
