using UnityEngine;

namespace VastMetaverseTools.Runtime.Helper
{
    public class MaintainOffset : MonoBehaviour
    {
        [SerializeField] private Transform _trackedObject;
        [SerializeField] private Vector3 _worldOffset;

        private void Update()
        {
            transform.position = _trackedObject.position + _worldOffset;
        }
    }
}
