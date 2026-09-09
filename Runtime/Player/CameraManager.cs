using UnityEngine;

namespace VastMetaverseTools.Runtime.Player
{
    public class CameraManager : MonoBehaviour
    {
        public static CameraManager Instance { get; private set; }
        public static Transform CameraTransfrom => Instance._mainCamera.transform;
        public static Vector3 CamXZForward => Vector3.ProjectOnPlane(Instance._mainCamera.transform.forward, Vector3.up);

        [SerializeField] private Camera _mainCamera;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            // temp fix for testing
            var spatialChar = FindObjectOfType<CharacterController>();
            if (spatialChar != null)
            {
                Destroy(spatialChar.gameObject);
            }
            if (_mainCamera == null)
            {
                _mainCamera = gameObject.AddComponent<Camera>();
            }
        }

        public static void MoveCamera(Vector3 pos, Quaternion rot)
        {
            if (Instance == null || Instance._mainCamera == null)
            {
                Debug.LogWarning("CameraManager or MainCamera is not set.");
                return;
            }
            Instance._mainCamera.transform.SetPositionAndRotation(pos, rot);
        }
    }
}