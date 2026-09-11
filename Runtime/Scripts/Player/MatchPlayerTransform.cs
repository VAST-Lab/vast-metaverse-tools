using UnityEngine;

namespace VastMetaverseTools.Player
{
    public class MatchPlayerTransform : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private float _yOffset = 0.25f;

        private void Start()
        {
            if (_target == null) _target = transform;
        }

        private void LateUpdate()
        {
            if (LocalPlayer.Instance == null) return;

            var camForward = Camera.main.transform.forward;
            var camProjected = Vector3.ProjectOnPlane(camForward, Vector3.up);

            var pos = LocalPlayer.Instance.transform.position + Vector3.up * _yOffset - camProjected;
            transform.SetPositionAndRotation(pos, Quaternion.LookRotation(camProjected));
        }
    }
}