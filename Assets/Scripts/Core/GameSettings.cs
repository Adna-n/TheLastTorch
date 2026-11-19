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
        [Tooltip("Hearts lost per trap hit (spikes, darts, boulder, falling rocks).")]
        public int trapDamage = 1;
        [Tooltip("Delay between neighbouring spike tiles, creating a wave.")]
        public float spikeWaveOffset = 0.35f;
        [Tooltip("How far the spikes poke up during the warning (metres).")]
        public float spikeWarnRise = 0.3f;
        public float spikeMoveSpeed = 10f;
        [Tooltip("Spike sounds only play when the player is this close.")]
        public float spikeSoundDistance = 14f;
        [Tooltip("Delay between stepping on a dart plate and the darts firing.")]
        public float dartFireDelay = 0.25f;
        public float dartLifetime = 3f;
        public float dartLauncherHeight = 1.1f;
        public float dartPlateSinkDepth = 0.06f;
        [Tooltip("The boulder knocks the player back this many times harder than normal hits.")]
        public float boulderKnockbackMultiplier = 1.6f;

        [Header("Collapse")]
        public float collapseDuration = 60f;
        public float debrisPerSecond = 3f;
        public float collapseShakeIntensity = 0.06f;
        [Tooltip("Seconds for the red collapse post-processing to fade in.")]
        public float collapseVolumeFadeTime = 2f;
        [Tooltip("Red warning circle shows this long before a rock lands.")]
        public float debrisWarnTime = 0.8f;
        public float debrisSpawnRadius = 7f;
        [Range(0f, 1f)] public float debrisTargetPlayerChance = 0.3f;
        public float debrisDropHeight = 9f;
        public float debrisGravity = 30f;
        [Tooltip("A rock landing closer than this to the player hurts them.")]
        public float debrisHitRadius = 0.9f;

        [Header("Doors")]
        [Tooltip("How close the player must be for doors to react / accept E.")]
        public float doorInteractDistance = 3f;
        public float doorOpenTime = 1.5f;
        public float doorShakeIntensity = 0.08f;
        public float doorShakeDuration = 1.2f;
        [Tooltip("Seconds after the intro before the entrance slams shut.")]
        public float entranceSlamDelay = 1.5f;
        public float entranceSlamDuration = 0.18f;
        public float slamShakeIntensity = 0.6f;
        public float slamShakeDuration = 0.7f;
        public float gateOpenTime = 2f;

        [Header("Heart of Eternity")]
        public float heartPulseSpeed = 2.5f;
        [Range(0f, 1f)] public float heartPulseAmount = 0.3f;

        [Header("Hints")]
        public float hintDuration = 3f;

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
        [Tooltip("Degrees per second the explorer tips over when out of hearts.")]
        public float deathFallSpeed = 220f;

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

        [Header("Profile")]
        public int maxNameLength = 12;
        public int leaderboardSize = 5;

        [Header("Main Menu")]
        [Tooltip("How far the menu camera drifts (metres).")]
        public float menuDriftAmount = 0.12f;
        public float menuDriftSpeed = 0.12f;
        [Tooltip("How far the menu camera turns left/right while drifting (degrees).")]
        public float menuDriftYawDegrees = 0.6f;
        public float menuTorchIntensity = 2.5f;
        public float menuTorchRange = 9f;

        [Header("HUD & Screens")]
        public float floatingTextDuration = 1.1f;
        [Tooltip("How far (UI pixels) the floating +points text rises.")]
        public float floatingTextRise = 90f;
        [Range(0f, 1f)] public float damageFlashAlpha = 0.4f;
        public float damageFlashFade = 0.5f;
        public float lowFuelPulseSpeed = 6f;
        public float hintFadeTime = 0.4f;
        [Tooltip("Seconds the controls reminder shows after the intro.")]
        public float controlsHintDuration = 6f;
        public float introLineFade = 1f;
        [Tooltip("White fade before the win screen.")]
        public float winFadeTime = 1.2f;
        [Tooltip("Dark fade before the lose screen.")]
        public float loseFadeTime = 1f;

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
