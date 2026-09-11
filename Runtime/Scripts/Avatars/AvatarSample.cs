using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ViverseSDK.Login;

namespace VastMetaverseTools.Avatars
{
    public class AvatarSample : MonoBehaviour
    {
        [Header("Login Reference")]
        public LoginSample loginSample;

        [Header("Avatar Manager")]
        public AvatarManager avatarManager;

        [Header("UI References")]
        public Button loadAvatarButton;
        public Button clearAvatarButton;
        public TMP_Text statusText;
        public TMP_Text avatarInfoText;

        [Header("Avatar Display")]
        public Transform avatarDisplayParent;

        private bool isProcessing = false; // Track if currently processing an operation
        private string lastStatusMessage = ""; // Track last status message to avoid duplicate logging

        private void Start()
        {
            SetupUI();
            SetupAvatarManager();
            UpdateUI();
        }

        private void SetupUI()
        {
            // Setup button events
            if (loadAvatarButton != null)
            {
                loadAvatarButton.onClick.AddListener(OnLoadAvatarButtonClick);
            }

            if (clearAvatarButton != null)
            {
                clearAvatarButton.onClick.AddListener(OnClearAvatarButtonClick);
            }

            // Ensure AvatarManager exists
            if (avatarManager == null)
            {
                avatarManager = GetComponent<AvatarManager>();
                if (avatarManager == null)
                {
                    avatarManager = gameObject.AddComponent<AvatarManager>();
                }
            }

            // Set Avatar display position
            if (avatarDisplayParent != null && avatarManager.avatarParent == null)
            {
                avatarManager.avatarParent = avatarDisplayParent;
            }
        }

        private void SetupAvatarManager()
        {
            if (avatarManager != null)
            {
                // Subscribe to AvatarManager events
                avatarManager.OnStatusUpdated += OnStatusUpdated; // Use a wrapper method
                avatarManager.OnAvatarLoaded += OnAvatarLoaded;
                avatarManager.OnError += OnAvatarError;
            }
        }

        private void UpdateUI()
        {
            // Update UI based on login status
            bool isLoggedIn = loginSample?.IsLoggedIn() ?? false;

            if (loadAvatarButton != null)
            {
                loadAvatarButton.interactable = isLoggedIn && !isProcessing;
            }

            if (clearAvatarButton != null)
            {
                clearAvatarButton.interactable = avatarManager?.GetCurrentAvatar() != null && !isProcessing;
            }

            // Update status display only if not currently processing an operation
            if (!isLoggedIn && !isProcessing)
            {
                UpdateStatus("Please login first", false); // Don't force log this repeated message
            }
        }

        private void Update()
        {
            UpdateUI();
        }

        public void OnLoadAvatarButtonClick()
        {
            // Set processing flag to prevent status override
            isProcessing = true;

            if (loginSample == null)
            {
                UpdateStatus("LoginSample not configured");
                isProcessing = false;
                return;
            }

            if (!loginSample.IsLoggedIn())
            {
                UpdateStatus("Please login first - Button clicked but not logged in");
                isProcessing = false;
                return;
            }

            string accessToken = loginSample.GetAccessToken();
            if (string.IsNullOrEmpty(accessToken))
            {
                UpdateStatus("Unable to get Access Token");
                isProcessing = false;
                return;
            }

            if (avatarManager == null)
            {
                UpdateStatus("AvatarManager not configured");
                isProcessing = false;
                return;
            }

            UpdateStatus("Starting Avatar loading...");
            avatarManager.LoadDefaultAvatar(accessToken);
            // Note: isProcessing will be reset when avatar loading completes or fails
        }

        public void OnClearAvatarButtonClick()
        {
            if (avatarManager != null)
            {
                isProcessing = true;
                avatarManager.ClearCurrentAvatar();
                UpdateAvatarInfo("");
                isProcessing = false;
            }
        }

        // Wrapper method for AvatarManager OnStatusUpdated event (must match Action<string>)
        private void OnStatusUpdated(string message)
        {
            UpdateStatus(message, true); // Always log status updates from AvatarManager
        }

        private void UpdateStatus(string message, bool forceLog = true)
        {
            // Update UI text
            if (statusText != null)
            {
                statusText.text = message;
            }

            // Only log if the message is different from the last one, or if forced
            if (forceLog || lastStatusMessage != message)
            {
                Debug.Log($"[AvatarSample] Status: {message}");
                lastStatusMessage = message;
            }
        }

        private void OnAvatarLoaded(GameObject avatarGameObject)
        {
            isProcessing = false; // Reset processing flag
            UpdateStatus("Avatar loaded successfully!");

            string avatarInfo = $"Avatar Name: {avatarGameObject.name}\n";
            avatarInfo += $"Position: {avatarGameObject.transform.position}\n";
            avatarInfo += $"Child Count: {avatarGameObject.transform.childCount}";

            UpdateAvatarInfo(avatarInfo);

            Debug.Log($"[AvatarSample] Avatar loaded: {avatarGameObject.name}");
        }

        private void OnAvatarError(string errorMessage)
        {
            isProcessing = false; // Reset processing flag
            UpdateStatus($"Error: {errorMessage}");
            Debug.LogError($"[AvatarSample] Avatar error: {errorMessage}");
        }

        private void UpdateAvatarInfo(string info)
        {
            if (avatarInfoText != null)
            {
                avatarInfoText.text = info;
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (avatarManager != null)
            {
                avatarManager.OnStatusUpdated -= OnStatusUpdated; // Use the wrapper method
                avatarManager.OnAvatarLoaded -= OnAvatarLoaded;
                avatarManager.OnError -= OnAvatarError;
            }
        }

        // Context Menu methods for testing
        [ContextMenu("Test Load Avatar")]
        public void TestLoadAvatar()
        {
            OnLoadAvatarButtonClick();
        }

        [ContextMenu("Test Clear Avatar")]
        public void TestClearAvatar()
        {
            OnClearAvatarButtonClick();
        }

        [ContextMenu("Check Avatar Manager Status")]
        public void CheckAvatarManagerStatus()
        {
            if (avatarManager == null)
            {
                Debug.Log("[AvatarSample] AvatarManager is null");
                return;
            }

            GameObject currentAvatar = avatarManager.GetCurrentAvatar();
            if (currentAvatar != null)
            {
                Debug.Log($"[AvatarSample] Current Avatar: {currentAvatar.name}");
                Debug.Log($"[AvatarSample] Avatar Position: {currentAvatar.transform.position}");
                Debug.Log($"[AvatarSample] Avatar Children: {currentAvatar.transform.childCount}");
            }
            else
            {
                Debug.Log("[AvatarSample] No current avatar loaded");
            }
        }

        [ContextMenu("Reset Processing State")]
        public void ResetProcessingState()
        {
            isProcessing = false;
            lastStatusMessage = ""; // Also reset the last message tracking
            Debug.Log("[AvatarSample] Processing state reset");
        }
    }
}