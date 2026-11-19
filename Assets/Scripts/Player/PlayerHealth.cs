// PlayerHealth: the explorer's hearts. TakeDamage(amount, sourcePosition) removes hearts, knocks the
// player away from the source, gives a short invincibility window (the model blinks), shakes the
// camera, plays the hurt sound and subtracts the damage penalty from the score.
// At 0 hearts the player loses ("The temple claimed another explorer.").
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastTorch
{
    public class PlayerHealth : MonoBehaviour
    {
        [Tooltip("Root of the visual model (its renderers blink while invincible).")]
        public Transform model;

        public int Hearts { get; private set; }
        public bool IsDead { get; private set; }
        public bool IsInvincible => invincibleTimer > 0f;

        /// <summary>(currentHearts, maxHearts)</summary>
        public event Action<int, int> OnHealthChanged;
        /// <summary>Fired on every hit (HUD red flash).</summary>
        public event Action OnDamaged;

        PlayerController controller;
        ProceduralAnimator animator;
        Renderer[] blinkRenderers;
        float invincibleTimer;
        float blinkTimer;
        bool visible = true;

        void Awake()
        {
            controller = GetComponent<PlayerController>();
            animator = GetComponent<ProceduralAnimator>();
            Hearts = GameManager.Settings.maxHearts;

            // Blink the body only, not the torch flame particles
            var list = new List<Renderer>();
            foreach (var r in (model != null ? model : transform).GetComponentsInChildren<Renderer>(true))
                if (!(r is ParticleSystemRenderer)) list.Add(r);
            blinkRenderers = list.ToArray();
        }

        void Start()
        {
            OnHealthChanged?.Invoke(Hearts, GameManager.Settings.maxHearts);
        }

        /// <summary>Returns true if the damage was applied (false while invincible / not playing).</summary>
        public bool TakeDamage(int amount, Vector3 sourcePosition)
        {
            var gm = GameManager.Instance;
            if (IsDead || IsInvincible || amount <= 0) return false;
            if (gm != null && !gm.IsGameplayActive) return false;

            var s = GameManager.Settings;
            Hearts = Mathf.Max(0, Hearts - amount);
            invincibleTimer = s.invincibilitySeconds;
            blinkTimer = 0f;

            if (ScoreManager.Instance != null) ScoreManager.Instance.AddPenalty(s.damagePenalty * amount);

            Vector3 away = transform.position - sourcePosition;
            away.y = 0f;
            if (away.sqrMagnitude < 0.001f) away = -transform.forward;
            if (controller != null) controller.AddKnockback(away.normalized * s.knockbackForce);

            if (animator != null) animator.Flinch();
            if (CameraShake.Instance != null) CameraShake.Instance.Shake(s.hitShakeIntensity, s.hitShakeDuration);
            if (AudioManager.Instance != null) AudioManager.Instance.Play(SoundId.Hurt);

            OnDamaged?.Invoke();
            OnHealthChanged?.Invoke(Hearts, s.maxHearts);

            if (Hearts == 0)
            {
                IsDead = true;
                invincibleTimer = 0f;
                SetVisible(true);
                if (gm != null) gm.Lose("The temple claimed another explorer.");
            }
            return true;
        }

        void Update()
        {
            if (invincibleTimer <= 0f) return;

            invincibleTimer -= Time.deltaTime;
            blinkTimer -= Time.deltaTime;
            if (blinkTimer <= 0f)
            {
                blinkTimer = GameManager.Settings.blinkInterval;
                SetVisible(!visible);
            }
            if (invincibleTimer <= 0f) SetVisible(true);
        }

        void SetVisible(bool value)
        {
            visible = value;
            foreach (var r in blinkRenderers)
                if (r != null) r.enabled = value;
        }
    }
}
