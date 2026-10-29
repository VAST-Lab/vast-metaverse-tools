using System;
using System.Collections.Generic;
using UnityEngine;

namespace VastMetaverseTools.Networking
{
    public class NetworkSyncedObject : MonoBehaviour
    {
        [SerializeField] private bool _syncTransform;
        [SerializeField] private bool _syncRigidbody;
        [SerializeField] private bool _syncAnimator;
        [SerializeField] private bool _saveWithSpace;
        [SerializeField] private bool _destroyOnCreatorDisconnect;
        [SerializeField] private bool _destroyOnOwnerDisconnect;
        [SerializeField] private List<string> _syncVariables;

        public bool SyncTransform => _syncTransform;
        public bool SyncRigidbody => _syncRigidbody;
        public bool SyncAnimator => _syncAnimator;
        public bool SaveWithSpace => _saveWithSpace;
        public bool DestroyOnCreatorDisconnect => _destroyOnCreatorDisconnect;
        public bool DestroyOnOwnerDisconnect => _destroyOnOwnerDisconnect;
        public IReadOnlyList<string> SyncVariables => _syncVariables;

        public bool IsOwner { get; private set; } = true;
        public bool IsNetworked { get; private set; }

        public event Action<NetworkSyncedObject> OnOwnershipRequested;

        public void TakeOwnership()
        {
            if (!IsNetworked)
            {
                IsOwner = true;
                return;
            }
            OnOwnershipRequested?.Invoke(this);
        }

        public void SetNetworkState(bool isNetworked, bool isOwner)
        {
            IsNetworked = isNetworked;
            IsOwner = isOwner;
        }
    }
}