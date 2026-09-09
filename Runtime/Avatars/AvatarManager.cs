using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using AvatarLibrary;
using System.Linq;

namespace VastMetaverseTools.Runtime.Avatars
{
    // Conditional compilation: Import based on the VRM version detected in the project
#if VRM_1_0 || UNIVRM_1_0
    using UniVRM10;
    using UniGLTF;
#elif VRM_0_X || UNIVRM_0_X
using VRM;
using UniGLTF;
#else
// No VRM plugin detected - compilation will succeed but functionality will be limited
// Please install UniVRM from: https://github.com/vrm-c/UniVRM/releases
#warning "No VRM plugin detected. Please install UniVRM 0.x or VRM 1.0 from https://github.com/vrm-c/UniVRM/releases and add VRM_0_X or VRM_1_0 to Scripting Define Symbols"
#endif

    public class AvatarManager : MonoBehaviour
    {
        [System.Serializable]
        public class AvatarData
        {
            public long id;
            public string ViveportId;
            public string DataType;
            public string Data;
            public string MetaData;
            public string s3Key_bin;
            public string s3Key_snapshot;
            public string s3Key_headicon;
            public string s3Key_vrmBin;
            public string BinaryDataUrl;
            public string VrmBinaryDataUrl;
            public string SnapshotDataUrl;
            public string HeadIconDataUrl;
            public long UpdateTime;
            public long CreateTime;
            public bool IsEncrypted;
            public bool IsDisabled;
            public string Lod;
            public string VRMData;
            public object[] Assets;
            public string HeadId;
            public int ModerationStatus;
            public object MarketAssetPath;
            public bool AllowExport;
        }

        [System.Serializable]
        public class AvatarListData
        {
            public long CurrentAvatarId;
            public AvatarData[] Avatars;
        }

        [System.Serializable]
        public class AvatarApiResponse
        {
            public string version;
            public string error;
            public AvatarListData data;
        }

        [Header("Avatar Settings")]
        public Transform avatarParent;

        [Header("VRM 1.0 Expression Fix")]
        [Tooltip("Disable expressions to avoid morph target binding errors")]
        public bool disableExpressionsOnLoad = true;

        [Tooltip("Enable detailed expression diagnostics")]
        public bool enableExpressionDiagnostics = false;

        [Header("VRM Plugin Status")]
        [SerializeField, Tooltip("Current VRM plugin status")]
        private string vrmPluginStatus = "Checking...";

        private const string AVATAR_API_URL = "https://sdk-api.viverse.com/api/meetingareaselector/";
        private const string GET_AVATAR_LIST_API = "v1/newgenavatar/getavatarlist";
        private const string VRM_DOWNLOAD_URL = "https://github.com/vrm-c/UniVRM/releases";

        private GameObject currentAvatarObject;

        public System.Action<string> OnStatusUpdated;
        public System.Action<GameObject> OnAvatarLoaded;
        public System.Action<string> OnError;

        // VRM version detection
        private enum VRMVersion
        {
            Unknown,
            VRM0,
            VRM10
        }

        private VRMVersion _detectedVersion = VRMVersion.Unknown;

        private void Awake()
        {
            DetectVRMVersion();
            UpdateVRMPluginStatus();
        }

        /// <summary>
        /// Update VRM plugin status for inspector display
        /// </summary>
        private void UpdateVRMPluginStatus()
        {
            switch (_detectedVersion)
            {
                case VRMVersion.VRM10:
                    vrmPluginStatus = "✓ VRM 1.0 Detected";
                    break;
                case VRMVersion.VRM0:
                    vrmPluginStatus = "✓ VRM 0.x Detected";
                    break;
                case VRMVersion.Unknown:
                    vrmPluginStatus = "✗ No VRM Plugin - Please install from:\n" + VRM_DOWNLOAD_URL;
                    Debug.LogError($"[AvatarManager] No VRM plugin detected!\n" +
                                  $"Please install UniVRM from: {VRM_DOWNLOAD_URL}\n" +
                                  $"After installation, add 'VRM_0_X' or 'VRM_1_0' to Scripting Define Symbols in:\n" +
                                  $"Edit > Project Settings > Player > Other Settings > Scripting Define Symbols");
                    break;
            }
        }

