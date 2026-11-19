// Dart: flies straight forward at dartSpeed. Each frame a short sphere-cast checks what it would hit:
// the player takes damage (pushed along the dart's direction), anything else (a wall) destroys it.
// It also disappears after dartLifetime seconds.
using UnityEngine;

namespace LastTorch
{
    public class Dart : MonoBehaviour
    {
        [HideInInspector] public PlayerHealth player;

        const float HitRadius = 0.08f;
        float age;

        void Update()
        {
            var s = GameManager.Settings;
            float step = s.dartSpeed * Time.deltaTime;
            age += Time.deltaTime;

            if (Physics.SphereCast(transform.position, HitRadius, transform.forward, out RaycastHit hit, step, ~0, QueryTriggerInteraction.Ignore))
            {
                var health = hit.collider.GetComponentInParent<PlayerHealth>();
                if (health != null) health.TakeDamage(s.trapDamage, transform.position - transform.forward);
                Destroy(gameObject);
                return;
            }

            transform.position += transform.forward * step;
            if (age > s.dartLifetime) Destroy(gameObject);
        }
    }
}
