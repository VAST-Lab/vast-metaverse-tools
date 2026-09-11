using UnityEngine;
using UnityEngine.Events;
using VastMetaverseTools.Player;

namespace VastMetaverseTools.Interactables
{
    public class Teleporter : MonoBehaviour
    {
        [SerializeField] private Transform _targetLocation;
        [SerializeField] private UnityEvent _onTeleportEvent;

        private Collider _collider;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            if (_collider != null) _collider.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out PlayerController player))
            {
                player.TeleportTo(_targetLocation);
                _onTeleportEvent?.Invoke();
            }
        }

        /*
        [SerializeField] private SpatialAvatarTeleporter _spatial;

        private void OnValidate()
        {
            _spatial = GetComponent<SpatialAvatarTeleporter>();
            if (_spatial != null)
            {
                _targetLocation = _spatial.targetLocation;
            }
        }
        */

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (_targetLocation == null) return;
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, _targetLocation.position);
        }
#endif
    }
}