// Collectible: anything the explorer can pick up: Gem (score), Oil jar (+fuel), Key (Sun/Moon/
// Serpent) or the Heart. The visual bobs and spins; touching the trigger collects it, applies the
// effect, adds score, plays a sound and removes the object. Point values come from GameSettings.
// (Phase 3 adds the floating "+10" text and particle burst.)
using System;
using UnityEngine;

namespace LastTorch
{
    public enum CollectibleType { Gem, Oil, Key, Heart }

    [RequireComponent(typeof(Collider))]
    public class Collectible : MonoBehaviour
    {
        public CollectibleType type = CollectibleType.Gem;
        [Tooltip("Only used when type = Key.")]
        public KeyType keyType = KeyType.Sun;
        [Tooltip("The part that bobs and spins (the trigger itself stays still).")]
        public Transform visual;
        public bool spin = true;

        /// <summary>Raised when any collectible is picked up (used by the Heart, HUD, etc.).</summary>
        public static event Action<Collectible> AnyCollected;

        Vector3 visualBasePos;
        float bobOffset;
        bool collected;

        void Start()
        {
            if (visual == null && transform.childCount > 0) visual = transform.GetChild(0);
            if (visual != null) visualBasePos = visual.localPosition;
            bobOffset = transform.position.x * 0.37f + transform.position.z * 0.61f; // desync neighbours
        }

        void Update()
        {
            if (visual == null) return;
            var s = GameManager.Settings;
            float bob = Mathf.Sin((Time.time + bobOffset) * s.collectibleBobSpeed) * s.collectibleBobHeight;
            visual.localPosition = visualBasePos + Vector3.up * bob;
            if (spin) visual.Rotate(0f, s.collectibleSpinSpeed * Time.deltaTime, 0f, Space.World);
        }

        void OnTriggerEnter(Collider other)
        {
            if (collected) return;
            var gm = GameManager.Instance;
            if (gm != null && !gm.IsGameplayActive) return;

            var player = other.GetComponentInParent<PlayerController>();
            if (player == null) return;

            collected = true;
            Collect(player);
        }

        void Collect(PlayerController player)
        {
            var s = GameManager.Settings;
            var score = ScoreManager.Instance;
            var audio = AudioManager.Instance;

            switch (type)
            {
                case CollectibleType.Gem:
                    if (score != null) score.AddPoints(s.gemPoints, ScoreCategory.Gems);
                    if (audio != null) audio.Play(SoundId.PickupGem);
                    break;

                case CollectibleType.Oil:
                    var torch = player.GetComponent<TorchSystem>();
                    if (torch != null) torch.Refuel(s.oilRefillAmount);
                    if (score != null) score.AddPoints(s.oilPoints, ScoreCategory.Oil);
                    if (audio != null) audio.Play(SoundId.PickupOil);
                    break;

                case CollectibleType.Key:
                    var inventory = player.GetComponent<Inventory>();
                    if (inventory != null) inventory.AddKey(keyType);
                    if (score != null) score.AddPoints(s.keyPoints, ScoreCategory.Keys);
                    if (audio != null) audio.Play(SoundId.PickupKey);
                    break;

                case CollectibleType.Heart:
                    var inv = player.GetComponent<Inventory>();
                    if (inv != null) inv.TakeHeart();
                    if (score != null) score.AddPoints(s.heartPoints, ScoreCategory.Heart);
                    break;
            }

            Debug.Log($"[Collectible] Picked up {(type == CollectibleType.Key ? Inventory.KeyName(keyType) : type.ToString())}");
            AnyCollected?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
