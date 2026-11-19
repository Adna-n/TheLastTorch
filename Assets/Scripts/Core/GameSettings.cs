// GameSettings: the ONE place every tunable gameplay number lives (speeds, fuel, damage, scores...).
// It is a ScriptableObject saved at Assets/Settings/GameSettings.asset. Gameplay scripts read it every
// frame through GameManager.Settings, so editing a value in the Inspector during Play mode changes
// the game instantly (useful for live changes during evaluation).
using UnityEngine;

namespace LastTorch
{
    [CreateAssetMenu(fileName = "GameSettings", menuName = "The Last Torch/Game Settings")]
    public class GameSettings : ScriptableObject
    {
        [Header("Player")]
        public float walkSpeed = 4f;
        public float sprintSpeed = 6.5f;
        [Tooltip("How fast the explorer turns toward the move direction.")]
        public float rotationSpeed = 12f;
        public float gravity = -20f;
        [Tooltip("Small downward speed while grounded so the controller stays glued to the floor.")]
        public float groundStickForce = -2f;
        [Tooltip("Metres walked per footstep (also drives the walk animation cycle).")]
        public float strideLength = 1.4f;
        [Range(0f, 1f)] public float footstepVolume = 0.5f;
        public float footstepPitchMin = 0.9f;
        public float footstepPitchMax = 1.1f;

        [Header("Camera")]
        public float mouseSensitivity = 0.12f;
        public float cameraDistance = 4.5f;
        public float minPitch = -20f;
        public float maxPitch = 60f;
        public float startPitch = 15f;
        [Tooltip("Height of the point the camera orbits around (above the player's feet).")]
        public float cameraPivotHeight = 1.6f;
        [Tooltip("Sideways over-the-shoulder offset.")]
        public float shoulderOffset = 0.45f;
        [Tooltip("Radius of the SphereCast that stops the camera going through walls.")]
        public float cameraCollisionRadius = 0.3f;
        public float cameraMinDistance = 0.6f;
        [Tooltip("How quickly the camera slides back out after a wall pushed it in.")]
        public float cameraReturnSpeed = 6f;

        [Header("Camera Shake")]
        public float shakeFrequency = 25f;
        public float hitShakeIntensity = 0.25f;
        public float hitShakeDuration = 0.25f;

        [Header("Torch")]
        public float maxFuel = 100f;
        public float burnRatePerSec = 0.8f;
        public float sprintBurnMultiplier = 1.6f;
        public float minLightRange = 4f;
        public float maxLightRange = 14f;
        public float lightIntensity = 3f;
        [Tooltip("Below this fuel % the HUD pulses and a heartbeat plays.")]
        public float lowFuelWarningPercent = 20f;
        public Color torchColor = new Color(1f, 0.62f, 0.28f);
        public float flickerSpeed = 9f;
        [Range(0f, 1f)] public float flickerAmount = 0.15f;
        [Tooltip("Flame particle size at 0% fuel, relative to full size.")]
        [Range(0f, 1f)] public float minFlameScale = 0.35f;
        public float lowFuelBeatInterval = 1.1f;
        public bool torchShadows = true;

        [Header("Oil")]
        public float oilRefillAmount = 25f;

        [Header("Health")]
        public int maxHearts = 3;
        public float invincibilitySeconds = 1.5f;
        public float knockbackForce = 6f;
        [Tooltip("How quickly knockback slows down.")]
        public float knockbackDecay = 8f;
        public float blinkInterval = 0.1f;

        [Header("Traps")]
        public float spikeDownTime = 2f;
        public float spikeWarnTime = 0.5f;
        public float spikeUpTime = 1f;
        public float dartSpeed = 14f;
        public float dartCooldown = 2f;
        public float boulderSpeed = 8f;

        [Header("Collapse")]
        public float collapseDuration = 60f;
        public float debrisPerSecond = 3f;

        [Header("Score")]
        public int gemPoints = 10;
        public int oilPoints = 5;
        public int keyPoints = 50;
        public int heartPoints = 200;
        public int damagePenalty = 20;
        public int escapeBonusPerSecond = 5;
        public int torchBonusPerFuel = 1;

        [Header("Difficulty (fuel burn multipliers)")]
        public float easyBurnMult = 0.7f;
        public float normalBurnMult = 1.0f;
        public float hardBurnMult = 1.4f;

        [Header("Level")]
        public float cellSize = 4f;
        public float wallHeight = 4f;

        [Header("Collectibles")]
        public float collectibleBobHeight = 0.15f;
        public float collectibleBobSpeed = 2f;
        public float collectibleSpinSpeed = 90f;

        [Header("Explorer Animation")]
        public float legSwingAngle = 35f;
        public float armSwingAngle = 25f;
        [Tooltip("How high the torch arm is held (degrees forward/up).")]
        public float torchArmRaiseAngle = 65f;
        [Tooltip("How much the torch arm still swings while walking (0 = rigid, 1 = like the free arm).")]
        [Range(0f, 1f)] public float torchArmSwingFactor = 0.3f;
        public float bodyBobHeight = 0.05f;
        public float moveLeanAngle = 6f;
        public float animBlendSpeed = 6f;
        public float breathSpeed = 1.6f;
        public float breathAmount = 0.012f;
        public float breathSwayAngle = 1.2f;
        public float flinchAngle = 20f;
        public float flinchDuration = 0.3f;

        [Header("Intro")]
        public float introDuration = 7f;

        [Header("Lighting")]
        public Color ambientColor = new Color(0.02f, 0.02f, 0.03f);
        public float fogDensity = 0.05f;

        [Header("Audio")]
        [Range(0f, 1f)] public float musicVolume = 0.5f;
        [Range(0f, 1f)] public float sfxVolume = 1f;
        public int sfxPoolSize = 16;
        public float sfxMaxDistance = 30f;

        [Header("Debug")]
        [Tooltip("Enables the debug overlay keys (H = hurt, R = restart). Turn off for the final build.")]
        public bool enableDebugKeys = true;

        /// <summary>Fuel burn multiplier for the chosen difficulty.</summary>
        public float GetDifficultyMultiplier(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Easy: return easyBurnMult;
                case Difficulty.Hard: return hardBurnMult;
                default: return normalBurnMult;
            }
        }
    }
}
