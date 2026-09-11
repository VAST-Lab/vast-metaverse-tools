using AvatarLibrary;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UniVRM10;
using UniVRM10.VRM10Viewer;
using VastMetaverseTools.Attachments;
using VastMetaverseTools.Player;

namespace VastMetaverseTools.Avatars
{
    public class AvatarLoader : MonoBehaviour
    {
        [SerializeField] private PlayerAnimationController _animController;
        [SerializeField] private PlayerAttachmentManager _playerAttachmentManager;
        [SerializeField] private GameObject _defaultAvatar;
        [SerializeField] private Transform _defaultAvatarHatSocket;
        [SerializeField] private Transform _defaultAvatarHandSocket;
        [SerializeField] private RuntimeAnimatorController _animatorController;
        [SerializeField] private Transform _lookAtTarget;

        private GameObject _currentAvatar;
        private Vrm10Instance _currentVrmInstance;
        private VRM10Blinker _blinker;
        private VRM10AIUEO _lipSync;
        private VRM10AutoExpression _autoExpression;

        private const string AVATAR_API_URL = "https://sdk-api.viverse.com/api/meetingareaselector/v1/newgenavatar/getavatarlist";

        [System.Serializable]
        private class AvatarData
        {
            public long id;
            public string VrmBinaryDataUrl;
            public bool IsEncrypted;
            public bool IsDisabled;
        }

        [System.Serializable]
        private class AvatarListData
        {
            public long CurrentAvatarId;
            public AvatarData[] Avatars;
        }

        [System.Serializable]
        private class AvatarApiResponse
        {
            public AvatarListData data;
        }

        private void Start()
        {
            if (_defaultAvatar.TryGetComponent(out Animator animator))
            {
                if (_animController != null) _animController.SetAnimator(animator);
                if (_playerAttachmentManager != null)
                {
                    // TODO: Find bones in rig and set. Also need to do this for SetupAvatar()
                    _playerAttachmentManager.SetAttachmentTransform(AttachmentLocation.RightHand, _defaultAvatarHandSocket);
                    _playerAttachmentManager.SetAttachmentTransform(AttachmentLocation.Hat, _defaultAvatarHatSocket);
                }
            }
        }

        public void LoadAvatarReference(AvatarReference avatarData)
        {
            var avatar = Instantiate(avatarData.Prefab);
            SetupAvatar(avatar);
        }

        public void LoadViverseAvatar(string accessToken)
        {
            if (!string.IsNullOrEmpty(accessToken))
            {
                StartCoroutine(FetchAndLoadViverseAvatar(accessToken));
            }
        }

