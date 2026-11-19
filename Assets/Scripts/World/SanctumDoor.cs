// SanctumDoor: the Inner Sanctum door ("H") with three key sockets (Sun, Moon, Serpent).
// Near the door, press E to place every key you carry: each socket lights up with a chime.
// When all three are placed the door grinds open. Otherwise a hint lists the missing keys.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastTorch
{
    public class SanctumDoor : MonoBehaviour
    {
        public Transform slab;
        [Tooltip("Socket renderers in KeyType order: Sun, Moon, Serpent.")]
        public Renderer[] sockets = new Renderer[3];
        public Material socketOnMaterial;

        public bool IsOpen { get; private set; }

        readonly bool[] placed = new bool[3];
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
            if (inRange && !wasInRange && gm != null) gm.ShowHint(PromptText());
            if (inRange && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) PlaceKeys();
            wasInRange = inRange;
        }

        bool HasUnplacedKey()
        {
            for (int i = 0; i < 3; i++)
                if (!placed[i] && inventory != null && inventory.HasKey((KeyType)i)) return true;
            return false;
        }

        /// <summary>Names of keys that are neither placed nor carried.</summary>
        string MissingKeys()
        {
            var names = new List<string>();
            for (int i = 0; i < 3; i++)
                if (!placed[i] && (inventory == null || !inventory.HasKey((KeyType)i))) names.Add(Inventory.KeyName((KeyType)i));
            if (names.Count == 0) return string.Empty;
            if (names.Count == 1) return names[0];
            return string.Join(", ", names.GetRange(0, names.Count - 1)) + " and " + names[names.Count - 1];
        }

        string PromptText()
        {
            if (HasUnplacedKey()) return "Press E to place your keys in the sockets.";
            return $"Sealed. Needs the {MissingKeys()}.";
        }

        void PlaceKeys()
        {
            int newlyPlaced = 0;
            for (int i = 0; i < 3; i++)
            {
                if (placed[i] || inventory == null || !inventory.HasKey((KeyType)i)) continue;
                placed[i] = true;
                newlyPlaced++;
                if (sockets[i] != null && socketOnMaterial != null) sockets[i].sharedMaterial = socketOnMaterial;
            }

            var gm = GameManager.Instance;
            if (newlyPlaced > 0 && AudioManager.Instance != null) AudioManager.Instance.Play(SoundId.PickupKey);

            if (placed[0] && placed[1] && placed[2]) Open();
            else if (gm != null) gm.ShowHint(newlyPlaced > 0 ? $"Still missing: {MissingKeys()}." : PromptText());
        }

        void Open()
        {
            IsOpen = true;
            var s = GameManager.Settings;
            Vector3 closed = slab.localPosition;
            StartCoroutine(DoorMotion.Slide(slab, closed, closed + Vector3.down * (s.wallHeight + 0.2f), s.doorOpenTime));
            if (AudioManager.Instance != null) AudioManager.Instance.PlayAt(SoundId.DoorGrind, transform.position);
            if (CameraShake.Instance != null) CameraShake.Instance.Shake(s.doorShakeIntensity, s.doorShakeDuration);
            if (GameManager.Instance != null) GameManager.Instance.ShowHint("The Inner Sanctum opens...");
        }
    }
}
