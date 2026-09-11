using UnityEngine;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
 
namespace ViverseSDK.Login
{
    /// <summary>
    /// LoginManager - Core login logic handling authentication for different platforms.
    /// Does not include UI components, focuses solely on login functionality implementation.
    /// </summary>
    public class LoginManager : MonoBehaviour
    {
        // Events for UI feedback
        public event Action<string> OnStatusUpdated;
        public event Action<string> OnTokenUpdated;
        public event Action<string> OnUserInfoUpdated;
        public event Action<bool> OnLoginStateChanged;
 
        // NEW: fires once the VIVERSE SDK has finished loading AND the client
        // has been initialized (i.e. globalThis.viverse + globalThis.viverseClient
        // are ready to use). Other scripts (e.g. MultiplayerManager) that depend
        // on the SDK being present should wait for this instead of guessing timing.
        public event Action OnSDKReady;
 
        private HttpServer httpServer;
        private string appId;
        private const string DOMAIN = "account.htcvive.com";
        private const string COOKIEDOMAIN = "";
 
        // Static queue for scheduling UI actions from other threads
        private static readonly Queue<Action> mainThreadActions = new Queue<Action>();
 
        // Flag to track SDK loading status
        private bool isSDKLoaded = false;
        private bool isSDKLoading = false;
        private bool isCheckingAuth = false;
 
        // Properties for access
        public bool IsLoggedIn => !string.IsNullOrEmpty(PlayerPrefs.GetString("access_token", ""));
        public string AccessToken => PlayerPrefs.GetString("access_token", "");
        public string AccountId => PlayerPrefs.GetString("account_id", "");
 
        // Public so other scripts (e.g. MultiplayerManager) can check before acting
        public bool IsSDKReady => isSDKLoaded;
 
        // ----------------------------------------------------
        // C# declarations for JavaScript functions (JSLib imports)
        // ----------------------------------------------------
        #region JSLib Imports
 
        [DllImport("__Internal")]
        private static extern void VIVERSE_LoadSDK(string gameObjectName);
 
        [DllImport("__Internal")]
        private static extern void VIVERSE_InitializeClient(string clientId, string domain, string cookieDomain, string gameObjectName);
 
        [DllImport("__Internal")]
        private static extern void VIVERSE_LoginWithWorlds(string state);
 
        [DllImport("__Internal")]
        private static extern void VIVERSE_CheckAuth();
 
        #endregion
 
        public static void RunOnMainThread(Action action)
        {
            lock (mainThreadActions)
            {
                mainThreadActions.Enqueue(action);
            }
        }
 
        void Update()
        {
            lock (mainThreadActions)
            {
                while (mainThreadActions.Count > 0)
                {
                    mainThreadActions.Dequeue()?.Invoke();
                }
            }
        }
 
        void OnEnable()
        {
            // Subscribe to the HttpServer's authentication callback event
            HttpServer.OnAuthCallbackReceived += HandleHttpServerCallback;
        }
 
        void OnDisable()
        {
            // Unsubscribe from the event to prevent memory leaks
            HttpServer.OnAuthCallbackReceived -= HandleHttpServerCallback;
        }
 
        /// <summary>
        /// Handles the authentication callback from HttpServer.
        /// </summary>
        /// <param name="message">The callback message.</param>
        private void HandleHttpServerCallback(string message)
        {
            Debug.Log($"[LoginManager] Received HttpServer callback: {message}");
 
            if (message.Contains("Login successful"))
            {
                string savedToken = PlayerPrefs.GetString("access_token", "");
                string savedAccountId = PlayerPrefs.GetString("account_id", "");
 
                if (!string.IsNullOrEmpty(savedToken) && !string.IsNullOrEmpty(savedAccountId))
                {
                    Debug.Log($"[LoginManager] HttpServer login successful! Account ID: {savedAccountId}");
                    Debug.Log($"[LoginManager] Token (first 20 chars): {savedToken.Substring(0, Math.Min(20, savedToken.Length))}...");
 
                    UpdateStatus("Login successful!");
                    OnTokenUpdated?.Invoke(savedToken);
                    OnUserInfoUpdated?.Invoke(savedAccountId);
                    OnLoginStateChanged?.Invoke(true);
                }
                else
                {
                    Debug.LogWarning("[LoginManager] HttpServer callback received but no valid credentials found in PlayerPrefs");
                    UpdateStatus("Login callback received but credentials not found");
                }
            }
            else
            {
                Debug.LogWarning($"[LoginManager] HttpServer callback with error: {message}");
                UpdateStatus($"Login failed: {message}");
                OnLoginStateChanged?.Invoke(false);
            }
        }
 
