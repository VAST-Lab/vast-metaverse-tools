using System;
using UnityEngine;
using UnityEngine.InputSystem;
using VastMetaverseTools.Networking;

namespace VastMetaverseTools.Player
{
    public class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private bool _debugInput;

        public event Action PrimaryInteract = delegate { };
        public event Action SecondaryInteract = delegate { };
        public event Action Jump = delegate { };

        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public float ZoomInput { get; private set; }
        public bool IsTouching { get; private set; }
        public bool IsRunInputActive { get; private set; }

        private NetworkSyncedObject _syncObject;
        private bool _isInputEnabled = true;

        private bool CanProcessInput => _isInputEnabled && (_syncObject == null || _syncObject.IsOwner);

        private void Awake()
        {
            if (_syncObject == null) _syncObject = GetComponent<NetworkSyncedObject>();
        }

        public void SetInputMapping(InputActionAsset mapping)
        {
            if (TryGetComponent(out PlayerInput playerInput))
            {
                playerInput.actions = mapping;
            }
        }

        public void SetInputEnabled(bool isEnabled)
        {
            _isInputEnabled = isEnabled;
            if (!isEnabled)
            {
                MoveInput = Vector2.zero;
                LookInput = Vector2.zero;
                IsTouching = false;
                ZoomInput = 0f;
                IsRunInputActive = false;
            }
        }

        private void OnMove(InputValue value)
        {
            if (!CanProcessInput) return;
            MoveInput = value.Get<Vector2>();
            if (_debugInput) Debug.Log($"MoveInput: {MoveInput}");
        }

        private void OnLook(InputValue value)
        {
            if (!CanProcessInput) return;
            LookInput = value.Get<Vector2>();
            if (_debugInput && IsTouching) Debug.Log($"LookInput: {LookInput}");
        }

        private void OnTouch(InputValue value)
        {
            if (!CanProcessInput) return;
            IsTouching = value.isPressed;
            if (_debugInput) Debug.Log($"IsTouching: {IsTouching}");
        }

        private void OnJump(InputValue value)
        {
            if (!CanProcessInput) return;
            if (value.Get<float>() > 0.5f)
            {
                Jump?.Invoke();
                if (_debugInput) Debug.Log("Jump");
            }
        }

        private void OnZoom(InputValue value)
        {
            if (!CanProcessInput) return;
            ZoomInput = value.Get<float>();
            if (_debugInput) Debug.Log($"ZoomInput: {ZoomInput}");
        }

        private void OnInteractPrimary(InputValue value)
        {
            if (!CanProcessInput) return;
            if (value.isPressed)
            {
                PrimaryInteract?.Invoke();
                if (_debugInput) Debug.Log("PrimaryInteract");
            }
        }

        private void OnInteractSecondary(InputValue value)
        {
            if (!CanProcessInput) return;
            if (value.isPressed)
            {
                SecondaryInteract?.Invoke();
                if (_debugInput) Debug.Log("PrimaryInteract");
            }
        }
    }
}