using System;
using UnityEngine;
using VastMetaverseTools.Runtime.Networking;

namespace VastMetaverseTools.Runtime.Player
{
    public class PlayerInput : MonoBehaviour
    {
        public event Action OnPrimaryInteract = delegate { };
        public event Action OnSecondaryInteract = delegate { };
        public event Action OnJump = delegate { };

        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public bool IsLookInputActive { get; private set; }
        public bool IsRunInputActive { get; private set; }

        private NetworkSyncedObject _syncObject;

        private void Awake()
        {
            if (_syncObject == null) _syncObject = GetComponent<NetworkSyncedObject>();
        }

        private void Update()
        {
            if (_syncObject != null && !_syncObject.IsOwner) return;

            MoveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            LookInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

            IsLookInputActive = Input.GetMouseButton(0);
            IsRunInputActive = Input.GetKey(KeyCode.LeftShift);

            if (Input.GetKeyDown(KeyCode.E)) OnPrimaryInteract?.Invoke();
            if (Input.GetKeyDown(KeyCode.F)) OnSecondaryInteract?.Invoke();
            if (Input.GetButtonDown("Jump")) OnJump?.Invoke();
        }
    }
}