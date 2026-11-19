// HeartOfEternity: the treasure. A floating crystal (bob/spin from Collectible) with a pulsing teal
// light and a humming sound. Taking it (Collectible type Heart: +200 points) starts the
// CollapseSequence: the escape countdown begins.
using UnityEngine;

namespace LastTorch
{
    [RequireComponent(typeof(Collectible))]
    public class HeartOfEternity : MonoBehaviour
    {
        public Light glow;

        float baseIntensity;

        void Awake()
        {
            GetComponent<Collectible>().Collected += OnTaken;
            if (glow != null) baseIntensity = glow.intensity;
        }

        void Update()
        {
            if (glow == null) return;
            var s = GameManager.Settings;
            glow.intensity = baseIntensity * (1f + Mathf.Sin(Time.time * s.heartPulseSpeed) * s.heartPulseAmount);
        }

        void OnTaken(Collectible c)
        {
            Debug.Log("[Heart] The Heart of Eternity has been taken. The temple begins to collapse!");
            if (CollapseSequence.Instance != null) CollapseSequence.Instance.Begin();
            else if (GameManager.Instance != null) GameManager.Instance.BeginCollapse();
        }
    }
}
