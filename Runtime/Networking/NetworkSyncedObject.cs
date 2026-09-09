using System.Collections.Generic;
using UnityEngine;

namespace VastMetaverseTools.Runtime.Networking
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

        public bool IsOwner => true;

        public void TakeOwnership()
        {
        }
    }
}