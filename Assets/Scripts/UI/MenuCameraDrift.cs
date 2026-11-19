// MenuCameraDrift: slowly and smoothly sways the main-menu camera (Perlin noise) so the torch-lit
// background feels alive. Amount and speed are in GameSettings (Main Menu group).
using UnityEngine;

namespace LastTorch
{
    public class MenuCameraDrift : MonoBehaviour
    {
        Vector3 basePosition;
        Quaternion baseRotation;

        void Start()
        {
            basePosition = transform.position;
            baseRotation = transform.rotation;
        }

        void Update()
        {
            var s = GameManager.Settings;
            float t = Time.time * s.menuDriftSpeed;
            var offset = new Vector3(Mathf.PerlinNoise(t, 0f) - 0.5f, Mathf.PerlinNoise(0f, t) - 0.5f, 0f) * (2f * s.menuDriftAmount);
            transform.position = basePosition + baseRotation * offset;
            float yaw = (Mathf.PerlinNoise(t + 5f, 5f) - 0.5f) * 2f * s.menuDriftYawDegrees;
            transform.rotation = baseRotation * Quaternion.Euler(0f, yaw, 0f);
        }
    }
}
