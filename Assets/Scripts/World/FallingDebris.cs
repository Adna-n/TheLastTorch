// FallingDebris: one rock dropped by the collapse. First a red warning circle pulses on the floor
// (debrisWarnTime), then the rock falls from above and spins. On impact it hurts the player if they
// are within debrisHitRadius, plays a crash, puffs dust and disappears.
using UnityEngine;

namespace LastTorch
{
    public class FallingDebris : MonoBehaviour
    {
        [Tooltip("The visible rock (hidden during the warning).")]
        public Transform rock;
        public GameObject warningPrefab;
        public GameObject dustBurstPrefab;

        enum Phase { Warning, Falling }

        Phase phase;
        Vector3 ground;
        PlayerHealth player;
        GameObject warning;
        Vector3 warningScale;
        float timer;
        float fallSpeed;
        Vector3 spin;

        /// <summary>Called right after spawning: where the rock will land and who it can hit.</summary>
        public void Init(Vector3 groundPoint, PlayerHealth target)
        {
            var s = GameManager.Settings;
            ground = groundPoint;
            player = target;
            transform.position = ground + Vector3.up * s.debrisDropHeight;
            transform.rotation = Random.rotation;
            spin = Random.insideUnitSphere * 360f;
            if (rock != null) rock.gameObject.SetActive(false);

            if (warningPrefab != null)
            {
                warning = Instantiate(warningPrefab, ground + Vector3.up * 0.03f, Quaternion.identity);
                warningScale = Vector3.one * (s.debrisHitRadius * 2.6f);
                warning.transform.localScale = warningScale;
            }
        }

        void Update()
        {
            var s = GameManager.Settings;
            float dt = Time.deltaTime;

            if (phase == Phase.Warning)
            {
                timer += dt;
                if (warning != null)
                    warning.transform.localScale = warningScale * (1f + 0.15f * Mathf.Sin(timer * 25f));
                if (timer >= s.debrisWarnTime)
                {
                    phase = Phase.Falling;
                    if (rock != null) rock.gameObject.SetActive(true);
                }
                return;
            }

            fallSpeed += s.debrisGravity * dt;
            transform.position += Vector3.down * fallSpeed * dt;
            transform.Rotate(spin * dt, Space.World);

            if (transform.position.y <= ground.y + 0.3f) Impact(s);
        }

        void Impact(GameSettings s)
        {
            if (player != null && DoorMotion.FlatDistance(player.transform.position, ground) <= s.debrisHitRadius)
                player.TakeDamage(1, ground);

            if (AudioManager.Instance != null) AudioManager.Instance.PlayAt(SoundId.DoorSlam, ground, 0.5f, Random.Range(1.3f, 1.7f));
            Effects.Spawn(dustBurstPrefab, ground + Vector3.up * 0.2f);
            if (warning != null) Destroy(warning);
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (warning != null) Destroy(warning);
        }
    }
}
