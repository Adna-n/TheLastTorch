// DartTrap: a pressure plate. When the player steps on it the plate sinks, and after dartFireDelay
// seconds darts shoot out of the launchers in the side walls, across the corridor. Then it waits
// dartCooldown seconds before it can fire again. The plate rises when the player steps off.
using System.Collections;
using UnityEngine;

namespace LastTorch
{
    public class DartTrap : MonoBehaviour
    {
        public Transform plate;
        [Tooltip("Wall launchers (each fires along its own forward/+Z axis). Set by LevelBuilder.")]
        public Transform[] launchers = new Transform[0];
        public GameObject dartPrefab;

        PlayerHealth player;
        Vector3 plateUp;
        float cooldown;
        bool pressed;

        void Start()
        {
            player = FindAnyObjectByType<PlayerHealth>();
            if (plate != null) plateUp = plate.localPosition;
        }

        void Update()
        {
            var s = GameManager.Settings;
            cooldown -= Time.deltaTime;

            bool onPlate = false;
            if (player != null)
            {
                Vector3 local = transform.InverseTransformPoint(player.transform.position);
                float half = s.cellSize * 0.4f;
                onPlate = Mathf.Abs(local.x) < half && Mathf.Abs(local.z) < half && local.y < 1f;
            }

            var gm = GameManager.Instance;
            bool active = gm == null || gm.IsGameplayActive;
            if (onPlate && !pressed && cooldown <= 0f && active)
            {
                cooldown = s.dartCooldown;
                StartCoroutine(Fire(s));
            }
            pressed = onPlate;

            if (plate != null)
            {
                Vector3 target = plateUp + Vector3.down * (onPlate ? s.dartPlateSinkDepth : 0f);
                plate.localPosition = Vector3.MoveTowards(plate.localPosition, target, Time.deltaTime);
            }
        }

        IEnumerator Fire(GameSettings s)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayAt(SoundId.SpikeUp, transform.position, 0.6f, 0.7f); // plate click
            yield return new WaitForSeconds(s.dartFireDelay);
            if (dartPrefab == null) yield break;
            foreach (var launcher in launchers)
            {
                if (launcher == null) continue;
                var go = Instantiate(dartPrefab, launcher.position + launcher.forward * 0.35f, launcher.rotation);
                var dart = go.GetComponent<Dart>();
                if (dart != null) dart.player = player;
            }
            if (AudioManager.Instance != null) AudioManager.Instance.PlayAt(SoundId.DartWhoosh, transform.position);
        }
    }
}
