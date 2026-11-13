using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using VastMetaverseTools.Networking;

namespace VastMetaverseTools.Player
{
    [RequireComponent(typeof(NetworkSyncedObject))]
    public class PlayerController : MonoBehaviour
    {
        // TODO: Add Gravity / Falling Gravity Multipliers
        // TODO: Add Variable Jump Height checkbox and Max Jump Count (Double jump, etc.)
        // TODO: Add Ground friction and air control
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _runSpeed = 8f;
        [SerializeField] private float _jumpHeight = 2f;
        [SerializeField] private float _sprintJumpMultiplier = 1.25f;
        [SerializeField] private float _airControlSpeed = 2f;
        [SerializeField] private float _rotationSpeed = 10f;
        [SerializeField] private float _animationBlendSpeed = 5f;

        [Header("Camera")]
        [SerializeField] private bool _followBehindPlayer = true;
        [SerializeField] private bool _followOnlyWhenMoving = true;
        [SerializeField] private float _cameraReturnDelay = 2f;
        [SerializeField] private float _cameraReturnSpeed = 2f;
        [SerializeField] private float _maxReturnSpeed = 120f;
        [SerializeField] private float _mouseSensitivity = 2f;
        [Tooltip("Off: hold left mouse and drag to turn the camera. On: the cursor is hidden and moving the mouse turns the camera.")]
        [SerializeField] private bool _cameraLock;
        [SerializeField] private float _zoomSpeed = 2f;
        [SerializeField] private float _zoomSmoothSpeed = 10f;
        [SerializeField] private float _minZoom = 0f;
        [SerializeField] private float _maxZoom = 10f;
        [SerializeField] private float _tpsMinZoom = 2f;
        [SerializeField] private float _cameraCollisionRadius = 0.2f;
        [SerializeField] private LayerMask _cameraCollisionLayers = 1;

        [Header("Ground")]
        [SerializeField] private float _groundCheckOffset = 0.1f;
        [SerializeField] private float _groundCheckRadius = 0.3f;
        [SerializeField] private LayerMask _groundLayers = 1;

        [Header("References")]
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private PlayerAnimationController _animator;
        [SerializeField] private Rigidbody _rb;
        [SerializeField] private Transform _cameraTarget;

        public NetworkSyncedObject SyncedObject { get; private set; }
        public PlayerInputReader InputReader => _input;
        public PlayerAnimationController AnimController => _animator;
        public event System.Action<Vector3, Quaternion> OnTeleported;

        private float _currentZoom = 5f;
        private float _currentAnimationBlend;
        private float _yaw;
        private float _pitch;
        private float _targetZoom = 5f;
        private float _lastManualRotationTime;
        private float _cameraReturnProgress;
        private bool _isCameraFollowing;
        private bool _isMoving;
        private bool _lockMovement;
        private bool _isGrounded;
        private bool _cursorReleased;
        private bool _isCursorLocked;
        private float _sensitivityMultiplier = 1f;

        public const string CameraLockPrefKey = "CameraLock";
        public const string SensitivityPrefKey = "CameraSensitivity";
        public bool CameraLock => _cameraLock;

        private void Awake()
        {
            SyncedObject = GetComponent<NetworkSyncedObject>();

            if (_input == null) _input = GetComponent<PlayerInputReader>();
            if (_input == null) _input = GetComponentInChildren<PlayerInputReader>();
            if (_animator == null) _animator = GetComponent<PlayerAnimationController>();
            if (_animator == null) _animator = GetComponentInChildren<PlayerAnimationController>();
            if (_rb == null) _rb = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            _yaw = transform.eulerAngles.y;
            _cameraLock = PlayerPrefs.GetInt(CameraLockPrefKey, _cameraLock ? 1 : 0) == 1;
            _sensitivityMultiplier = PlayerPrefs.GetFloat(SensitivityPrefKey, 1f);
        }

        private void OnEnable()
        {
            if (_input != null) _input.Jump += OnJump;
        }