        /// <summary>
        /// Automatically detect which VRM version is installed in the project
        /// </summary>
        private void DetectVRMVersion()
        {
#if VRM_1_0 || UNIVRM_1_0
            _detectedVersion = VRMVersion.VRM10;
            Debug.Log("[AvatarManager] VRM version detected: VRM 1.0 (from Scripting Define Symbol)");
#elif VRM_0_X || UNIVRM_0_X
        _detectedVersion = VRMVersion.VRM0;
        Debug.Log("[AvatarManager] VRM version detected: VRM 0.x (from Scripting Define Symbol)");
#else
        // Runtime detection using reflection
        var vrm10Type = System.Type.GetType("UniVRM10.Vrm10, VRM10");
        var vrm0Type = System.Type.GetType("VRM.VRMImporterContext, VRM");

        if (vrm10Type != null)
        {
            _detectedVersion = VRMVersion.VRM10;
            Debug.Log("[AvatarManager] VRM version detected: VRM 1.0 (runtime detection)");
            Debug.LogWarning("[AvatarManager] For better performance, add 'VRM_1_0' to Scripting Define Symbols in Player Settings");
        }
        else if (vrm0Type != null)
        {
            _detectedVersion = VRMVersion.VRM0;
            Debug.Log("[AvatarManager] VRM version detected: VRM 0.x (runtime detection)");
            Debug.LogWarning("[AvatarManager] For better performance, add 'VRM_0_X' to Scripting Define Symbols in Player Settings");
        }
        else
        {
            _detectedVersion = VRMVersion.Unknown;
            Debug.LogError($"[AvatarManager] No VRM plugin detected!\n" +
                          $"Please install UniVRM from: {VRM_DOWNLOAD_URL}\n" +
                          $"Recommended versions:\n" +
                          $"- VRM 1.0 (Latest): For new projects\n" +
                          $"- VRM 0.x: For legacy compatibility");
        }
#endif
        }

        /// <summary>
        /// Gets Avatar list and loads the default VRM model
        /// </summary>
        /// <param name="accessToken">Access Token obtained from LoginManager</param>
        public void LoadDefaultAvatar(string accessToken)
        {
            // Check if VRM plugin is available
            if (_detectedVersion == VRMVersion.Unknown)
            {
                string errorMsg = $"Cannot load VRM: No VRM plugin detected.\n" +
                                $"Please install UniVRM from:\n{VRM_DOWNLOAD_URL}\n\n" +
                                $"Installation steps:\n" +
                                $"1. Download UniVRM package (.unitypackage)\n" +
                                $"2. Import into Unity project\n" +
                                $"3. Add 'VRM_0_X' or 'VRM_1_0' to Scripting Define Symbols";
                OnError?.Invoke(errorMsg);
                Debug.LogError($"[AvatarManager] {errorMsg}");
                return;
            }

            if (string.IsNullOrEmpty(accessToken))
            {
                OnError?.Invoke("Access token is required");
                return;
            }

            OnStatusUpdated?.Invoke("Fetching Avatar list...");
            StartCoroutine(GetAvatarListCoroutine(accessToken));
        }

