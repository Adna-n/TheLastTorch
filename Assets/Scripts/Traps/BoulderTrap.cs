// BoulderTrap: sits on the trigger tile ("b"). The first time the player steps on it, the boulder
// waiting at "B" starts rolling along the corridor at boulderSpeed (spinning, with a rumble). Being
// hit costs a heart and a strong knockback. At the end wall it shatters into rubble with a crash.
// One-shot: it never resets.
using UnityEngine;

namespace LastTorch
{
    public class BoulderTrap : MonoBehaviour
    {
        public Transform boulder;
        public float radius = 1.5f;
        [Tooltip("World direction the boulder rolls in.")]
        public Vector3 direction = Vector3.right;
        [Tooltip("Where the boulder stops and shatters.")]
        public Vector3 endPoint;
        public AudioSource rumble;
        public GameObject rubblePrefab;
        public GameObject dustBurstPrefab;

        enum State { Waiting, Rolling, Done }

        State state;
        PlayerHealth player;
        PlayerController playerController;

        void Start()
        {
            player = FindAnyObjectByType<PlayerHealth>();
            if (player != null) playerController = player.GetComponent<PlayerController>();
        }

        void Update()
        {
            var s = GameManager.Settings;
            if (state == State.Waiting) WaitForTrigger(s);
            else if (state == State.Rolling) Roll(s);
        }

        void WaitForTrigger(GameSettings s)
        {
            if (player == null) return;
            var gm = GameManager.Instance;
            if (gm != null && !gm.IsGameplayActive) return;
            Vector3 local = transform.InverseTransformPoint(player.transform.position);
            float half = s.cellSize * 0.5f;
            if (Mathf.Abs(local.x) < half && Mathf.Abs(local.z) < half)
            {
                state = State.Rolling;
                if (rumble != null) rumble.Play();
                if (gm != null) gm.ShowHint("RUN!", 1.5f);
                if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.15f, 0.6f);
            }
        }

        void Roll(GameSettings s)
        {
            float step = s.boulderSpeed * Time.deltaTime;
            Vector3 toEnd = endPoint - boulder.position;
            toEnd.y = 0f;
            bool arrived = toEnd.magnitude <= step;
            boulder.position = arrived ? new Vector3(endPoint.x, boulder.position.y, endPoint.z) : boulder.position + direction * step;

            // Spin around the axis perpendicular to the rolling direction
            Vector3 axis = Vector3.Cross(Vector3.up, direction);
            boulder.Rotate(axis, step / Mathf.Max(0.01f, radius) * Mathf.Rad2Deg, Space.World);

            if (player != null && DoorMotion.FlatDistance(player.transform.position, boulder.position) < radius + 0.35f)
            {
                if (player.TakeDamage(s.trapDamage, boulder.position) && playerController != null)
                    playerController.AddKnockback(direction * s.knockbackForce * (s.boulderKnockbackMultiplier - 1f));
            }

            if (arrived) Shatter(s);
        }

        void Shatter(GameSettings s)
        {
            state = State.Done;
            if (rumble != null) rumble.Stop();
            Vector3 ground = new Vector3(boulder.position.x, 0f, boulder.position.z);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayAt(SoundId.DoorSlam, ground);
            if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.3f, 0.5f);
            Effects.Spawn(dustBurstPrefab, ground + Vector3.up * 0.5f);
            if (rubblePrefab != null) Instantiate(rubblePrefab, ground, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            boulder.gameObject.SetActive(false);
        }
    }
}