        /// <summary>
        /// Initializes the LoginManager.
        /// </summary>
        /// <param name="appId">The Application ID (Client ID).</param>
        public void Initialize(string appId)
        {
            if (string.IsNullOrEmpty(appId))
            {
                Debug.LogError("[LoginManager] APP_ID is required for initialization.");
                UpdateStatus("APP_ID is required.");
                return;
            }
 
            this.appId = appId;
            Debug.Log($"[LoginManager] Initializing with APP_ID: {appId}");
            Debug.Log($"[LoginManager] Application.persistentDataPath: {Application.persistentDataPath}");
            Debug.Log($"[LoginManager] GameObject name: {gameObject.name}");
 
            LoadSavedUserInfo();
 
#if !UNITY_EDITOR && UNITY_WEBGL
            LoadViverseSDK();
#else
            InitializeHttpServer();
#endif
        }
 
        private void InitializeHttpServer()
        {
            httpServer = FindObjectOfType<HttpServer>();
            if (httpServer == null)
            {
                Debug.LogError("[LoginManager] Cannot find HttpServer script in the scene.");
                UpdateStatus("HttpServer not found in scene.");
                return;
            }
            Debug.Log("[LoginManager] HttpServer initialized successfully.");
        }
 
        /// <summary>
        /// Starts the login process.
        /// </summary>
        public void StartLogin()
        {
            if (string.IsNullOrEmpty(appId))
            {
                UpdateStatus("APP_ID is required. Please configure your App ID.");
                Debug.LogError("[LoginManager] APP_ID is empty. Cannot proceed with login.");
                return;
            }
 
#if !UNITY_EDITOR && UNITY_WEBGL
            if (!isSDKLoaded)
            {
                UpdateStatus("SDK not ready. Please wait...");
                Debug.LogWarning("[LoginManager] VIVERSE SDK is not loaded yet. Cannot proceed with login.");
                return;
            }
            RequestLogin();
#else
            StartLocalLogin();
#endif
        }
 
        /// <summary>
        /// Clears the login data.
        /// </summary>
        public void ClearLoginData()
        {
            Debug.Log("[LoginManager] Clearing login data...");
 
            PlayerPrefs.DeleteKey("access_token");
            PlayerPrefs.DeleteKey("account_id");
            PlayerPrefs.DeleteKey("expires_in");
            PlayerPrefs.Save();
 
            OnTokenUpdated?.Invoke("");
            OnUserInfoUpdated?.Invoke("");
            OnLoginStateChanged?.Invoke(false);
 
            UpdateStatus("Login data cleared. Please login again.");
            Debug.Log("[LoginManager] Login data cleared successfully!");
        }
 
        private void LoadSavedUserInfo()
        {
            string savedToken = PlayerPrefs.GetString("access_token", "");
            string savedAccountId = PlayerPrefs.GetString("account_id", "");
 
            if (!string.IsNullOrEmpty(savedToken))
            {
                OnTokenUpdated?.Invoke(savedToken);
                OnLoginStateChanged?.Invoke(true);
                UpdateStatus("Already logged in");
                Debug.Log("[LoginManager] Found saved login credentials");
            }
 
            if (!string.IsNullOrEmpty(savedAccountId))
            {
                OnUserInfoUpdated?.Invoke(savedAccountId);
            }
        }
 
