using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Supercyan.AnimalPeopleSample
{
    public class SimpleSampleCharacterControl : MonoBehaviour
    {
        private enum ControlMode
        {
            Tank,
            Direct
        }

        [SerializeField] private float m_moveSpeed = 2;
        [SerializeField] private float m_turnSpeed = 200;
        [SerializeField] private float m_jumpForce = 4;

        [SerializeField] private Animator m_animator = null;
        [SerializeField] private Rigidbody m_rigidBody = null;

        [SerializeField] private ControlMode m_controlMode = ControlMode.Direct;

        // ---- NEW: Audio fields ----
        [Header("Step Sounds")]
        [SerializeField] private AudioClip[] m_stepClips;
        [SerializeField] private float m_stepInterval = 0.5f;   // seconds between steps
        private float m_stepTimer = 0f;

        [Header("Jump & Land Sounds")]
        [SerializeField] private AudioClip m_jumpClip;
        [SerializeField] private AudioClip m_landClip;

        [Header("Animation Speed Freeze")]
        [SerializeField] private float m_animationFreezeDuration = 0.5f; // seconds speed = 0 after E/Q
        private float m_originalMoveSpeed;
        private bool m_isSpeedFrozen = false;

        private AudioSource m_audioSource;

        // ---- Existing private fields ----
        private readonly string[] m_animations = { "Pickup", "Wave" };

        private float m_currentV = 0;
        private float m_currentH = 0;

        private readonly float m_interpolation = 10;
        private readonly float m_walkScale = 0.33f;
        private readonly float m_backwardsWalkScale = 0.16f;
        private readonly float m_backwardRunScale = 0.66f;

        private bool m_wasGrounded;
        private Vector3 m_currentDirection = Vector3.zero;

        private float m_jumpTimeStamp = 0;
        private float m_minJumpInterval = 0.25f;
        private bool m_jumpInput = false;

        private bool m_isGrounded;

        private List<Collider> m_collisions = new List<Collider>();

        // ------------------------------------------------------------------
        // MonoBehaviour callbacks
        // ------------------------------------------------------------------

        private void Awake()
        {
            if (!m_animator) m_animator = GetComponent<Animator>();
            if (!m_rigidBody) m_rigidBody = GetComponent<Rigidbody>();

            // Setup audio
            m_audioSource = GetComponent<AudioSource>();
            if (m_audioSource == null)
                m_audioSource = gameObject.AddComponent<AudioSource>();
            m_audioSource.spatialBlend = 1f; // 3D sound

            m_originalMoveSpeed = m_moveSpeed;
        }

        private void OnCollisionEnter(Collision collision)
        {
            ContactPoint[] contactPoints = collision.contacts;
            for (int i = 0; i < contactPoints.Length; i++)
            {
                if (Vector3.Dot(contactPoints[i].normal, Vector3.up) > 0.5f)
                {
                    if (!m_collisions.Contains(collision.collider))
                        m_collisions.Add(collision.collider);
                    m_isGrounded = true;
                }
            }
        }

        private void OnCollisionStay(Collision collision)
        {
            ContactPoint[] contactPoints = collision.contacts;
            bool validSurfaceNormal = false;
            for (int i = 0; i < contactPoints.Length; i++)
            {
                if (Vector3.Dot(contactPoints[i].normal, Vector3.up) > 0.5f)
                {
                    validSurfaceNormal = true;
                    break;
                }
            }

            if (validSurfaceNormal)
            {
                m_isGrounded = true;
                if (!m_collisions.Contains(collision.collider))
                    m_collisions.Add(collision.collider);
            }
            else
            {
                if (m_collisions.Contains(collision.collider))
                    m_collisions.Remove(collision.collider);
                if (m_collisions.Count == 0)
                    m_isGrounded = false;
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            if (m_collisions.Contains(collision.collider))
                m_collisions.Remove(collision.collider);
            if (m_collisions.Count == 0)
                m_isGrounded = false;
        }

        private void Update()
        {
            if (!m_jumpInput && Input.GetKey(KeyCode.Space))
                m_jumpInput = true;

            // ---- E / Q with speed freeze ----
            if (Input.GetKeyDown(KeyCode.Q))
            {
                m_animator.SetTrigger(m_animations[1]); // Wave
                FreezeMovementTemporarily();
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                m_animator.SetTrigger(m_animations[0]); // Pickup
                FreezeMovementTemporarily();
            }
        }

        private void FixedUpdate()
        {
            m_animator.SetBool("Grounded", m_isGrounded);

            if (!m_isSpeedFrozen)
            {
                switch (m_controlMode)
                {
                    case ControlMode.Direct:
                        DirectUpdate();
                        break;
                    case ControlMode.Tank:
                        TankUpdate();
                        break;
                }
            }
            else
            {
                m_animator.SetFloat("MoveSpeed", 0f);
            }

            // Step sounds
            if (m_isGrounded && m_animator.GetFloat("MoveSpeed") > 0.1f)
            {
                m_stepTimer += Time.deltaTime;
                if (m_stepTimer >= m_stepInterval)
                {
                    m_stepTimer = 0f;
                    PlayRandomStepSound();
                }
            }
            else
            {
                m_stepTimer = 0f;
            }

            // Landing sound only (jump sound is played inside JumpingAndLanding)
            bool justLanded = m_isGrounded && !m_wasGrounded;
            if (justLanded && m_landClip != null)
                m_audioSource.PlayOneShot(m_landClip);

            m_wasGrounded = m_isGrounded;
            m_jumpInput = false;
        }

        // ------------------------------------------------------------------
        // Movement logic (unchanged, except using m_moveSpeed)
        // ------------------------------------------------------------------

        private void TankUpdate()
        {
            float v = Input.GetAxis("Vertical");
            float h = Input.GetAxis("Horizontal");

            bool walk = Input.GetKey(KeyCode.LeftShift);

            if (v < 0)
            {
                if (walk) v *= m_backwardsWalkScale;
                else v *= m_backwardRunScale;
            }
            else if (walk)
                v *= m_walkScale;

            m_currentV = Mathf.Lerp(m_currentV, v, Time.deltaTime * m_interpolation);
            m_currentH = Mathf.Lerp(m_currentH, h, Time.deltaTime * m_interpolation);

            transform.position += transform.forward * m_currentV * m_moveSpeed * Time.deltaTime;
            transform.Rotate(0, m_currentH * m_turnSpeed * Time.deltaTime, 0);

            m_animator.SetFloat("MoveSpeed", m_currentV);
            JumpingAndLanding(); // jump physics
        }

        private void DirectUpdate()
        {
            float v = Input.GetAxis("Vertical");
            float h = Input.GetAxis("Horizontal");

            Transform camera = Camera.main.transform;

            if (Input.GetKey(KeyCode.LeftShift))
            {
                v *= m_walkScale;
                h *= m_walkScale;
            }

            m_currentV = Mathf.Lerp(m_currentV, v, Time.deltaTime * m_interpolation);
            m_currentH = Mathf.Lerp(m_currentH, h, Time.deltaTime * m_interpolation);

            Vector3 direction = camera.forward * m_currentV + camera.right * m_currentH;

            float directionLength = direction.magnitude;
            direction.y = 0;
            direction = direction.normalized * directionLength;

            if (direction != Vector3.zero)
            {
                m_currentDirection = Vector3.Slerp(m_currentDirection, direction, Time.deltaTime * m_interpolation);

                transform.rotation = Quaternion.LookRotation(m_currentDirection);
                transform.position += m_currentDirection * m_moveSpeed * Time.deltaTime;

                m_animator.SetFloat("MoveSpeed", direction.magnitude);
            }
            else
            {
                m_animator.SetFloat("MoveSpeed", 0);
            }

            JumpingAndLanding();
        }

        private void JumpingAndLanding()
        {
            bool jumpCooldownOver = (Time.time - m_jumpTimeStamp) >= m_minJumpInterval;
            if (jumpCooldownOver && m_isGrounded && m_jumpInput)
            {
                m_jumpTimeStamp = Time.time;
                m_rigidBody.AddForce(Vector3.up * m_jumpForce, ForceMode.Impulse);

                // Play jump sound immediately
                if (m_jumpClip != null && m_audioSource != null)
                    m_audioSource.PlayOneShot(m_jumpClip);
            }
        }

        // ------------------------------------------------------------------
        // New helper methods
        // ------------------------------------------------------------------

        private void PlayRandomStepSound()
        {
            if (m_stepClips == null || m_stepClips.Length == 0) return;
            int index = Random.Range(0, m_stepClips.Length);
            m_audioSource.PlayOneShot(m_stepClips[index]);
        }

        private void FreezeMovementTemporarily()
        {
            if (m_isSpeedFrozen) return; // already frozen

            m_isSpeedFrozen = true;
            m_moveSpeed = 0f;
            StartCoroutine(UnfreezeMovementAfterDelay());
        }

        private IEnumerator UnfreezeMovementAfterDelay()
        {
            yield return new WaitForSeconds(m_animationFreezeDuration);
            m_moveSpeed = m_originalMoveSpeed;
            m_isSpeedFrozen = false;
        }
    }
}