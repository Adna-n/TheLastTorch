// FlickerLight: makes a wall torch's point light flicker like fire using Perlin noise.
// Uses the torch flicker speed/amount and the menu torch intensity/range from GameSettings.
using UnityEngine;

namespace LastTorch
{
    [RequireComponent(typeof(Light))]
    public class FlickerLight : MonoBehaviour
    {
        Light lightSource;
        float seed;

        void Awake()
        {
            lightSource = GetComponent<Light>();
            seed = transform.position.x * 13.1f + transform.position.z * 7.7f; // different per torch
        }

        void Update()
        {
            var s = GameManager.Settings;
            float n = Mathf.PerlinNoise(seed, Time.time * s.flickerSpeed) - 0.5f;
            lightSource.intensity = s.menuTorchIntensity * (1f + n * 2f * s.flickerAmount);
            lightSource.range = s.menuTorchRange * (1f + n * 0.5f * s.flickerAmount);
            lightSource.color = s.torchColor;
        }
    }
}