        private void OnDisable()
        {
            if (_input != null) _input.Jump -= OnJump;
            SetCursorLocked(false);
        }

        private void Update()
        {
            if (!SyncedObject.IsOwner) return;
            HandleCursorLock();
            HandleCameraInput();
        }

        private void FixedUpdate()
        {
            if (!SyncedObject.IsOwner) return;
            CheckGrounded();
            HandleMovement();
        }

        private void LateUpdate()
        {
            if (!SyncedObject.IsOwner) return;
            UpdateCamera();
        }

        public void TogglePlayerVisualsShown() => SetPlayerVisualsShown(!_animator.gameObject.activeSelf);
        public void SetPlayerVisualsShown(bool shown)
        {
            _animator.gameObject.SetActive(shown);
        }

        public void TeleportTo(Transform t) => TeleportTo(t.position, t.rotation);
        public void TeleportTo(Vector3 position) => TeleportTo(position, transform.rotation);
        public void TeleportTo(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            _yaw = rotation.eulerAngles.y;
            if (_rb != null)
            {
                _rb.position = position;
                _rb.rotation = rotation;
                if (!_rb.isKinematic)
                {
                    _rb.linearVelocity = Vector3.zero;
                    _rb.angularVelocity = Vector3.zero;
                }
                _rb.Sleep();
            }
            Physics.SyncTransforms();
            OnTeleported?.Invoke(position, rotation);
        }

        // Scales the prefab's Mouse Sensitivity, so each project keeps its own base feel.
        public void SetSensitivityMultiplier(float multiplier) => _sensitivityMultiplier = multiplier;

        public void SetCameraLock(bool locked)
        {
            _cameraLock = locked;
            _cursorReleased = false;
        }

        public void LockMovement(bool locked) => _lockMovement = locked;
        public void LockEverything(bool locked)
        {
            _lockMovement = locked;
            _rb.isKinematic = locked;
            _animator.SetMoveSpeed(0f);
        }

        private void CheckGrounded()
        {
            Vector3 spherePosition = transform.position + (Vector3.up * _groundCheckOffset);
            _isGrounded = Physics.CheckSphere(spherePosition, _groundCheckRadius, _groundLayers, QueryTriggerInteraction.Ignore);

            if (_animator != null) _animator.SetGrounded(_isGrounded);
        }

        // With camera lock on, the cursor stays hidden and centered. Esc frees it
        // (e.g. to click the UI); clicking back into the world locks it again.
        private void HandleCursorLock()
        {
            if (!_cameraLock || _lockMovement)
            {
                SetCursorLocked(false);
                return;
            }

            // The editor and browsers can free the cursor themselves (e.g. on Esc).
            if (_isCursorLocked && Cursor.lockState != CursorLockMode.Locked) _cursorReleased = true;

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                _cursorReleased = true;
            }
            else if (_cursorReleased && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame
                && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
            {
                _cursorReleased = false;
            }

            SetCursorLocked(!_cursorReleased);
        }

