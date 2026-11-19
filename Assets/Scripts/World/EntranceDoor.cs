// EntranceDoor: the door the explorer came in through ("D"). It starts open (slab raised, daylight
// outside) and slams shut entranceSlamDelay seconds after the intro ends: the slab drops fast,
// then a slam sound, a big camera shake, a dust puff, and the daylight goes out. You are trapped.
using System.Collections;
using UnityEngine;

namespace LastTorch
{
    public class EntranceDoor : MonoBehaviour
    {
        public Transform slab;
        public Light outsideLight;
        public GameObject dustBurstPrefab;

        Vector3 closedPos;
        bool scheduled;

        void Awake()
        {
            closedPos = slab.localPosition;
            slab.localPosition = closedPos + Vector3.up * (GameManager.Settings.wallHeight + 0.3f);
        }

        void Start()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            gm.OnStateChanged += OnStateChanged;
            if (gm.State == GameState.Playing) Schedule();
        }

        void OnDestroy()
        {
            if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= OnStateChanged;
        }

        void OnStateChanged(GameState state)
        {
            if (state == GameState.Playing) Schedule();
        }

        void Schedule()
        {
            if (scheduled) return;
            scheduled = true;
            StartCoroutine(SlamRoutine());
        }

        IEnumerator SlamRoutine()
        {
            var s = GameManager.Settings;
            yield return new WaitForSeconds(s.entranceSlamDelay);

            // Don't trap the player in the doorway: push them one cell inside (local -Z = inside)
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                Vector3 local = transform.InverseTransformPoint(player.transform.position);
                if (Mathf.Abs(local.x) < s.cellSize * 0.5f && local.z > -1f)
                    player.Teleport(transform.TransformPoint(new Vector3(0f, 0f, -s.cellSize)));
            }

            yield return DoorMotion.Slide(slab, slab.localPosition, closedPos, s.entranceSlamDuration, accelerate: true);

            if (AudioManager.Instance != null) AudioManager.Instance.Play(SoundId.DoorSlam);
            if (CameraShake.Instance != null) CameraShake.Instance.Shake(s.slamShakeIntensity, s.slamShakeDuration);
            Effects.Spawn(dustBurstPrefab, transform.position + Vector3.up * 0.3f);
            if (outsideLight != null) outsideLight.enabled = false;
        }
    }
}
