// PlayerController: moves the explorer with a CharacterController. WASD/arrows move relative to
// the camera, Shift sprints, gravity keeps them on the floor, and the body turns smoothly toward
// the move direction. Plays a footstep every stride. Only moves while the game is Playing/Collapse.
// Other scripts read CurrentSpeed/IsSprinting (animation, torch) and call AddKnockback (traps).
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastTorch
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Tooltip("Camera used for camera-relative movement. Defaults to Camera.main.")]
        public Transform cameraTransform;

        public float CurrentSpeed { get; private set; }
        public bool IsSprinting { get; private set; }
        public bool IsGrounded => controller != null && controller.isGrounded;

        CharacterController controller;
        float verticalVelocity;
        Vector3 knockback;
        float stepDistance;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        void Start()
        {
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        }

        void Update()
        {
            var s = GameManager.Settings;
            var gm = GameManager.Instance;
            bool active = gm == null || gm.IsGameplayActive;
            float dt = Time.deltaTime;

            // 1. Read input
            Vector2 input = active ? ReadMoveInput() : Vector2.zero;
            var kb = Keyboard.current;
            bool sprintHeld = active && kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);

            // 2. Convert to a camera-relative direction on the ground plane
            Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;
            forward.y = 0f; right.y = 0f;
            forward.Normalize(); right.Normalize();

            Vector3 move = forward * input.y + right * input.x;
            if (move.sqrMagnitude > 1f) move.Normalize();

            bool moving = move.sqrMagnitude > 0.01f;
            IsSprinting = sprintHeld && moving;
            float speed = IsSprinting ? s.sprintSpeed : s.walkSpeed;

            // 3. Turn toward the move direction
            if (moving)
            {
                Quaternion targetRot = Quaternion.LookRotation(move, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 1f - Mathf.Exp(-s.rotationSpeed * dt));
            }

            // 4. Gravity
            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = s.groundStickForce;
            verticalVelocity += s.gravity * dt;

            // 5. Knockback fades out over time
            knockback = Vector3.Lerp(knockback, Vector3.zero, 1f - Mathf.Exp(-s.knockbackDecay * dt));

            Vector3 velocity = move * speed + knockback + Vector3.up * verticalVelocity;
            controller.Move(velocity * dt);

            Vector3 flat = controller.velocity;
            flat.y = 0f;
            CurrentSpeed = flat.magnitude;

            // 6. Footsteps every stride
            if (active && controller.isGrounded && moving)
            {
                stepDistance += CurrentSpeed * dt;
                if (stepDistance >= s.strideLength)
                {
                    stepDistance -= s.strideLength;
                    if (AudioManager.Instance != null)
                        AudioManager.Instance.PlayAt(SoundId.Footstep, transform.position, s.footstepVolume,
                            Random.Range(s.footstepPitchMin, s.footstepPitchMax));
                }
            }
        }

        static Vector2 ReadMoveInput()
        {
            var kb = Keyboard.current;
            if (kb == null) return Vector2.zero;
            float x = 0f, y = 0f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) x -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) y -= 1f;
            return new Vector2(x, y);
        }

        /// <summary>Pushes the player horizontally (e.g. when hit by a trap).</summary>
        public void AddKnockback(Vector3 force)
        {
            force.y = 0f;
            knockback += force;
        }

        /// <summary>Moves the player instantly (CharacterController must be disabled to teleport).</summary>
        public void Teleport(Vector3 position)
        {
            controller.enabled = false;
            transform.position = position;
            controller.enabled = true;
            verticalVelocity = 0f;
            knockback = Vector3.zero;
        }
    }
}
