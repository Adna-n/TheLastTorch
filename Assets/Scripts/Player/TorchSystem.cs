// TorchSystem: the game's core mechanic. Fuel drains every second (faster when sprinting and on
// harder difficulty). Less fuel = smaller light radius and smaller flame. The light flickers with
// Perlin noise. Below the warning % it raises OnLowFuelChanged and plays a heartbeat.
// At 0 fuel the player loses ("The darkness took you."). Oil jars call Refuel().
using System;
using UnityEngine;

namespace LastTorch
{
    public class TorchSystem : MonoBehaviour
    {
        public Light torchLight;
        public ParticleSystem flame;
        public ParticleSystem embers;
        public AudioSource crackleSource;

        public float Fuel { get; private set; }
        public float MaxFuel => Mathf.Max(1f, GameManager.Settings.maxFuel);
        public float FuelPercent => Mathf.Clamp01(Fuel / MaxFuel);
        public bool IsLowFuel { get; private set; }
        public bool IsBurnedOut { get; private set; }

        /// <summary>(currentFuel, maxFuel)</summary>
        public event Action<float, float> OnFuelChanged;
        public event Action<bool> OnLowFuelChanged;

        PlayerController player;
        float flameBaseSize = 1f;
        float emberBaseRate;
        float beatTimer;
        float noiseSeed;

        void Awake()
        {
            player = GetComponent<PlayerController>();
            Fuel = MaxFuel;
            noiseSeed = UnityEngine.Random.value * 100f;
            if (flame != null) flameBaseSize = flame.main.startSizeMultiplier;
            if (embers != null) emberBaseRate = embers.emission.rateOverTimeMultiplier;
        }

        void Start()
        {
            if (torchLight != null)
                torchLight.shadows = GameManager.Settings.torchShadows ? LightShadows.Soft : LightShadows.None;
            if (AudioManager.Instance != null) AudioManager.Instance.SetupLoopSource(crackleSource, SoundId.TorchLoop, 0.6f);
            OnFuelChanged?.Invoke(Fuel, MaxFuel);
        }

        void Update()
        {
            var s = GameManager.Settings;
            var gm = GameManager.Instance;
            bool active = gm == null || gm.IsGameplayActive;

            if (active && !IsBurnedOut)
            {
                float rate = s.burnRatePerSec;
                if (player != null && player.IsSprinting) rate *= s.sprintBurnMultiplier;
                if (gm != null) rate *= gm.DifficultyMultiplier;
                SetFuel(Fuel - rate * Time.deltaTime);
            }

            UpdateVisuals(s);
            UpdateLowFuelWarning(s, active);
        }

        /// <summary>Adds fuel (from an oil jar). Does nothing once the torch has burned out.</summary>
        public void Refuel(float amount)
        {
            if (IsBurnedOut) return;
            SetFuel(Fuel + amount);
        }

        void SetFuel(float value)
        {
            Fuel = Mathf.Clamp(value, 0f, MaxFuel);
            OnFuelChanged?.Invoke(Fuel, MaxFuel);

            if (Fuel <= 0f && !IsBurnedOut)
            {
                IsBurnedOut = true;
                if (flame != null) flame.Stop();
                if (embers != null) embers.Stop();
                if (crackleSource != null) crackleSource.Stop();
                if (GameManager.Instance != null) GameManager.Instance.Lose("The darkness took you.");
            }
        }

        void UpdateVisuals(GameSettings s)
        {
            float pct = FuelPercent;

            if (torchLight != null)
            {
                float flicker = (Mathf.PerlinNoise(noiseSeed, Time.time * s.flickerSpeed) - 0.5f) * 2f * s.flickerAmount;
                torchLight.range = Mathf.Lerp(s.minLightRange, s.maxLightRange, pct);
                torchLight.intensity = IsBurnedOut ? 0f : s.lightIntensity * (1f + flicker);
                torchLight.color = s.torchColor;
            }

            float flameScale = Mathf.Lerp(s.minFlameScale, 1f, pct);
            if (flame != null)
            {
                var main = flame.main;
                main.startSizeMultiplier = flameBaseSize * flameScale;
            }
            if (embers != null)
            {
                var emission = embers.emission;
                emission.rateOverTimeMultiplier = emberBaseRate * flameScale;
            }
        }

        void UpdateLowFuelWarning(GameSettings s, bool active)
        {
            bool low = !IsBurnedOut && FuelPercent * 100f <= s.lowFuelWarningPercent;
            if (low != IsLowFuel)
            {
                IsLowFuel = low;
                beatTimer = 0f;
                OnLowFuelChanged?.Invoke(low);
            }

            if (IsLowFuel && active)
            {
                beatTimer -= Time.deltaTime;
                if (beatTimer <= 0f)
                {
                    beatTimer = s.lowFuelBeatInterval;
                    if (AudioManager.Instance != null) AudioManager.Instance.Play(SoundId.LowFuelBeat);
                }
            }
        }
    }
}
