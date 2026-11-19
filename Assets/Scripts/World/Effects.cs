// Effects: tiny helper that spawns a one-shot particle prefab (pickup sparks, dust puffs) at a
// position, optionally tints it, and plays it. The prefab destroys itself when finished.
using UnityEngine;

namespace LastTorch
{
    public static class Effects
    {
        public static ParticleSystem Spawn(GameObject prefab, Vector3 position, Color? tint = null)
        {
            if (prefab == null) return null;
            var go = Object.Instantiate(prefab, position, Quaternion.identity);
            var ps = go.GetComponent<ParticleSystem>();
            if (ps == null) return null;
            if (tint.HasValue)
            {
                var main = ps.main;
                main.startColor = tint.Value;
            }
            ps.Play(true);
            return ps;
        }
    }
}
