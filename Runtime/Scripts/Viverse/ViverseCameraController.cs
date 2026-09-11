using UnityEngine;
using VastMetaverseTools.Avatars;

namespace VastMetaverseTools
{
    /// <summary>
    /// Creates and attaches a third-person follow camera once AvatarManager
    /// finishes loading the player's VRM avatar. Solves the issue where
    /// Viverse has no built-in player camera (unlike Spatial), leaving the
    /// world stuck on a default overview camera.
    /// </summary>
    public class ViverseCameraController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Drag the ViverseAvatarController object (with AvatarManager) here")]
        public AvatarManager avatarManager;

        [Header("Camera Settings")]
        [Tooltip("Position offset from the avatar, in the avatar's local space")]
        public Vector3 thirdPersonOffset = new Vector3(0f, 1.6f, -3f);

        [Tooltip("How quickly the camera catches up to its target position")]
        public float followSmoothness = 10f;

        [Tooltip("How quickly the camera catches up to its target rotation")]
        public float lookSmoothness = 10f;

        [Tooltip("If true, deactivates all other active cameras once this one is created")]
        public bool deactivateOtherCameras = true;

        private Camera _playerCamera;
        private Transform _avatarTransform;

        private void OnEnable()
        {
            if (avatarManager != null)
            {
                avatarManager.OnAvatarLoaded += HandleAvatarLoaded;
            }
            else
            {
                Debug.LogError("[ViverseCameraController] AvatarManager reference is not set in the Inspector.");
            }
        }

        private void OnDisable()
        {
            if (avatarManager != null)
            {
                avatarManager.OnAvatarLoaded -= HandleAvatarLoaded;
            }
        }

        private void HandleAvatarLoaded(GameObject avatar)
        {
            Debug.Log("[ViverseCameraController] Avatar loaded, setting up camera for: " + avatar.name);
            _avatarTransform = avatar.transform;

            if (_playerCamera == null)
            {
                SetupCamera();
            }
        }

        private void SetupCamera()
        {
            GameObject camObj = new GameObject("ViversePlayerCamera");
            _playerCamera = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();

            if (deactivateOtherCameras)
            {
                foreach (var cam in Camera.allCameras)
                {
                    if (cam != _playerCamera)
                    {
                        Debug.Log("[ViverseCameraController] Deactivating other camera: " + cam.gameObject.name);
                        cam.gameObject.SetActive(false);
                    }
                }
            }
        }

        private void LateUpdate()
        {
            if (_playerCamera == null || _avatarTransform == null) return;

            Vector3 desiredPosition = _avatarTransform.position + _avatarTransform.TransformDirection(thirdPersonOffset);
            _playerCamera.transform.position = Vector3.Lerp(
                _playerCamera.transform.position, desiredPosition, Time.deltaTime * followSmoothness);

            Vector3 lookTarget = _avatarTransform.position + Vector3.up * 1.5f;
            Quaternion desiredRotation = Quaternion.LookRotation(lookTarget - _playerCamera.transform.position);
            _playerCamera.transform.rotation = Quaternion.Slerp(
                _playerCamera.transform.rotation, desiredRotation, Time.deltaTime * lookSmoothness);
        }
    }
}