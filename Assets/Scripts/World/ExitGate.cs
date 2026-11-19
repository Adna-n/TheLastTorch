// ExitGate: the sealed way out ("X"). When the collapse begins, the slab rises and daylight fades
// in behind it. Walking into the trigger beyond the gate = WIN: the escape and torch bonuses are
// added to the score, then GameManager.Win(). (The white fade-out is added in Phase 6.)
using System.Collections;
using UnityEngine;

namespace LastTorch
{
    public class ExitGate : MonoBehaviour
    {
        public Transform slab;
        public Light daylight;

        public bool IsOpen { get; private set; }

        float daylightIntensity;
        bool escaped;

        void Awake()
        {
            if (daylight != null)
            {
                daylightIntensity = daylight.intensity;
                daylight.enabled = false;
            }
        }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            var s = GameManager.Settings;
            Vector3 closed = slab.localPosition;
            StartCoroutine(DoorMotion.Slide(slab, closed, closed + Vector3.up * (s.wallHeight + 0.2f), s.gateOpenTime));
            StartCoroutine(FadeInDaylight(s.gateOpenTime));
            if (AudioManager.Instance != null) AudioManager.Instance.PlayAt(SoundId.DoorGrind, transform.position);
        }

        IEnumerator FadeInDaylight(float duration)
        {
            if (daylight == null) yield break;
            daylight.enabled = true;
            for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(0.01f, duration))
            {
                daylight.intensity = daylightIntensity * t;
                yield return null;
            }
            daylight.intensity = daylightIntensity;
        }

        // Called for the child "WinTrigger" collider (this object has a kinematic Rigidbody)
        void OnTriggerEnter(Collider other)
        {
            if (!IsOpen || escaped) return;
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null) return;
            var gm = GameManager.Instance;
            if (gm != null && gm.IsGameOver) return;

            escaped = true;
            var collapse = CollapseSequence.Instance;
            var torch = player.GetComponent<TorchSystem>();
            if (ScoreManager.Instance != null)
                ScoreManager.Instance.ApplyEscapeBonuses(collapse != null ? collapse.TimeLeft : 0f, torch != null ? torch.Fuel : 0f);
            if (gm != null) gm.Win("You escaped with the Heart of Eternity!");
        }
    }
}
