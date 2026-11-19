// DoorMotion: shared coroutine that slides a door slab from one local position to another over
// time, either smoothly (ease in-out, for grinding stone doors) or accelerating (for a slam).
using System.Collections;
using UnityEngine;

namespace LastTorch
{
    public static class DoorMotion
    {
        public static IEnumerator Slide(Transform slab, Vector3 from, Vector3 to, float duration, bool accelerate = false)
        {
            float t = 0f;
            duration = Mathf.Max(0.01f, duration);
            while (t < 1f)
            {
                t = Mathf.Min(1f, t + Time.deltaTime / duration);
                float k = accelerate ? t * t : t * t * (3f - 2f * t);
                slab.localPosition = Vector3.LerpUnclamped(from, to, k);
                yield return null;
            }
        }

        /// <summary>Horizontal distance between two points (ignores height).</summary>
        public static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f; b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