        /// <summary>
        /// Use UnityWebRequest to get Avatar list directly
        /// </summary>
        private IEnumerator GetAvatarListCoroutine(string accessToken)
        {
            string url = AVATAR_API_URL + GET_AVATAR_LIST_API;

            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                webRequest.SetRequestHeader("accesstoken", accessToken);
                webRequest.SetRequestHeader("Content-Type", "application/json");
                webRequest.timeout = 30;

                Debug.Log($"[AvatarManager] Sending request to: {url}");

                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.ConnectionError ||
                    webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    string error = $"Network request failed: {webRequest.error}";
                    if (!string.IsNullOrEmpty(webRequest.downloadHandler?.text))
                    {
                        error += $"\nResponse: {webRequest.downloadHandler.text}";
                    }
                    OnAvatarListError(error);
                }
                else
                {
                    OnAvatarListReceived(webRequest.downloadHandler.text);
                }
            }
        }

        private void OnAvatarListReceived(string jsonResponse)
        {
            try
            {
                OnStatusUpdated?.Invoke("Parsing Avatar list...");
                Debug.Log($"[AvatarManager] Avatar list response: {jsonResponse}");

                AvatarApiResponse response = JsonUtility.FromJson<AvatarApiResponse>(jsonResponse);

                if (response?.data?.Avatars != null && response.data.Avatars.Length > 0)
                {
                    Debug.Log($"[AvatarManager] Found {response.data.Avatars.Length} avatars. Current Avatar ID: {response.data.CurrentAvatarId}");

                    AvatarData targetAvatar = null;

                    // First try to find the current Avatar using CurrentAvatarId
                    if (response.data.CurrentAvatarId > 0)
                    {
                        foreach (var avatar in response.data.Avatars)
                        {
                            if (avatar.id == response.data.CurrentAvatarId && !string.IsNullOrEmpty(avatar.VrmBinaryDataUrl))
                            {
                                targetAvatar = avatar;
                                Debug.Log($"[AvatarManager] Found current avatar by ID: {avatar.id}");
                                break;
                            }
                        }
                    }

                    // If current Avatar not found, take the first available one with VRM URL
                    if (targetAvatar == null)
                    {
                        foreach (var avatar in response.data.Avatars)
                        {
                            if (!string.IsNullOrEmpty(avatar.VrmBinaryDataUrl) && !avatar.IsDisabled)
                            {
                                targetAvatar = avatar;
                                Debug.Log($"[AvatarManager] Found first available avatar: ID {avatar.id}");
                                break;
                            }
                        }
                    }

                    if (targetAvatar != null)
                    {
                        OnStatusUpdated?.Invoke($"Found Avatar: ID {targetAvatar.id}");
                        Debug.Log($"[AvatarManager] VRM URL: {targetAvatar.VrmBinaryDataUrl}");
                        Debug.Log($"[AvatarManager] Is Encrypted: {targetAvatar.IsEncrypted}");
                        DownloadAndLoadVRM(targetAvatar);
                    }
                    else
                    {
                        OnError?.Invoke("No available VRM model found");
                    }
                }
                else
                {
                    if (response?.data == null)
                    {
                        OnError?.Invoke("Avatar list response data is empty");
                    }
                    else
                    {
                        OnError?.Invoke("Avatar list is empty");
                    }
                }
            }
            catch (System.Exception e)
            {
                OnError?.Invoke($"Failed to parse Avatar list: {e.Message}");
                Debug.LogError($"[AvatarManager] Parse error: {e.Message}");
                Debug.LogError($"[AvatarManager] JSON Response: {jsonResponse}");
            }
        }

        private void OnAvatarListError(string errorMessage)
        {
            OnError?.Invoke($"Failed to get Avatar list: {errorMessage}");
            Debug.LogError($"[AvatarManager] Get avatar list error: {errorMessage}");
        }

        private void DownloadAndLoadVRM(AvatarData avatarData)
        {
            if (string.IsNullOrEmpty(avatarData.VrmBinaryDataUrl))
            {
                OnError?.Invoke("VRM URL is empty");
                return;
            }

            OnStatusUpdated?.Invoke("Downloading VRM model...");
            StartCoroutine(DownloadVRMCoroutine(avatarData));
        }

        private IEnumerator DownloadVRMCoroutine(AvatarData avatarData)
        {
            string encryptionIV = null;

            using (UnityWebRequest webRequest = UnityWebRequest.Get(avatarData.VrmBinaryDataUrl))
            {
                webRequest.timeout = 60;

                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.ConnectionError ||
                    webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    OnError?.Invoke($"Failed to download VRM: {webRequest.error}");
                    yield break;
                }

                // Check if response headers contain encryption IV
                var responseHeaders = webRequest.GetResponseHeaders();
                if (responseHeaders != null && responseHeaders.ContainsKey(SimpleAvatarEncryptUtility.ResponseHeader))
                {
                    encryptionIV = responseHeaders[SimpleAvatarEncryptUtility.ResponseHeader];
                    Debug.Log($"[AvatarManager] Found encryption IV in header: {encryptionIV}");
                }

                byte[] vrmData = webRequest.downloadHandler.data;
                OnStatusUpdated?.Invoke("Processing VRM data...");

                if (avatarData.IsEncrypted || !string.IsNullOrEmpty(encryptionIV))
                {
                    OnStatusUpdated?.Invoke("Decrypting VRM data...");
                    bool decryptionComplete = false;
                    byte[] decryptedData = null;

                    SimpleAvatarEncryptUtility.AsyncDecryptBinaryData(vrmData, encryptionIV, (result) =>
                    {
                        decryptedData = result;
                        decryptionComplete = true;
                    });

                    // Wait for decryption to complete
                    while (!decryptionComplete)
                    {
                        yield return null;
                    }

                    vrmData = decryptedData;
                    OnStatusUpdated?.Invoke("VRM data decrypted successfully");
                }

                yield return StartCoroutine(LoadVRMFromBytesCoroutine(vrmData));
            }
        }

        private IEnumerator LoadVRMFromBytesCoroutine(byte[] vrmData)
        {
            if (currentAvatarObject != null)
            {
                DestroyImmediate(currentAvatarObject);
                currentAvatarObject = null;
            }

            OnStatusUpdated?.Invoke("Loading VRM model...");

            // Check if VRM plugin is detected
            if (_detectedVersion == VRMVersion.Unknown)
            {
                string errorMsg = $"No VRM plugin detected. Please install UniVRM from:\n{VRM_DOWNLOAD_URL}";
                OnError?.Invoke(errorMsg);
                Debug.LogError($"[AvatarManager] {errorMsg}");
                yield break;
            }

#if VRM_1_0 || UNIVRM_1_0
            yield return StartCoroutine(LoadVRM10(vrmData));
#elif VRM_0_X || UNIVRM_0_X
        yield return StartCoroutine(LoadVRM0(vrmData));
#else
        // Use detected version for runtime loading
        if (_detectedVersion == VRMVersion.VRM10)
        {
            yield return StartCoroutine(LoadVRM10Runtime(vrmData));
        }
        else if (_detectedVersion == VRMVersion.VRM0)
        {
            yield return StartCoroutine(LoadVRM0Runtime(vrmData));
        }
#endif
        }

