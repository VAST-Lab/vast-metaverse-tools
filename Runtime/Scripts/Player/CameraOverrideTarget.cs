using UnityEngine;

namespace VastMetaverseTools.Player
{
    public class CameraOverrideTarget : MonoBehaviour
    {
        [SerializeField] private int _priority = 999;
        [SerializeField] private float _fieldOfView = 80f;
        [SerializeField] private float _nearClipPlane = 0.1f;
        [SerializeField] private float _farClipPlane = 5000f;
    }
}
