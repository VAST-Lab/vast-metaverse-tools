using UnityEngine;

namespace VastMetaverseTools.Player
{
    public class CameraManager : MonoBehaviour
    {
        public static CameraManager Instance { get; private set; }
        public static Transform CameraTransfrom => Instance._mainCamera.transform;
        public static Vector3 CamXZForward => Vector3.ProjectOnPlane(Instance._mainCamera.transform.forward, Vector3.up);

        [SerializeField] private Camera _mainCamera;
        [SerializeField] private float _fieldOfView = 80f;
        [SerializeField] private float _nearClipPlane = 0.1f;
        [SerializeField] private float _farClipPlane = 5000f;

        private bool _hasOverride;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (Camera.main != _mainCamera && Camera.main != null) _mainCamera = Camera.main;
            if (_mainCamera == null) _mainCamera = gameObject.AddComponent<Camera>();
        }

        public static void MoveCamera(Vector3 pos, Quaternion rot)
        {
            if (Instance == null || Instance._mainCamera == null)
            {
                Debug.LogWarning("CameraManager or MainCamera is not set.");
                return;
            }
            if (Instance._hasOverride) return;
            Instance._mainCamera.transform.SetPositionAndRotation(pos, rot);
        }

        // TODO: Blend/smooth movement between overrides and setup priority system for using multiple overrides
        public void SetCameraOverride(CameraOverrideTarget target)
        {
            _hasOverride = target != null;
            if (_hasOverride) _mainCamera.transform.SetPositionAndRotation(target.transform.position, target.transform.rotation);
            _mainCamera.fieldOfView = _hasOverride ? target.FieldOfView : _fieldOfView;
            _mainCamera.nearClipPlane = _hasOverride ? target.NearClipPlane : _nearClipPlane;
            _mainCamera.farClipPlane = _hasOverride ? target.FarClipPlane : _farClipPlane;
        }
    }
}