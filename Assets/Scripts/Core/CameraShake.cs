// CameraShake: sits on the camera and produces a shaking offset using Perlin noise.
// Shake(intensity, duration) gives a short hit that fades out (damage, door slam);
// SetContinuous(intensity) keeps a constant rumble (temple collapse). ThirdPersonCamera adds Offset.
using UnityEngine;

namespace LastTorch
{
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        /// <summary>Current shake offset in world space (added by ThirdPersonCamera).</summary>
        public Vector3 Offset { get; private set; }

        float impulse;
        float impulseDuration;
        float impulseTimeLeft;
        float continuous;
        float seed;

        void Awake()
        {
            Instance = this;
            seed = Random.value * 100f;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>A shake that fades out over 'duration' seconds. A stronger shake overrides a weaker one.</summary>
        public void Shake(float intensity, float duration)
        {
            float current = impulseDuration > 0f ? impulse * (impulseTimeLeft / impulseDuration) : 0f;
            if (intensity < current) return;
            impulse = intensity;
            impulseDuration = Mathf.Max(0.01f, duration);
            impulseTimeLeft = impulseDuration;
        }

        /// <summary>Constant shake that stays until set back to 0.</summary>
        public void SetContinuous(float intensity) { continuous = Mathf.Max(0f, intensity); }

        void Update()
        {
            if (impulseTimeLeft > 0f) impulseTimeLeft = Mathf.Max(0f, impulseTimeLeft - Time.deltaTime);
            float amount = continuous + (impulseDuration > 0f ? impulse * (impulseTimeLeft / impulseDuration) : 0f);

            if (amount <= 0f)
            {
                Offset = Vector3.zero;
                return;
            }

            float t = Time.time * GameManager.Settings.shakeFrequency;
            Offset = new Vector3(
                Mathf.PerlinNoise(seed, t) - 0.5f,
                Mathf.PerlinNoise(seed + 10f, t) - 0.5f,
                Mathf.PerlinNoise(seed + 20f, t) - 0.5f) * (2f * amount);
        }
    }
}
