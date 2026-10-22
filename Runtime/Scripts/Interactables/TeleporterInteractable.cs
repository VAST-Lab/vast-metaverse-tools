using UnityEngine;
using VastMetaverseTools.Managers;
using VastMetaverseTools.Player;
using VastMetaverseTools.UI;


namespace VastMetaverseTools
{
    public class TeleporterInteractable : MonoBehaviour
    {
        [SerializeField] private Transform _targetLocation;

        public void Teleport()
        {
            MetaverseManager.LocalPlayer.TeleportTo(_targetLocation);
        }

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
