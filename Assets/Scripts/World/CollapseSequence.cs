// CollapseSequence: the escape phase after the Heart is taken. Starts a countdown (collapseDuration),
// fades in the red collapse post-processing, keeps the camera shaking, plays the rumble loop, opens
// the ExitGate and drops FallingDebris near the player (debrisPerSecond, some aimed at the player).
// If the countdown reaches 0 the player loses: "The temple sealed itself forever."
using UnityEngine;
using UnityEngine.Rendering;

namespace LastTorch
{
    public class CollapseSequence : MonoBehaviour
    {
        public static CollapseSequence Instance { get; private set; }

        public Volume collapseVolume;
        public ExitGate exitGate;
        public GameObject debrisPrefab;

        public bool IsActive { get; private set; }
        public float TimeLeft { get; private set; }

        PlayerHealth player;
        float spawnAccumulator;

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        void Start()
        {
            player = FindAnyObjectByType<PlayerHealth>();
            if (collapseVolume != null) collapseVolume.weight = 0f;
            if (GameManager.Instance != null) GameManager.Instance.OnGameOver += OnGameOver;
            TimeLeft = GameManager.Settings.collapseDuration;
        }

        public void Begin()
        {
            if (IsActive) return;
            var s = GameManager.Settings;
            IsActive = true;
            TimeLeft = s.collapseDuration;

            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.BeginCollapse();
                gm.ShowHint("The temple is collapsing! Find the way out!", 4f);
            }
            if (CameraShake.Instance != null) CameraShake.Instance.SetContinuous(s.collapseShakeIntensity);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayLoop(SoundId.CollapseRumble);
            if (exitGate != null) exitGate.Open();
        }

        void Update()
        {
            if (!IsActive) return;
            var s = GameManager.Settings;

            if (collapseVolume != null)
                collapseVolume.weight = Mathf.MoveTowards(collapseVolume.weight, 1f, Time.deltaTime / Mathf.Max(0.01f, s.collapseVolumeFadeTime));

            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Collapse) return;

            TimeLeft -= Time.deltaTime;

            spawnAccumulator += s.debrisPerSecond * Time.deltaTime;
            while (spawnAccumulator >= 1f)
            {
                spawnAccumulator -= 1f;
                SpawnDebris(s);
            }

            if (TimeLeft <= 0f)
            {
                TimeLeft = 0f;
                gm.Lose("The temple sealed itself forever.");
            }
        }

        void SpawnDebris(GameSettings s)
        {
            if (debrisPrefab == null || player == null) return;
            Transform root = player.transform;

            for (int attempt = 0; attempt < 5; attempt++)
            {
                float radius = Random.value < s.debrisTargetPlayerChance ? 1f : s.debrisSpawnRadius;
                Vector2 r = Random.insideUnitCircle * radius;
                Vector3 p = root.position + new Vector3(r.x, 0f, r.y);
                Vector3 from = p + Vector3.up * (s.wallHeight + 5f);

                if (!Physics.Raycast(from, Vector3.down, out RaycastHit hit, s.wallHeight + 10f, ~0, QueryTriggerInteraction.Ignore))
                    continue;

                Vector3 ground;
                if (hit.collider.transform.IsChildOf(root)) ground = new Vector3(p.x, root.position.y, p.z);
                else if (hit.point.y < 0.5f) ground = hit.point; // floor, not the top of a wall
                else continue;

                var go = Instantiate(debrisPrefab);
                var debris = go.GetComponent<FallingDebris>();
                if (debris != null) debris.Init(ground, player);
                return;
            }
        }

        void OnGameOver(bool won, string reason)
        {
            if (CameraShake.Instance != null) CameraShake.Instance.SetContinuous(0f);
        }
    }
}