        private void UpdateStatus(string message)
        {
            OnStatusUpdated?.Invoke(message);
        }
 
        // ----------------------------------------------------
        // Load VIVERSE SDK dynamically
        // ----------------------------------------------------
        private void LoadViverseSDK()
        {
#if !UNITY_EDITOR && UNITY_WEBGL
            if (isSDKLoading || isSDKLoaded)
            {
                Debug.Log("[LoginManager] VIVERSE SDK is already loading or loaded.");
                return;
            }
 
            isSDKLoading = true;
            UpdateStatus("Loading VIVERSE SDK...");
 
            try
            {
                VIVERSE_LoadSDK(gameObject.name);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LoginManager] Failed to load VIVERSE SDK: {ex.Message}");
                UpdateStatus("Failed to load SDK");
                isSDKLoading = false;
            }
#endif
        }
 
        // ----------------------------------------------------
        // SDK Load Callbacks (called from JavaScript)
        // ----------------------------------------------------
        public void OnSDKLoaded()
        {
#if !UNITY_EDITOR && UNITY_WEBGL
            Debug.Log("[LoginManager] VIVERSE SDK loaded successfully!");
            isSDKLoaded = true;
            isSDKLoading = false;
 
            UpdateStatus("SDK loaded. Initializing...");
 
            try
            {
                VIVERSE_InitializeClient(appId, DOMAIN, COOKIEDOMAIN, gameObject.name);
                UpdateStatus("SDK initialized. Checking login status...");
 
                // NEW: notify anything waiting on the SDK (e.g. MultiplayerManager)
                // that globalThis.viverse / globalThis.viverseClient are ready.
                OnSDKReady?.Invoke();
 
                CheckAuthenticationStatus();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LoginManager] Failed to initialize VIVERSE client: {ex.Message}");
                UpdateStatus("Failed to initialize SDK");
            }
#endif
        }
 
        public void OnSDKLoadFailed()
        {
#if !UNITY_EDITOR && UNITY_WEBGL
            Debug.LogError("[LoginManager] Failed to load VIVERSE SDK from JavaScript!");
            isSDKLoaded = false;
            isSDKLoading = false;
            UpdateStatus("Failed to load SDK");
#endif
        }
 
        private void CheckAuthenticationStatus()
        {
#if !UNITY_EDITOR && UNITY_WEBGL
            if (!isSDKLoaded)
            {
                Debug.LogWarning("[LoginManager] SDK not loaded yet, cannot check auth status.");
                return;
            }
 
            if (isCheckingAuth)
            {
                Debug.Log("[LoginManager] Already checking auth status...");
                return;
            }
 
            isCheckingAuth = true;
 
            try
            {
                VIVERSE_CheckAuth();
                Debug.Log("[LoginManager] Checking authentication status...");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LoginManager] Failed to check authentication: {ex.Message}");
                isCheckingAuth = false;
                UpdateStatus("Auth check failed. Click to login.");
            }
#endif
        }
 
        // ----------------------------------------------------
        // WebGL Login using VIVERSE SDK
        // ----------------------------------------------------
        public void RequestLogin()
        {
#if !UNITY_EDITOR && UNITY_WEBGL
            if (!isSDKLoaded)
            {
                UpdateStatus("SDK not ready. Loading...");
                LoadViverseSDK();
                return;
            }
 
            UpdateStatus("Opening login window...");
            Debug.Log("=== Starting VIVERSE Login Process ===");
            Debug.Log($"APP_ID: {appId}");
            Debug.Log($"DOMAIN: {DOMAIN}");
            Debug.Log($"COOKIEDOMAIN: '{COOKIEDOMAIN}'");
 
            try
            {
                VIVERSE_LoginWithWorlds("");
                StartCoroutine(CheckLoginTimeout());
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LoginManager] Login request failed: {ex.Message}");
                UpdateStatus($"Login failed: {ex.Message}");
                OnLoginStateChanged?.Invoke(false);
            }
