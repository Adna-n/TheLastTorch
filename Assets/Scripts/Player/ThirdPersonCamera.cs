// ThirdPersonCamera: orbits the camera around the player with the mouse (yaw + clamped pitch),
// looking over the shoulder. A SphereCast from the player toward the camera pulls it in front of
// walls so it never clips through them. Locks the cursor while playing and adds CameraShake.Offset.
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastTorch
{
    [RequireComponent(typeof(Camera))]
    public class ThirdPersonCamera : MonoBehaviour
    {
        public Transform target;
        [Tooltip("Layers that block the camera. The player's own colliders are always ignored.")]
        public LayerMask collisionMask = ~0;

        public float Yaw => yaw;

        float yaw;
        float pitch;
        float currentDistance;
        bool cursorLocked;
        CameraShake shake;
        readonly RaycastHit[] hits = new RaycastHit[16];

        void Start()
        {
            shake = GetComponent<CameraShake>();
            var s = GameManager.Settings;
            pitch = s.startPitch;
            currentDistance = s.cameraDistance;
            if (target != null) yaw = target.eulerAngles.y;
            SetCursorLocked(false);
        }

        void LateUpdate()
        {
            if (target == null) return;
            var s = GameManager.Settings;
            var gm = GameManager.Instance;
            bool active = gm == null || gm.IsGameplayActive;
            float dt = Time.unscaledDeltaTime;

            if (active != cursorLocked) SetCursorLocked(active);

            // 1. Mouse orbit
            if (active && Mouse.current != null)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                float sensitivity = s.mouseSensitivity * PlayerOptions.Current.mouseSensitivity;
                yaw += delta.x * sensitivity;
                pitch -= delta.y * sensitivity;
            }
            pitch = Mathf.Clamp(pitch, s.minPitch, s.maxPitch);

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

            // 2. Pivot = head height + shoulder offset (cast from the head so the offset can't enter a wall)
            Vector3 head = target.position + Vector3.up * s.cameraPivotHeight;
            Vector3 shoulderDir = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
            float shoulder = CastDistance(head, shoulderDir, s.shoulderOffset, s.cameraCollisionRadius);
            Vector3 pivot = head + shoulderDir * shoulder;

            // 3. Pull the camera in front of any wall between the pivot and the desired position
            Vector3 back = rotation * Vector3.back;
            float wanted = CastDistance(pivot, back, s.cameraDistance, s.cameraCollisionRadius);
            wanted = Mathf.Max(wanted, s.cameraMinDistance);

            // Snap in instantly (no clipping), ease back out smoothly
            if (wanted < currentDistance) currentDistance = wanted;
            else currentDistance = Mathf.Lerp(currentDistance, wanted, 1f - Mathf.Exp(-s.cameraReturnSpeed * dt));

            Vector3 offset = shake != null ? shake.Offset : Vector3.zero;
            transform.SetPositionAndRotation(pivot + back * currentDistance + offset, rotation);
        }

        /// <summary>How far a sphere can travel from origin along dir before hitting something (ignoring the player).</summary>
        float CastDistance(Vector3 origin, Vector3 dir, float maxDistance, float radius)
        {
            int count = Physics.SphereCastNonAlloc(origin, radius, dir, hits, maxDistance, collisionMask, QueryTriggerInteraction.Ignore);
            float best = maxDistance;
            Transform root = target.root;
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (hit.collider.transform.IsChildOf(root)) continue;
                if (hit.distance <= 0f) continue; // sphere started inside something: ignore
                if (hit.distance < best) best = hit.distance;
            }
            return best;
        }

        void SetCursorLocked(bool locked)
        {
            cursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
