using UnityEngine;
using System.Collections;

namespace Supercyan.AnimalPeopleSample
{
    public class FreeCameraLogic : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform m_target = null;

        [Header("Camera Parameters")]
        [SerializeField] private float m_height = 1.5f;
        [SerializeField] private float m_defaultDistance = 4f;
        [SerializeField] private float m_minDistance = 1f;
        [SerializeField] private float m_maxDistance = 10f;

        [Header("Rotation")]
        [SerializeField] private float m_mouseSensitivity = 2f;
        [SerializeField] private float m_minPitch = -30f;
        [SerializeField] private float m_maxPitch = 60f;
        [SerializeField] private float m_defaultPitch = 15f;

        [Header("Zoom")]
        [SerializeField] private float m_zoomSpeed = 2f;

        [Header("Smooth Reset")]
        [SerializeField] private float m_resetSmoothTime = 0.5f;

        // Current camera state (directly controlled by mouse & zoom)
        private float m_currentYaw;
        private float m_currentPitch;
        private float m_currentDistance;

        // Reset coroutine reference (to avoid overlapping resets)
        private Coroutine m_resetRoutine = null;

        private void Start()
        {
            if (m_target == null)
            {
                Debug.LogError("FreeCameraLogic: No target assigned!");
                enabled = false;
                return;
            }

            // Lock and hide cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Initialise from current transform or defaults
            Vector3 angles = transform.eulerAngles;
            m_currentYaw = angles.y;
            m_currentPitch = ClampPitch(angles.x);
            m_currentDistance = m_defaultDistance;
        }

        private void Update()
        {
            if (m_target == null) return;

            // 1) Mouse look – always active
            float deltaX = Input.GetAxis("Mouse X") * m_mouseSensitivity;
            float deltaY = Input.GetAxis("Mouse Y") * m_mouseSensitivity;

            m_currentYaw += deltaX;
            m_currentPitch -= deltaY;
            m_currentPitch = ClampPitch(m_currentPitch);

            // 2) Zoom with mouse wheel
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                m_currentDistance -= scroll * m_zoomSpeed;
                m_currentDistance = Mathf.Clamp(m_currentDistance, m_minDistance, m_maxDistance);
            }

            // 3) Manual reset with Middle Mouse Button
            if (Input.GetMouseButtonDown(2))
            {
                // Cancel any ongoing reset to avoid conflicts
                if (m_resetRoutine != null)
                    StopCoroutine(m_resetRoutine);
                m_resetRoutine = StartCoroutine(SmoothResetCoroutine());
            }
        }

        private void LateUpdate()
        {
            if (m_target == null) return;

            // Apply current yaw/pitch/distance directly (no extra smoothing here)
            Vector3 lookAtPoint = m_target.position + Vector3.up * m_height;
            Quaternion rotation = Quaternion.Euler(m_currentPitch, m_currentYaw, 0f);
            Vector3 desiredPosition = lookAtPoint - rotation * Vector3.forward * m_currentDistance;

            transform.position = desiredPosition;
            transform.LookAt(lookAtPoint);
        }

        private IEnumerator SmoothResetCoroutine()
        {
            // Target values: behind player (player's forward yaw), default pitch, default distance
            float startYaw = m_currentYaw;
            float startPitch = m_currentPitch;
            float startDistance = m_currentDistance;

            float targetYaw = m_target.eulerAngles.y;
            float targetPitch = m_defaultPitch;
            float targetDistance = m_defaultDistance;

            float elapsed = 0f;
            float yawVel = 0f, pitchVel = 0f, distVel = 0f;

            while (elapsed < m_resetSmoothTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / m_resetSmoothTime; // 0 → 1

                // SmoothStep for nicer easing
                float smoothT = t * t * (3f - 2f * t);

                m_currentYaw = Mathf.SmoothDamp(startYaw, targetYaw, ref yawVel, m_resetSmoothTime, Mathf.Infinity, Time.deltaTime);
                m_currentPitch = Mathf.SmoothDamp(startPitch, targetPitch, ref pitchVel, m_resetSmoothTime, Mathf.Infinity, Time.deltaTime);
                m_currentDistance = Mathf.SmoothDamp(startDistance, targetDistance, ref distVel, m_resetSmoothTime, Mathf.Infinity, Time.deltaTime);

                yield return null;
            }

            // Snap to final values exactly (avoids floating point leftovers)
            m_currentYaw = targetYaw;
            m_currentPitch = targetPitch;
            m_currentDistance = targetDistance;

            m_resetRoutine = null;
        }

        private float ClampPitch(float pitch)
        {
            pitch = (pitch + 360f) % 360f;
            if (pitch > 180f) pitch -= 360f;
            return Mathf.Clamp(pitch, m_minPitch, m_maxPitch);
        }
    }
}