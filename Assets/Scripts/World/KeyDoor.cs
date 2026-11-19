// KeyDoor: a stone door that needs one symbol key (door "A" needs the Sun Key). When the player
// comes within doorInteractDistance: if they carry the key, the slab grinds down into the floor
// (sound + small camera shake); otherwise a hint says "Sealed. Needs the Sun Key."
using UnityEngine;

namespace LastTorch
{
    public class KeyDoor : MonoBehaviour
    {
        public KeyType requiredKey = KeyType.Sun;
        public Transform slab;

        public bool IsOpen { get; private set; }

        Transform player;
        Inventory inventory;
        bool wasInRange;

        void Start()
        {
            var pc = FindAnyObjectByType<PlayerController>();
            if (pc != null) { player = pc.transform; inventory = pc.GetComponent<Inventory>(); }
        }

        void Update()
        {
            if (IsOpen || player == null) return;
            var gm = GameManager.Instance;
            if (gm != null && !gm.IsGameplayActive) return;

            bool inRange = DoorMotion.FlatDistance(player.position, transform.position) <= GameManager.Settings.doorInteractDistance;
            if (inRange)
            {
                if (inventory != null && inventory.HasKey(requiredKey)) Open();
                else if (!wasInRange && gm != null) gm.ShowHint($"Sealed. Needs the {Inventory.KeyName(requiredKey)}.");
            }
            wasInRange = inRange;
        }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            var s = GameManager.Settings;
            Vector3 closed = slab.localPosition;
            StartCoroutine(DoorMotion.Slide(slab, closed, closed + Vector3.down * (s.wallHeight + 0.2f), s.doorOpenTime));
            if (AudioManager.Instance != null) AudioManager.Instance.PlayAt(SoundId.DoorGrind, transform.position);
            if (CameraShake.Instance != null) CameraShake.Instance.Shake(s.doorShakeIntensity, s.doorShakeDuration);
            if (GameManager.Instance != null) GameManager.Instance.ShowHint($"The {Inventory.KeyName(requiredKey)} opens the door.");
        }
    }
}