        private IEnumerator FetchAndLoadViverseAvatar(string accessToken)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(AVATAR_API_URL))
            {
                webRequest.SetRequestHeader("accesstoken", accessToken);
                webRequest.SetRequestHeader("Content-Type", "application/json");
                webRequest.timeout = 30;

                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    ParseAndDownloadAvatar(webRequest.downloadHandler.text);
                }
            }
        }

        private void ParseAndDownloadAvatar(string jsonResponse)
        {
            AvatarApiResponse response = JsonUtility.FromJson<AvatarApiResponse>(jsonResponse);

            if (response?.data?.Avatars != null && response.data.Avatars.Length > 0)
            {
                AvatarData targetAvatar = null;

                if (response.data.CurrentAvatarId > 0)
                {
                    foreach (AvatarData avatar in response.data.Avatars)
                    {
                        if (avatar.id == response.data.CurrentAvatarId && !string.IsNullOrEmpty(avatar.VrmBinaryDataUrl))
                        {
                            targetAvatar = avatar;
                            break;
                        }
                    }
                }

                if (targetAvatar == null)
                {
                    foreach (AvatarData avatar in response.data.Avatars)
                    {
                        if (!string.IsNullOrEmpty(avatar.VrmBinaryDataUrl) && !avatar.IsDisabled)
                        {
                            targetAvatar = avatar;
                            break;
                        }
                    }
                }

                if (targetAvatar != null)
                {
                    StartCoroutine(DownloadVRMCoroutine(targetAvatar));
                }
            }
        }

        private IEnumerator DownloadVRMCoroutine(AvatarData avatarData)
        {
            string encryptionIV = null;

            using (UnityWebRequest webRequest = UnityWebRequest.Get(avatarData.VrmBinaryDataUrl))
            {
                webRequest.timeout = 60;
                yield return webRequest.SendWebRequest();

                if (webRequest.result != UnityWebRequest.Result.Success) yield break;

                var responseHeaders = webRequest.GetResponseHeaders();
                if (responseHeaders != null && responseHeaders.ContainsKey(SimpleAvatarEncryptUtility.ResponseHeader))
                {
                    encryptionIV = responseHeaders[SimpleAvatarEncryptUtility.ResponseHeader];
                }

                byte[] vrmData = webRequest.downloadHandler.data;

                if (avatarData.IsEncrypted || !string.IsNullOrEmpty(encryptionIV))
                {
                    bool decryptionComplete = false;
                    byte[] decryptedData = null;

                    SimpleAvatarEncryptUtility.AsyncDecryptBinaryData(vrmData, encryptionIV, (result) =>
                    {
                        decryptedData = result;
                        decryptionComplete = true;
                    });

                    while (!decryptionComplete)
                    {
                        yield return null;
                    }

                    vrmData = decryptedData;
                }

                LoadVRMFromBytes(vrmData);
            }
        }

        private async void LoadVRMFromBytes(byte[] vrmData)
        {
            Vrm10Instance vrmInstance = await Vrm10.LoadBytesAsync(vrmData, canLoadVrm0X: true);
            if (vrmInstance != null)
            {
                if (_currentVrmInstance != null)
                {
                    Destroy(_currentVrmInstance.gameObject);
                }
                SetupAvatar(vrmInstance.gameObject);
            }
        }

        private void SetupAvatar(GameObject avatarGameObject)
        {
            if (_defaultAvatar != null) Destroy(_defaultAvatar);
            if (_currentAvatar != null) Destroy(_currentAvatar);

            _currentAvatar = avatarGameObject;
            avatarGameObject.transform.SetParent(transform, false);
            avatarGameObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            if (avatarGameObject.TryGetComponent(out Animator animator))
            {
                if (_animatorController != null) animator.runtimeAnimatorController = _animatorController;
                if (_animController != null) _animController.SetAnimator(animator);

                Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
                Transform head = animator.GetBoneTransform(HumanBodyBones.Head);

                if (rightHand == null) rightHand = FindBoneRecursive(avatarGameObject.transform, "RightHand");
                if (rightFoot == null) rightFoot = FindBoneRecursive(avatarGameObject.transform, "RightFoot");
                if (head == null) head = FindBoneRecursive(avatarGameObject.transform, "HeadTop_End");

                if (_playerAttachmentManager != null)
                {
                    if (rightHand != null) _playerAttachmentManager.SetAttachmentTransform(AttachmentLocation.RightHand, rightHand);
                    if (rightFoot != null) _playerAttachmentManager.SetAttachmentTransform(AttachmentLocation.RightFoot, rightFoot);
                    if (head != null) _playerAttachmentManager.SetAttachmentTransform(AttachmentLocation.Hat, head);
                }
            }

            if (avatarGameObject.TryGetComponent(out _currentVrmInstance))
            {
                _blinker = gameObject.AddComponent<VRM10Blinker>();
                _lipSync = gameObject.AddComponent<VRM10AIUEO>();
                _autoExpression = gameObject.AddComponent<VRM10AutoExpression>();
            }
        }

        private Transform FindBoneRecursive(Transform current, string boneName)
        {
            if (current.name == boneName) return current;
            foreach (Transform child in current)
            {
                Transform found = FindBoneRecursive(child, boneName);
                if (found != null) return found;
            }
            return null;
        }

        private void Update()
        {
            if (_currentVrmInstance == null) return;

            if (_blinker != null)
            {
                _currentVrmInstance.Runtime.Expression.SetWeight(ExpressionKey.Blink, _blinker.BlinkValue);
            }

            UpdateLookAtTarget();
            UpdateEmotions();
            UpdateLipSync();
        }

        private void UpdateLookAtTarget()
        {
            if (_lookAtTarget == null || _currentVrmInstance == null) return;

            var (yaw, pitch) = _currentVrmInstance.Runtime.LookAt.CalculateYawPitchFromLookAtPosition(_lookAtTarget.position);
            _currentVrmInstance.Runtime.LookAt.SetYawPitchManually(yaw, pitch);
        }

        private void UpdateEmotions()
        {
            if (_autoExpression == null || _currentVrmInstance == null) return;

            _currentVrmInstance.Runtime.Expression.SetWeight(ExpressionKey.Happy, _autoExpression.Happy);
            _currentVrmInstance.Runtime.Expression.SetWeight(ExpressionKey.Angry, _autoExpression.Angry);
            _currentVrmInstance.Runtime.Expression.SetWeight(ExpressionKey.Sad, _autoExpression.Sad);
            _currentVrmInstance.Runtime.Expression.SetWeight(ExpressionKey.Relaxed, _autoExpression.Relaxed);
            _currentVrmInstance.Runtime.Expression.SetWeight(ExpressionKey.Surprised, _autoExpression.Surprised);
        }

        private void UpdateLipSync()
        {
            if (_lipSync == null || _currentVrmInstance == null) return;

            _currentVrmInstance.Runtime.Expression.SetWeight(ExpressionKey.Aa, _lipSync.Aa);
            _currentVrmInstance.Runtime.Expression.SetWeight(ExpressionKey.Ih, _lipSync.Ih);
            _currentVrmInstance.Runtime.Expression.SetWeight(ExpressionKey.Ou, _lipSync.Ou);
            _currentVrmInstance.Runtime.Expression.SetWeight(ExpressionKey.Ee, _lipSync.Ee);
            _currentVrmInstance.Runtime.Expression.SetWeight(ExpressionKey.Oh, _lipSync.Oh);
        }
    }
}