#else
            Debug.Log("[LoginManager] VIVERSE login functionality only works in WebGL builds.");
#endif
        }
 
        private System.Collections.IEnumerator CheckLoginTimeout()
        {
            yield return new WaitForSeconds(10f);
 
            UpdateStatus("Login window may be blocked. Please check popup blockers and try again.");
            Debug.LogWarning("[LoginManager] Login window may have been blocked by browser");
            OnLoginStateChanged?.Invoke(false);
        }
 
        // ----------------------------------------------------
        // Editor Mode Login using localhost
        // ----------------------------------------------------
        void StartLocalLogin()
        {
            if (string.IsNullOrEmpty(appId))
            {
                UpdateStatus("APP_ID is required for localhost login.");
                Debug.LogError("[LoginManager] Cannot start localhost login: APP_ID is empty.");
                return;
            }
 
            if (httpServer == null)
            {
                UpdateStatus("HttpServer not available. Please check scene setup.");
                Debug.LogError("[LoginManager] HttpServer is not initialized.");
                return;
            }
 
            Debug.Log("[LoginManager] Starting localhost login process...");
            httpServer.StartServer();
            string url = $"http://localhost:{HttpServer.HTTP_PORT}/";
 
            try
            {
                Application.OpenURL(url);
                UpdateStatus("Opening login page...");
                OnLoginStateChanged?.Invoke(false);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Failed to open login page: {ex.Message}");
                Debug.LogError($"[LoginManager] Failed to open login page: {ex.Message}");
            }
        }
 
        // ----------------------------------------------------
        // Handle results returned from JavaScript
        // ----------------------------------------------------
        public void HandleLoginSuccess(string resultJson)
        {
            isCheckingAuth = false;
 
            Debug.Log("[LoginManager] VIVERSE login successful! Raw result: " + resultJson);
 
            try
            {
                Result authResult = JsonUtility.FromJson<Result>(resultJson);
                Debug.Log($"[LoginManager] Login Account ID: {authResult.account_id}, Token (first 10 chars): {authResult.access_token.Substring(0, Mathf.Min(10, authResult.access_token.Length))}...");
 
                PlayerPrefs.SetString("access_token", authResult.access_token);
                PlayerPrefs.SetString("account_id", authResult.account_id);
                PlayerPrefs.SetInt("expires_in", authResult.expires_in);
                PlayerPrefs.Save();
 
                UpdateStatus("Login success");
                OnTokenUpdated?.Invoke(authResult.access_token);
                OnUserInfoUpdated?.Invoke(authResult.account_id);
                OnLoginStateChanged?.Invoke(true);
 
                Debug.Log("[LoginManager] Access token and account ID saved successfully!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LoginManager] Failed to parse login result: {ex.Message}");
                UpdateStatus("Login success (parse error)");
            }
        }
 
        public void HandleLoginFailure(string errorMsg)
        {
            isCheckingAuth = false;
            Debug.Log($"[LoginManager] VIVERSE login check result: {errorMsg}");
 
            if (errorMsg.Contains("Guest mode"))
            {
                Debug.Log("[LoginManager] User is in guest mode - no existing authentication found");
                UpdateStatus("Guest mode detected. Click to login.");
            }
            else if (errorMsg.Contains("No existing token found") || errorMsg.Contains("not logged in"))
            {
                Debug.Log("[LoginManager] No existing login found, user needs to authenticate");
                UpdateStatus("Please click login to authenticate");
            }
            else
            {
                Debug.LogError("[LoginManager] VIVERSE login failed: " + errorMsg);
                UpdateStatus($"Login failed: {errorMsg}");
            }
 
            OnLoginStateChanged?.Invoke(false);
        }
    }
 
    // Data structure for JSON deserialization
    [System.Serializable]
    public class Result
    {
        public string access_token;
        public string account_id;
        public int expires_in;
        public string state;
    }
}