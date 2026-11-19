// ProceduralAnimator: animates the primitive-built explorer without any animation clips.
// Legs and the free arm swing with a sine wave scaled by movement speed, the body bobs each step,
// the torch arm stays raised (the torch itself is kept upright), an idle "breathing" motion plays
// when standing still, and Flinch() makes the body jerk back when hit. All angles are in GameSettings.
using UnityEngine;

namespace LastTorch
{
    public class ProceduralAnimator : MonoBehaviour
    {
        public Transform hips;
        public Transform torso;
        public Transform leftArm;
        public Transform rightArm;
        public Transform leftLeg;
        public Transform rightLeg;
        [Tooltip("Holds the torch; kept upright every frame.")]
        public Transform torchHolder;

        PlayerController controller;
        float phase;
        float moveBlend;
        float flinchTimer;
        Vector3 hipsBasePos;
        Vector3 torsoBasePos;

        void Awake()
        {
            controller = GetComponent<PlayerController>();
            if (hips != null) hipsBasePos = hips.localPosition;
            if (torso != null) torsoBasePos = torso.localPosition;
        }

        /// <summary>Plays the short hit reaction.</summary>
        public void Flinch() { flinchTimer = GameManager.Settings.flinchDuration; }

        void LateUpdate()
        {
            var s = GameManager.Settings;
            float dt = Time.deltaTime;
            float speed = controller != null ? controller.CurrentSpeed : 0f;

            // moveBlend: 0 = standing, 1 = walking, >1 = sprinting
            float targetBlend = Mathf.Clamp(speed / Mathf.Max(0.01f, s.walkSpeed), 0f, s.sprintSpeed / Mathf.Max(0.01f, s.walkSpeed));
            moveBlend = Mathf.MoveTowards(moveBlend, targetBlend, s.animBlendSpeed * dt);
            float swingAmount = Mathf.Clamp01(moveBlend);

            // One full cycle = two strides (left + right)
            phase += speed * dt / Mathf.Max(0.01f, 2f * s.strideLength) * Mathf.PI * 2f;
            float swing = Mathf.Sin(phase);

            if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(swing * s.legSwingAngle * swingAmount, 0f, 0f);
            if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(-swing * s.legSwingAngle * swingAmount, 0f, 0f);
            if (leftArm != null) leftArm.localRotation = Quaternion.Euler(-swing * s.armSwingAngle * swingAmount, 0f, 0f);
            if (rightArm != null)
                rightArm.localRotation = Quaternion.Euler(-s.torchArmRaiseAngle + swing * s.armSwingAngle * s.torchArmSwingFactor * swingAmount, 0f, 0f);

            // Body bob: peaks twice per cycle (once per step)
            if (hips != null)
                hips.localPosition = hipsBasePos + Vector3.up * (Mathf.Abs(swing) * s.bodyBobHeight * swingAmount);

            // Idle breathing when standing still
            float idle = 1f - swingAmount;
            float breathWave = Mathf.Sin(Time.time * s.breathSpeed * Mathf.PI * 2f) * idle;
            float breath = breathWave * s.breathAmount;

            // Flinch: lean back sharply, then recover
            float flinch = 0f;
            if (flinchTimer > 0f)
            {
                flinchTimer -= dt;
                flinch = s.flinchAngle * Mathf.Clamp01(flinchTimer / Mathf.Max(0.01f, s.flinchDuration));
            }

            if (torso != null)
            {
                torso.localPosition = torsoBasePos + Vector3.up * breath;
                float lean = s.moveLeanAngle * moveBlend - flinch;
                float sway = breathWave * s.breathSwayAngle;
                torso.localRotation = Quaternion.Euler(lean, 0f, sway);
            }

            // Keep the torch pointing straight up whatever the arm is doing
            if (torchHolder != null)
                torchHolder.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        }
    }
}