        private void SetCursorLocked(bool locked)
        {
            if (_isCursorLocked == locked) return;
            _isCursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void HandleCameraInput()
        {
            if (_input == null) return;
            Vector2 moveInput = _input.MoveInput;
            if (_followOnlyWhenMoving) _isCameraFollowing = moveInput.magnitude >= 0.1f;
            else if (!_isCameraFollowing && moveInput.magnitude >= 0.1f) _isCameraFollowing = true;

            if (_input.IsTouching || _isCursorLocked)
            {
                Vector2 lookDelta = _input.LookInput * (_mouseSensitivity * _sensitivityMultiplier);
                _yaw += lookDelta.x;
                _pitch -= lookDelta.y;
                _pitch = Mathf.Clamp(_pitch, -70f, 70f);

                _isCameraFollowing = false;
                _lastManualRotationTime = Time.time;
                _cameraReturnProgress = 0f;
            }
            else if (_followBehindPlayer && _isCameraFollowing && Time.time > _lastManualRotationTime + _cameraReturnDelay)
            {
                _cameraReturnProgress += Time.deltaTime * _cameraReturnSpeed;
                float currentSpeed = Mathf.SmoothStep(0f, _maxReturnSpeed, Mathf.Clamp01(_cameraReturnProgress));

                float targetYaw = _rb.rotation.eulerAngles.y;
                _yaw = Mathf.MoveTowardsAngle(_yaw, targetYaw, currentSpeed * Time.deltaTime);
            }
            else
            {
                _cameraReturnProgress = 0f;
            }

            float scroll = Mathf.Clamp(_input.ZoomInput, -1f, 1f);
            if (Mathf.Abs(scroll) > 0.01f)
            {
                _targetZoom -= scroll * _zoomSpeed;

                if (_targetZoom > 0f && _targetZoom < _tpsMinZoom)
                {
                    _targetZoom = scroll > 0f ? 0f : _tpsMinZoom;
                }

                _targetZoom = Mathf.Clamp(_targetZoom, _minZoom, _maxZoom);
            }

            _currentZoom = Mathf.Lerp(_currentZoom, _targetZoom, Time.deltaTime * _zoomSmoothSpeed);
        }

        private void HandleMovement()
        {
            if (_lockMovement || _input == null || _rb.isKinematic) return;

            Vector3 inputDirection = new Vector3(_input.MoveInput.x, 0f, _input.MoveInput.y).normalized;
            _isMoving = inputDirection.magnitude >= 0.1f;

            float targetSpeed = 0f;
            float targetBlend = 0f;

            if (_isMoving)
            {
                bool isRunning = _input.IsRunInputActive;
                targetSpeed = isRunning ? _runSpeed : _moveSpeed;
                targetBlend = isRunning ? 1f : 0.5f;

                float targetAngle = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + _yaw;
                Vector3 moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

                if (_isGrounded)
                {
                    _rb.linearVelocity = new Vector3(moveDirection.x * targetSpeed, _rb.linearVelocity.y, moveDirection.z * targetSpeed);
                }
                else
                {
                    Vector3 targetVelocity = new Vector3(moveDirection.x * targetSpeed, _rb.linearVelocity.y, moveDirection.z * targetSpeed);
                    _rb.linearVelocity = Vector3.Lerp(_rb.linearVelocity, targetVelocity, Time.fixedDeltaTime * _airControlSpeed);
                }

                Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
                _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, targetRotation, _rotationSpeed * Time.fixedDeltaTime));
            }
            else if (_isGrounded)
            {
                _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, 0f);
            }

            _rb.angularVelocity = Vector3.zero;

            if (_animator != null)
            {
                _currentAnimationBlend = Mathf.MoveTowards(_currentAnimationBlend, targetBlend, _animationBlendSpeed * Time.fixedDeltaTime);
                _animator.SetMoveSpeed(_currentAnimationBlend);
            }
        }

        private void OnJump()
        {
            if (_rb.isKinematic || _lockMovement || !_isGrounded) return;

            float currentJumpHeight = _jumpHeight;

            if (_input.IsRunInputActive)
            {
                currentJumpHeight *= _sprintJumpMultiplier;
            }

            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, Mathf.Sqrt(currentJumpHeight * -2f * Physics.gravity.y), _rb.linearVelocity.z);

            if (_animator != null) _animator.Jump();
        }

        private void UpdateCamera()
        {
            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 direction = rotation * Vector3.back;
            Vector3 desiredPosition = _cameraTarget.position + direction * _currentZoom;

            if (_currentZoom > 0f)
            {
                if (Physics.SphereCast(_cameraTarget.position, _cameraCollisionRadius, direction, out RaycastHit hit, _currentZoom, _cameraCollisionLayers, QueryTriggerInteraction.Ignore))
                {
                    desiredPosition = _cameraTarget.position + direction * hit.distance;
                }
            }

            CameraManager.MoveCamera(desiredPosition, rotation);
        }
    }
}