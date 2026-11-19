// SpikeTrap: a floor tile whose spikes cycle down -> warn (they poke up a little with a click) ->
// up (touching them hurts) -> down. Each tile's timing is offset by its position, so a row of spike
// tiles moves like a wave. All timings come from GameSettings (spikeDownTime/WarnTime/UpTime).
using UnityEngine;

namespace LastTorch
{
    public class SpikeTrap : MonoBehaviour
    {
        [Tooltip("Parent of all spike meshes; moved up and down.")]
        public Transform spikes;

        enum Phase { Down, Warn, Up }

        const float HiddenDepth = -0.86f; // spikes fully below the floor surface (spike mesh is ~0.84 m tall)

        Phase phase = Phase.Down;
        PlayerHealth player;
        float timeOffset;

        void Start()
        {
            player = FindAnyObjectByType<PlayerHealth>();
            var s = GameManager.Settings;
            float cells = (transform.position.x + transform.position.z) / Mathf.Max(0.01f, s.cellSize);
            timeOffset = cells * s.spikeWaveOffset;
            if (spikes != null) spikes.localPosition = new Vector3(0f, HiddenDepth, 0f);
        }

        void Update()
        {
            var s = GameManager.Settings;
            float period = s.spikeDownTime + s.spikeWarnTime + s.spikeUpTime;
            float t = Mathf.Repeat(Time.time + timeOffset, Mathf.Max(0.01f, period));

            Phase next = t < s.spikeDownTime ? Phase.Down : t < s.spikeDownTime + s.spikeWarnTime ? Phase.Warn : Phase.Up;
            if (next != phase)
            {
                phase = next;
                if (phase != Phase.Down) PlaySound(phase == Phase.Up ? 1f : 0.5f, s);
            }

            float targetY = phase == Phase.Down ? HiddenDepth : phase == Phase.Warn ? HiddenDepth + s.spikeWarnRise : 0f;
            if (spikes != null)
            {
                var p = spikes.localPosition;
                p.y = Mathf.MoveTowards(p.y, targetY, s.spikeMoveSpeed * Time.deltaTime);
                spikes.localPosition = p;
            }

            // Hurt the player standing on the tile while the spikes are (mostly) up
            if (phase == Phase.Up && player != null && spikes != null && spikes.localPosition.y > HiddenDepth * 0.4f)
            {
                Vector3 local = transform.InverseTransformPoint(player.transform.position);
                float half = s.cellSize * 0.45f;
                if (Mathf.Abs(local.x) < half && Mathf.Abs(local.z) < half && local.y < 1f)
                    player.TakeDamage(s.trapDamage, transform.position);
            }
        }

        void PlaySound(float volume, GameSettings s)
        {
            if (AudioManager.Instance == null || player == null) return;
            if (DoorMotion.FlatDistance(player.transform.position, transform.position) > s.spikeSoundDistance) return;
            AudioManager.Instance.PlayAt(SoundId.SpikeUp, transform.position, volume, Random.Range(0.9f, 1.1f));
        }
    }
}
