using UnityEngine;
using VastMetaverseTools.Runtime.Interactables;
using VastMetaverseTools.Runtime.Networking;

namespace VastMetaverseTools.Runtime.Player
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private Transform _cameraTarget;
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _runSpeed = 8f;
        [SerializeField] private float _jumpHeight = 2f;
        [SerializeField] private float _sprintJumpMultiplier = 1.25f;
        [SerializeField] private float _airControlSpeed = 2f;
        [SerializeField] private float _rotationSpeed = 10f;
        [SerializeField] private float _animationBlendSpeed = 5f;
        [SerializeField] private bool _followBehindPlayer = true;
        [SerializeField] private bool _followOnlyWhenMoving = true;
        [SerializeField] private float _cameraReturnDelay = 2f;
        [SerializeField] private float _cameraReturnSpeed = 2f;
        [SerializeField] private float _maxReturnSpeed = 120f;
        [SerializeField] private float _mouseSensitivity = 2f;
        [SerializeField] private float _zoomSpeed = 2f;
        [SerializeField] private float _zoomSmoothSpeed = 10f;
        [SerializeField] private float _minZoom = 0f;
        [SerializeField] private float _maxZoom = 10f;
        [SerializeField] private float _tpsMinZoom = 2f;
        [SerializeField] private float _cameraCollisionRadius = 0.2f;
        [SerializeField] private LayerMask _cameraCollisionLayers = 1;

        [SerializeField] private float _groundCheckOffset = 0.1f;
        [SerializeField] private float _groundCheckRadius = 0.3f;
        [SerializeField] private LayerMask _groundLayers = 1;

        private Rigidbody _rb;
        private PlayerInput _input;
        private PlayerAnimationController _animator;
        private NetworkSyncedObject _syncObject;
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

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _input = GetComponent<PlayerInput>();
            _animator = GetComponent<PlayerAnimationController>();
            _syncObject = GetComponent<NetworkSyncedObject>();

            if (_syncObject == null || _syncObject.IsOwner)
            {
                LocalPlayer.Instance = gameObject;
                LocalPlayer.Controller = this;
                LocalPlayer.Input = _input;
                LocalPlayer.Interactor = GetComponent<PlayerInteractor>();
            }
        }

        private void OnEnable()
        {
            if (_input != null) _input.OnJump += OnJump;
        }

        private void OnDisable()
        {
            if (_input != null) _input.OnJump -= OnJump;
        }

        private void Update()
        {
            if (_syncObject != null && !_syncObject.IsOwner) return;
            HandleCameraInput();
        }

        private void FixedUpdate()
        {
            if (_syncObject != null && !_syncObject.IsOwner) return;
            CheckGrounded();
            HandleMovement();
        }

        private void LateUpdate()
        {
            UpdateCamera();
        }

        public void TeleportTo(Transform t) => TeleportTo(t.position, t.rotation);
        public void TeleportTo(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            _rb.velocity = Vector3.zero;
            _rb.Sleep();
        }

        public void LockMovement(bool locked) => _lockMovement = locked;
        public void LockEverything(bool locked)
        {
            _lockMovement = locked;
            _rb.isKinematic = locked;
        }

        private void CheckGrounded()
        {
            Vector3 spherePosition = transform.position + (Vector3.up * _groundCheckOffset);
            _isGrounded = Physics.CheckSphere(spherePosition, _groundCheckRadius, _groundLayers, QueryTriggerInteraction.Ignore);

            if (_animator != null) _animator.SetGrounded(_isGrounded);
        }

        private void HandleCameraInput()
        {
            if (_input == null) return;
            Vector2 moveInput = _input.MoveInput;
            if (_followOnlyWhenMoving) _isCameraFollowing = moveInput.magnitude >= 0.1f;
            else if (!_isCameraFollowing && moveInput.magnitude >= 0.1f) _isCameraFollowing = true;

            if (_input.IsLookInputActive)
            {
                Vector2 lookDelta = _input.LookInput * _mouseSensitivity;
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

            float scroll = Input.mouseScrollDelta.y;
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
                    _rb.velocity = new Vector3(moveDirection.x * targetSpeed, _rb.velocity.y, moveDirection.z * targetSpeed);
                }
                else
                {
                    Vector3 targetVelocity = new Vector3(moveDirection.x * targetSpeed, _rb.velocity.y, moveDirection.z * targetSpeed);
                    _rb.velocity = Vector3.Lerp(_rb.velocity, targetVelocity, Time.fixedDeltaTime * _airControlSpeed);
                }

                Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
                _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, targetRotation, _rotationSpeed * Time.fixedDeltaTime));
            }
            else if (_isGrounded)
            {
                _rb.velocity = new Vector3(0f, _rb.velocity.y, 0f);
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

            _rb.velocity = new Vector3(_rb.velocity.x, Mathf.Sqrt(currentJumpHeight * -2f * Physics.gravity.y), _rb.velocity.z);

            if (_animator != null) _animator.Jump();
        }

        private void UpdateCamera()
        {
            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 direction = rotation * Vector3.back;
            Vector3 desiredPosition = _cameraTarget.position + direction * _currentZoom;

            if (_currentZoom > 0f)
            {
                if (Physics.SphereCast(_cameraTarget.position, _cameraCollisionRadius, direction, out RaycastHit hit, _currentZoom, _cameraCollisionLayers))
                {
                    desiredPosition = _cameraTarget.position + direction * hit.distance;
                }
            }

            CameraManager.MoveCamera(desiredPosition, rotation);
        }
    }
}