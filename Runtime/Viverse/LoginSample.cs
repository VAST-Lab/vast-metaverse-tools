using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ViverseSDK.Login
{
    public class LoginSample : MonoBehaviour
    {
        [Header("Login Settings")]
        public string appId = "";
        
        [Header("Auto Login")]
        [Tooltip("When checked, automatically clear login data and start login process on Start()")]
        public bool autoLogin = false;
        
        [Header("Auto Login Timing")]
        [Tooltip("Delay in seconds before starting auto login")]
        [Range(0f, 5f)]
        public float loginDelay = 0.5f;

        [Header("UI References")]
        public Button loginButton;
        public Button clearDataButton;
        public TMP_Text statusText;
        public TMP_Text token;
        public TMP_Text userInfo;

        private LoginManager _service;
        
        // Auto login state tracking
        private bool _isInitialized = false;
        private bool _loginRequested = false;
        private bool _hasCheckedAuth = false; // Track if authentication check is completed
        private bool _autoLoginCompleted = false;

        void Start()
        {
            Debug.Log($"[LoginSample] Platform: {Application.platform}, WebGL: {Application.platform == RuntimePlatform.WebGLPlayer}");
            Debug.Log($"[LoginSample] Auto login settings => enabled={autoLogin}, delay={loginDelay}s");
            
            // Check existing token in PlayerPrefs before clearing
            var savedToken = PlayerPrefs.GetString("access_token", "");
            Debug.Log($"[LoginSample] Existing token in PlayerPrefs: {(!string.IsNullOrEmpty(savedToken) ? "Found" : "None")} (length={savedToken?.Length ?? 0})");

            // Ensure LoginManager is attached to the GameObject
            _service = GetComponent<LoginManager>();
            if (_service == null)
            {
                _service = gameObject.AddComponent<LoginManager>();
            }

            // Check if HttpServer exists in the scene (for localhost login)
            CheckHttpServerAvailability();

            // Setup UI event listeners
            SetupUIListeners();

            // Subscribe to LoginManager events
            SubscribeToLoginManagerEvents();

            // Initialize LoginManager with APP_ID
            if (!string.IsNullOrEmpty(appId))
            {
                _service.Initialize(appId);
                _isInitialized = true;
                Debug.Log($"[LoginSample] LoginManager initialized successfully with App ID: {appId}");
                
#if UNITY_WEBGL && !UNITY_EDITOR
                Debug.Log("[LoginSample] WebGL build: Check browser console for additional logs");
#endif
                
                // Start auto login if enabled
                if (autoLogin)
                {
                    StartAutoLogin();
                }
            }
            else
            {
                ShowStatus("APP_ID is required. Please configure your App ID.");
                Debug.LogError("[LoginSample] APP_ID is empty. https://studio.viverse.com/upload Click 'Create New World' to generate a new App ID. To check your App ID, go to the 'Manage Content' page and view it under the Overview tab.");
            }
        }

        /// <summary>
        /// Starts the auto login process - always clears data first for a fresh login
        /// </summary>
        private void StartAutoLogin()
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("[LoginSample] LoginManager not initialized, cannot start auto login");
                return;
            }

            Debug.Log("[LoginSample] Auto login enabled: clearing login data and starting fresh login process...");

            // Always clear login data for auto login to ensure fresh authentication
            Debug.Log("[LoginSample] Auto login: Clearing existing login data");
            PlayerPrefs.DeleteKey("access_token");
            PlayerPrefs.DeleteKey("account_id");
            PlayerPrefs.Save();

            if (_service != null)
            {
                _service.ClearLoginData();
            }

            _loginRequested = true;

#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL: Wait for SDK initialization before triggering login
            Debug.Log("[LoginSample] WebGL: Auto login requested, will trigger automatically after SDK initialization");
#else
            // Non-WebGL platforms: Use delayed execution
            Debug.Log("[LoginSample] Starting auto login with delay...");
            Invoke(nameof(ExecuteAutoLogin), loginDelay);
#endif
        }

        /// <summary>
        /// Executes the actual auto login
        /// </summary>
        private void ExecuteAutoLogin()
        {
            if (_autoLoginCompleted)
            {
                Debug.Log("[LoginSample] Auto login already completed, skipping execution");
                return;
            }

            if (!autoLogin || !_loginRequested)
            {
                Debug.Log("[LoginSample] Auto login cancelled or not requested");
                return;
            }

            Debug.Log("[LoginSample] Executing auto login... (calling LoginManager.StartLogin)");
            OnStartButtonClick();
        }

        /// <summary>
        /// Legacy auto login method - kept for backward compatibility
        /// </summary>
        private void AutoLoginDelayed()
        {
            ExecuteAutoLogin();
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        /// <summary>
        /// WebGL-specific status monitoring for auto login
        /// </summary>
        private void OnWebGLStatusUpdate(string status)
        {
            if (string.IsNullOrEmpty(status) || !_loginRequested) return;

            Debug.Log($"[LoginSample] WebGL: Status update received: {status}");

            // Priority 1: Detect login success states - cancel auto login immediately
            if (status.Contains("Login successful") || status.Contains("Already logged in") || 
                status.Contains("Login success") || status.Contains("found saved login"))
            {
                if (_loginRequested)
                {
                    Debug.Log($"[LoginSample] WebGL: ✅ Login success detected: {status}, cancelling auto login request");
                    _loginRequested = false;
                    _hasCheckedAuth = true;
                    _autoLoginCompleted = true;
                }
                return;
            }

            // Priority 2: SDK initialization complete - mark auth check started
            if (status.Contains("SDK initialized") || status.Contains("Checking login status"))
            {
                _hasCheckedAuth = true;
                Debug.Log("[LoginSample] WebGL: SDK initialized, waiting for auth check result (CheckAuth)...");
                return;
            }

            // Priority 3: Not logged in confirmation - trigger auto login
            if ((status.Contains("Guest mode") || status.Contains("Please click login") || 
                 status.Contains("not logged in")) && _hasCheckedAuth && _loginRequested)
            {
                Debug.Log($"[LoginSample] WebGL: ⚠️ Not logged in confirmed: {status}, starting auto login");
                _loginRequested = false;
                _service.StartLogin();
                return;
            }
        }
#endif

        private void CheckHttpServerAvailability()
        {
            HttpServer httpServer = FindObjectOfType<HttpServer>();
            if (httpServer == null)
            {
                Debug.LogWarning("[LoginSample] HttpServer not found in scene. Creating HttpServer automatically...");
                
                // Create a new GameObject for HttpServer
                GameObject httpServerObject = new GameObject("HttpServer");
                
                // Add HttpServer component
                httpServer = httpServerObject.AddComponent<HttpServer>();
                
                Debug.Log("[LoginSample] HttpServer created successfully. Localhost login is now available.");
            }
            else
            {
                Debug.Log("[LoginSample] HttpServer found in scene. Localhost login is available.");
            }
        }

        private void SetupUIListeners()
        {
            if (loginButton != null)
            {
                loginButton.onClick.AddListener(OnStartButtonClick);
            }
            else
            {
                Debug.LogWarning("[LoginSample] Login button is not assigned in the inspector");
            }

            if (clearDataButton != null)
            {
                clearDataButton.onClick.AddListener(OnClearDataButtonClick);
            }
            else
            {
                Debug.LogWarning("[LoginSample] Clear data button is not assigned in the inspector");
            }
        }

        private void SubscribeToLoginManagerEvents()
        {
            if (_service != null)
            {
                _service.OnStatusUpdated += ShowStatus;
                _service.OnTokenUpdated += UpdateTokenDisplay;
                _service.OnUserInfoUpdated += UpdateUserInfo;
                _service.OnLoginStateChanged += OnLoginStateChanged;

#if UNITY_WEBGL && !UNITY_EDITOR
                // Additional WebGL-specific status monitoring for auto login
                _service.OnStatusUpdated += OnWebGLStatusUpdate;
#endif
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from events to prevent memory leaks
            if (_service != null)
            {
                _service.OnStatusUpdated -= ShowStatus;
                _service.OnTokenUpdated -= UpdateTokenDisplay;
                _service.OnUserInfoUpdated -= UpdateUserInfo;
                _service.OnLoginStateChanged -= OnLoginStateChanged;

#if UNITY_WEBGL && !UNITY_EDITOR
                _service.OnStatusUpdated -= OnWebGLStatusUpdate;
#endif
            }
        }

        public void OnStartButtonClick()
        {
            if (_service == null)
            {
                ShowStatus("LoginManager not initialized");
                Debug.LogError("[LoginSample] LoginManager is not initialized");
                return;
            }

            if (string.IsNullOrEmpty(appId))
            {
                ShowStatus("APP_ID is required. Please configure your App ID.");
                Debug.LogError("[LoginSample] APP_ID is empty. Cannot proceed with login.");
                return;
            }

            _service.StartLogin();
        }

        public void OnClearDataButtonClick()
        {
            if (_service == null)
            {
                ShowStatus("LoginManager not initialized");
                return;
            }

            _service.ClearLoginData();
            
            // Reset auto login state when data is cleared
            _autoLoginCompleted = false;
            _loginRequested = false;
            _hasCheckedAuth = false;
        }

        public void ShowStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
            Debug.Log($"[LoginSample] Status: {message}");
        }

        public void UpdateUserInfo(string accountId)
        {
            if (userInfo != null)
            {
                if (!string.IsNullOrEmpty(accountId))
                {
                    string displayAccountId = accountId.Length > 5
                        ? accountId.Substring(0, 5) + "..."
                        : accountId;
                    userInfo.text = $"Account ID: {displayAccountId}";
                    Debug.Log($"[LoginSample] User info updated with Account ID: {accountId}");
                }
                else
                {
                    userInfo.text = "Account ID: Not logged in";
                    Debug.Log("[LoginSample] User info display cleared");
                }
            }
            else
            {
                Debug.LogWarning("[LoginSample] UserInfo TMP_Text component is null");
            }
        }

        public void UpdateTokenDisplay(string accessToken)
        {
            if (token != null)
            {
                if (!string.IsNullOrEmpty(accessToken))
                {
                    string displayToken = accessToken.Length > 20
                        ? accessToken.Substring(0, 20) + "..."
                        : accessToken;
                    token.text = $"Token: {displayToken}";
                    Debug.Log("[LoginSample] Token display updated");
                    Debug.Log($"[LoginSample] Full Access Token: {accessToken}");
                }
                else
                {
                    token.text = "Token: Not logged in";
                    Debug.Log("[LoginSample] Token display cleared");
                }
            }
            else
            {
                Debug.LogWarning("[LoginSample] Token TMP_Text component is null");
            }
        }

        private void OnLoginStateChanged(bool isLoggedIn)
        {
            if (loginButton != null)
            {
                loginButton.interactable = !isLoggedIn;
            }

            if (isLoggedIn)
            {
                ShowStatus("Login success");
                
                // Auto login success handling
                if (_loginRequested)
                {
                    Debug.Log("[LoginSample] ✅ Auto login successful!");
                    _loginRequested = false;
                    _autoLoginCompleted = true;
                    
                    var accessToken = _service.AccessToken;
                    Debug.Log($"[LoginSample] Access Token preview: {(string.IsNullOrEmpty(accessToken) ? "N/A" : accessToken.Substring(0, Mathf.Min(20, accessToken.Length)) + "... (length=" + accessToken.Length + ")")}");
                    Debug.Log($"[LoginSample] Account ID: {_service.AccountId ?? "N/A"}");
                    
                    var savedToken = PlayerPrefs.GetString("access_token", "");
                    Debug.Log($"[LoginSample] PlayerPrefs token saved: {(!string.IsNullOrEmpty(savedToken) ? "Yes" : "No")} (length={savedToken?.Length ?? 0})");
                }
            }
            else
            {
                Debug.LogWarning("[LoginSample] ❌ Login failed or logged out");
            }
        }

        // Helper methods for external access
        public string GetAccessToken()
        {
            return _service?.AccessToken ?? "";
        }

        public string GetAccountId()
        {
            return _service?.AccountId ?? "";
        }

        public bool IsLoggedIn()
        {
            return _service?.IsLoggedIn ?? false;
        }

        // Context menu methods for testing
        [ContextMenu("Check Login Status")]
        public void CheckLoginStatus()
        {
            if (_service != null)
            {
                bool isLoggedIn = _service.IsLoggedIn;
                string token = _service.AccessToken;
                string accountId = _service.AccountId;

                Debug.Log($"[LoginSample] Login Status - IsLoggedIn: {isLoggedIn}");
                if (isLoggedIn)
                {
                    Debug.Log($"[LoginSample] Token: {token.Substring(0, System.Math.Min(10, token.Length))}...");
                    Debug.Log($"[LoginSample] Account ID: {accountId}");
                    ShowStatus("✅ Currently logged in");
                }
                else
                {
                    Debug.Log("[LoginSample] Not logged in");
                    ShowStatus("❌ Not logged in");
                }
            }
        }

        [ContextMenu("Force Clear Login Data")]
        public void ForceClearLoginData()
        {
            OnClearDataButtonClick();
        }

        [ContextMenu("Dump Auto Login State")]
        public void DumpAutoLoginState()
        {
            string token = _service?.AccessToken ?? "";
            string accountId = _service?.AccountId ?? "";
            string savedToken = PlayerPrefs.GetString("access_token", "");
            
            Debug.Log($"[LoginSample] === Auto Login State Dump ===\n" +
                      $"Platform: {Application.platform}\n" +
                      $"Initialized: {_isInitialized}\n" +
                      $"Auto Login Enabled: {autoLogin}\n" +
                      $"Login Delay: {loginDelay}s\n" +
                      $"Login Requested: {_loginRequested}\n" +
                      $"Has Checked Auth: {_hasCheckedAuth}\n" +
                      $"Auto Login Completed: {_autoLoginCompleted}\n" +
                      $"LoginManager.IsLoggedIn: {_service?.IsLoggedIn}\n" +
                      $"Account ID: {(string.IsNullOrEmpty(accountId) ? "<empty>" : accountId)}\n" +
                      $"Token Length: {token?.Length ?? 0}, Preview: {(string.IsNullOrEmpty(token) ? "<empty>" : token.Substring(0, Mathf.Min(20, token.Length)))}\n" +
                      $"PlayerPrefs Token Length: {savedToken?.Length ?? 0}");
        }

        // Context menu method for testing auto login
        [ContextMenu("Test Auto Login")]
        public void TestAutoLogin()
        {
            autoLogin = true;
            _autoLoginCompleted = false;
            _loginRequested = false;
            _hasCheckedAuth = false;
            StartAutoLogin();
        }

        // Context menu method to manually check HttpServer availability
        [ContextMenu("Check HttpServer Availability")]
        public void ManualCheckHttpServerAvailability()
        {
            CheckHttpServerAvailability();
        }

        // Context menu method to force create HttpServer
        [ContextMenu("Force Create HttpServer")]
        public void ForceCreateHttpServer()
        {
            // Check if HttpServer already exists
            HttpServer existingServer = FindObjectOfType<HttpServer>();
            if (existingServer != null)
            {
                Debug.Log("[LoginSample] HttpServer already exists in scene.");
                ShowStatus("HttpServer already exists");
                return;
            }

            // Create a new GameObject for HttpServer
            GameObject httpServerObject = new GameObject("HttpServer");
            
            // Add HttpServer component
            HttpServer httpServer = httpServerObject.AddComponent<HttpServer>();
            
            Debug.Log("[LoginSample] HttpServer created manually. Localhost login is now available.");
            ShowStatus("HttpServer created successfully");
        }
    }
}