#if VRM_1_0 || UNIVRM_1_0
        /// <summary>
        /// Load VRM model using VRM 1.0 plugin
        /// </summary>
        private IEnumerator LoadVRM10(byte[] vrmData)
        {
            Debug.Log("[AvatarManager] Starting VRM 1.0 load process...");

            var loadTask = Vrm10.LoadBytesAsync(
                vrmData,
                canLoadVrm0X: true, // Allow loading VRM 0.x files and auto-migrate to VRM 1.0
                showMeshes: true,
                awaitCaller: new ImmediateCaller(),
                ct: System.Threading.CancellationToken.None
            );

            while (!loadTask.IsCompleted)
            {
                yield return null;
            }

            Vrm10Instance vrm10Instance = null;
            System.Exception loadException = null;

            try
            {
                if (loadTask.Exception != null)
                {
                    throw loadTask.Exception;
                }

                vrm10Instance = loadTask.Result;
            }
            catch (System.Exception e)
            {
                loadException = e;
                Debug.LogError($"[AvatarManager] Load VRM 1.0 error: {e.Message}");
                Debug.LogError($"[AvatarManager] Stack trace: {e.StackTrace}");

                if (e.InnerException != null)
                {
                    Debug.LogError($"[AvatarManager] Inner exception: {e.InnerException.Message}");
                    Debug.LogError($"[AvatarManager] Inner stack trace: {e.InnerException.StackTrace}");
                }
            }

            if (loadException == null && vrm10Instance != null)
            {
                currentAvatarObject = vrm10Instance.gameObject;

                if (currentAvatarObject != null)
                {
                    if (avatarParent != null)
                    {
                        currentAvatarObject.transform.SetParent(avatarParent);
                    }

                    currentAvatarObject.transform.localPosition = Vector3.zero;
                    currentAvatarObject.transform.localRotation = Quaternion.identity;
                    currentAvatarObject.transform.localScale = Vector3.one;

                    // Apply expression fix if enabled
                    if (disableExpressionsOnLoad)
                    {
                        Debug.Log("[AvatarManager] Applying expression fix to prevent morph target binding errors...");
                        yield return StartCoroutine(FixVRM10Expressions(vrm10Instance));
                    }

                    // Diagnostics if enabled
                    if (enableExpressionDiagnostics)
                    {
                        DiagnoseVRM10Expressions(vrm10Instance);
                    }

                    OnStatusUpdated?.Invoke("VRM model loaded successfully (VRM 1.0)");
                    OnAvatarLoaded?.Invoke(currentAvatarObject);

                    Debug.Log($"[AvatarManager] VRM 1.0 loaded successfully: {currentAvatarObject.name}");
                }
                else
                {
                    OnError?.Invoke("VRM model loading failed - root object is empty");
                }
            }
            else
            {
                string errorMsg = loadException?.Message ?? "Unknown error";
                OnError?.Invoke($"Failed to load VRM: {errorMsg}");
            }
        }

        /// <summary>
        /// Fix VRM 1.0 expression morph target binding errors
        /// </summary>
        private IEnumerator FixVRM10Expressions(Vrm10Instance instance)
        {
            try
            {
                // Set UpdateType to None to prevent expression updates
                instance.UpdateType = Vrm10Instance.UpdateTypes.None;

                Debug.Log("[AvatarManager] Expression fix applied: UpdateType set to None");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[AvatarManager] Failed to apply expression fix: {e.Message}");
            }

            yield return null;
        }

        /// <summary>
        /// Diagnose VRM 1.0 expression configuration
        /// </summary>
        private void DiagnoseVRM10Expressions(Vrm10Instance instance)
        {
            Debug.Log("[AvatarManager] === VRM 1.0 Expression Diagnostics ===");

            try
            {
                if (instance.Vrm?.Expression?.Clips == null)
                {
                    Debug.LogWarning("[AvatarManager] No expression clips found");
                    return;
                }

                // Use LINQ Count() for IEnumerable
                int clipCount = instance.Vrm.Expression.Clips.Count();
                Debug.Log($"[AvatarManager] Found {clipCount} expression clips");

                foreach (var clipTuple in instance.Vrm.Expression.Clips)
                {
                    // Correctly access tuple members
                    if (clipTuple.Clip == null) continue;

                    Debug.Log($"[AvatarManager] Expression: {clipTuple.Clip.name} (Preset: {clipTuple.Preset})");

                    if (clipTuple.Clip.MorphTargetBindings != null)
                    {
                        Debug.Log($"[AvatarManager]   MorphTarget bindings: {clipTuple.Clip.MorphTargetBindings.Length}");

                        for (int i = 0; i < clipTuple.Clip.MorphTargetBindings.Length; i++)
                        {
                            var binding = clipTuple.Clip.MorphTargetBindings[i];
                            Debug.Log($"[AvatarManager]     [{i}] Path: {binding.RelativePath}, Index: {binding.Index}, Weight: {binding.Weight}");

                            if (binding.Index < 0)
                            {
                                Debug.LogError($"[AvatarManager]     ⚠️ Invalid binding index: {binding.Index} at path: {binding.RelativePath}");
                            }
                        }
                    }
                }

                var renderers = instance.GetComponentsInChildren<SkinnedMeshRenderer>();
                Debug.Log($"[AvatarManager] Found {renderers.Length} SkinnedMeshRenderers");

                foreach (var renderer in renderers)
                {
                    var mesh = renderer.sharedMesh;
                    if (mesh != null)
                    {
                        Debug.Log($"[AvatarManager]   Renderer: {renderer.name}, BlendShapes: {mesh.blendShapeCount}");

                        if (mesh.blendShapeCount > 0 && enableExpressionDiagnostics)
                        {
                            for (int i = 0; i < mesh.blendShapeCount; i++)
                            {
                                Debug.Log($"[AvatarManager]     [{i}] {mesh.GetBlendShapeName(i)}");
                            }
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AvatarManager] Expression diagnostics failed: {e.Message}");
            }

            Debug.Log("[AvatarManager] === End of Diagnostics ===");
        }
#endif

#if VRM_0_X || UNIVRM_0_X
    /// <summary>
    /// Load VRM model using VRM 0.x plugin
    /// </summary>
    private IEnumerator LoadVRM0(byte[] vrmData)
    {
        string tempPath = System.IO.Path.Combine(Application.temporaryCachePath, "temp_avatar.vrm");

        try
        {
            System.IO.File.WriteAllBytes(tempPath, vrmData);
        }
        catch (System.Exception e)
        {
            OnError?.Invoke($"Failed to write temporary file: {e.Message}");
            yield break;
        }

        GltfData gltfData = null;
        VRMData vrmDataObj = null;
        VRMImporterContext context = null;

        try
        {
            gltfData = new GlbFileParser(tempPath).Parse();
            vrmDataObj = new VRMData(gltfData);
            context = new VRMImporterContext(vrmDataObj);
        }
        catch (System.Exception e)
        {
            OnError?.Invoke($"Failed to parse VRM data: {e.Message}");
            Debug.LogError($"[AvatarManager] Parse VRM 0.x error: {e.Message}");

            if (System.IO.File.Exists(tempPath))
            {
                System.IO.File.Delete(tempPath);
            }
            yield break;
        }

        var loadTask = context.LoadAsync(new ImmediateCaller());

        while (!loadTask.IsCompleted)
        {
            yield return null;
        }

        RuntimeGltfInstance instance = null;
        System.Exception loadException = null;

        try
        {
            if (loadTask.Exception != null)
            {
                throw loadTask.Exception;
            }

            instance = loadTask.Result;
        }
        catch (System.Exception e)
        {
            loadException = e;
            Debug.LogError($"[AvatarManager] Load VRM 0.x error: {e.Message}");
        }

        if (System.IO.File.Exists(tempPath))
        {
            System.IO.File.Delete(tempPath);
        }

        if (loadException == null && instance != null)
        {
            currentAvatarObject = instance.Root;

            if (currentAvatarObject != null)
            {
                instance.ShowMeshes();

                if (avatarParent != null)
                {
                    currentAvatarObject.transform.SetParent(avatarParent);
                }

                currentAvatarObject.transform.localPosition = Vector3.zero;
                currentAvatarObject.transform.localRotation = Quaternion.identity;
                currentAvatarObject.transform.localScale = Vector3.one;

                OnStatusUpdated?.Invoke("VRM model loaded successfully (VRM 0.x)");
                OnAvatarLoaded?.Invoke(currentAvatarObject);

                Debug.Log($"[AvatarManager] VRM 0.x loaded successfully: {currentAvatarObject.name}");
            }
            else
            {
                OnError?.Invoke("VRM model loading failed - root object is empty");
            }
        }
        else
        {
            string errorMsg = loadException?.Message ?? "Unknown error";
            OnError?.Invoke($"Failed to load VRM: {errorMsg}");
        }

        context?.Dispose();
    }
#endif

#if !VRM_1_0 && !UNIVRM_1_0 && !VRM_0_X && !UNIVRM_0_X
    /// <summary>
    /// Load VRM 1.0 model using runtime detection (reflection-based fallback)
    /// Note: For better performance, add 'VRM_1_0' to Scripting Define Symbols
    /// </summary>
    private IEnumerator LoadVRM10Runtime(byte[] vrmData)
    {
        string errorMsg = $"Runtime VRM 1.0 loading requires 'VRM_1_0' in Scripting Define Symbols.\n" +
                         $"Please add it in Edit > Project Settings > Player > Other Settings > Scripting Define Symbols\n\n" +
                         $"If UniVRM is not installed, download from:\n{VRM_DOWNLOAD_URL}";
        OnError?.Invoke(errorMsg);
        Debug.LogError($"[AvatarManager] {errorMsg}");
        yield break;
    }

    /// <summary>
    /// Load VRM 0.x model using runtime detection (reflection-based fallback)
    /// Note: For better performance, add 'VRM_0_X' to Scripting Define Symbols
    /// </summary>
    private IEnumerator LoadVRM0Runtime(byte[] vrmData)
    {
        string errorMsg = $"Runtime VRM 0.x loading requires 'VRM_0_X' in Scripting Define Symbols.\n" +
                         $"Please add it in Edit > Project Settings > Player > Other Settings > Scripting Define Symbols\n\n" +
                         $"If UniVRM is not installed, download from:\n{VRM_DOWNLOAD_URL}";
        OnError?.Invoke(errorMsg);
        Debug.LogError($"[AvatarManager] {errorMsg}");
        yield break;
    }
#endif

        /// <summary>
        /// Clear the current Avatar
        /// </summary>
        public void ClearCurrentAvatar()
        {
            if (currentAvatarObject != null)
            {
                DestroyImmediate(currentAvatarObject);
                currentAvatarObject = null;
                OnStatusUpdated?.Invoke("Avatar cleared");
            }
        }

        /// <summary>
        /// Get the currently loaded Avatar GameObject
        /// </summary>
        /// <returns>Current Avatar GameObject, returns null if none</returns>
        public GameObject GetCurrentAvatar()
        {
            return currentAvatarObject;
        }

        /// <summary>
        /// Get the detected VRM version
        /// </summary>
        /// <returns>Version string for debugging purposes</returns>
        public string GetDetectedVRMVersion()
        {
            return _detectedVersion.ToString();
        }

        /// <summary>
        /// Check if VRM plugin is properly installed and configured
        /// </summary>
        /// <returns>True if VRM plugin is available</returns>
        public bool IsVRMPluginAvailable()
        {
            return _detectedVersion != VRMVersion.Unknown;
        }

        private void OnDestroy()
        {
            ClearCurrentAvatar();
        }
    }
}