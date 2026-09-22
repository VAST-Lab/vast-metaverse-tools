using System;
using UnityEngine;
using UnityEngine.InputSystem;
using VastMetaverseTools.Networking;

namespace VastMetaverseTools.Player
{
    public class PlayerInputReader : MonoBehaviour
    {
        public event Action PrimaryInteract = delegate { };
        public event Action SecondaryInteract = delegate { };
        public event Action Jump = delegate { };

        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public float ZoomInput { get; private set; }
        public bool IsLookInputActive { get; private set; }
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
                ZoomInput = 0f;
                IsLookInputActive = false;
                IsRunInputActive = false;
            }
        }

        private void OnMove(InputValue value)
        {
            if (!CanProcessInput) return;
            MoveInput = value.Get<Vector2>();
        }

        private void OnLook(InputValue value)
        {
            if (!CanProcessInput) return;
            LookInput = value.Get<Vector2>();
            IsLookInputActive = LookInput.sqrMagnitude > 0.01f;
        }

        private void OnJump(InputValue value)
        {
            if (!CanProcessInput) return;
            if (value.Get<float>() > 0.5f) Jump?.Invoke();
        }

        private void OnScroll(InputValue value)
        {
            if (!CanProcessInput) return;
            ZoomInput = value.Get<float>();
        }

        private void OnInteractPrimary(InputValue value)
        {
            if (!CanProcessInput) return;
            if (value.isPressed) PrimaryInteract?.Invoke();
        }

        private void OnInteractSecondary(InputValue value)
        {
            if (!CanProcessInput) return;
            if (value.isPressed) SecondaryInteract?.Invoke();
        }
    }
}