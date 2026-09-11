using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using VastMetaverseTools.Networking;

namespace VastMetaverseTools.Interactables
{
    public class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> All = new List<Interactable>();

        [SerializeField] private string _interactText = "Interact";
        [SerializeField] private float _interactRadius = 3f;
        [SerializeField] private float _distancePriorityOffset = 0f;
        [SerializeField] private float _visibilityRadius = 5f;
        [SerializeField] private float _verticalOffset = 0.2f;
        [SerializeField] private float _lockTime;

        [SerializeField] private UnityEvent _interactEvent;

        public event System.Action<GameObject> OnInteract = delegate { };

        private bool _canInteract = true;

        public string InteractText => _interactText;
        public float InteractRadius => _interactRadius;
        public float DistancePriorityOffset => _distancePriorityOffset;
        public float VisibilityRadius => _visibilityRadius;
        public float VerticalOffset => _verticalOffset;
        public virtual bool CanInteract => _canInteract;

        private void OnEnable()
        {
            All.Add(this);
        }

        private void OnDisable()
        {
            All.Remove(this);
        }

        public virtual void Interact(GameObject player)
        {
            if (gameObject.TryGetComponent(out NetworkSyncedObject syncObject)) syncObject.TakeOwnership();
            _interactEvent?.Invoke();
            OnInteract?.Invoke(player);
            if (_lockTime > 0)
            {
                _canInteract = false;
                Invoke(nameof(Unlock), _lockTime);
            }
        }

        protected virtual void Unlock() => _canInteract = true;

#if UNITY_EDITOR
        protected virtual void OnDrawGizmos()
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, InteractRadius);

            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * VerticalOffset, 0.1f);
        }

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.92f, 0.16f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, VisibilityRadius);
        }
#endif
    }
}