using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using ViverseSDK.Login;

namespace ViverseSDK.CloudSave
{
    /// <summary>
    /// CloudSaveService - Handles cloud save operations using VIVERSE APIs
    /// Requires LoginManager to be initialized and user to be logged in
    /// </summary>
    public class CloudSaveService : MonoBehaviour
    {
        [Header("API Configuration")]
        private const string BASE_URL = "https://broadcasting-gateway-gaming.vrprod.viveport.com/api/webrtcbot-service/v1/cloudsave";
        private const string USER_APP_BASE_URL = "https://broadcasting-gateway-gaming.vrprod.viveport.com/api/webrtcbot-service/v1/userapp";

        [Header("Dependencies")]
        public LoginManager loginManager;

        public event Action<string> OnStatusUpdated;
        public event Action<CloudSaveResponseWrapper> OnFullDataLoaded;
        public event Action<bool> OnSaveCompleted;

        // UserApp API Events
        public event Action<UserAppLatestResponse> OnUserAppLatestLoaded;
        public event Action<List<UserAppData>> OnUserAppAllLoaded;
        public event Action<bool> OnUserAppSaveCompleted;
        public event Action<bool> OnUserAppDeleteCompleted;

        [System.Serializable]
        public class CloudSaveData
        {
            public int level;
            public string @class;
            public List<string> inventory = new List<string>();
            public string last_updated;

            public CloudSaveData()
            {
                inventory = new List<string>();
            }
        }

        [System.Serializable]
        public class CloudSaveResponseWrapper
        {
            public string id;
            public string user_id;
            public string app_id;
            public CloudSaveResponseData data;
            public string created_at;
            public string updated_at;
        }

        [System.Serializable]
        public class CloudSaveResponseData
        {
            public CloudSaveData userdata;
        }

        // UserApp API Data Structures
        [System.Serializable]
        public class UserAppData
        {
            public string user_id;
            public string app_id;
            public UserAppGameData data;
            public long version;
            public string created_at;
        }

        [System.Serializable]
        public class UserAppGameData
        {
            public int level;
            public int score;
        }

        [System.Serializable]
        public class UserAppLatestResponse
        {
            public string user_id;
            public string app_id;
            public UserAppGameData data;
            public long version;
            public string created_at;
        }

        [System.Serializable]
        public class UserAppSaveRequest
        {
            public string app_id;
            public UserAppGameData data;
        }

     
        [System.Serializable]
        public class UserAppDeleteResponse
        {
            public string message;
        }


        void Start()
        {
            if (loginManager == null)
            {
                loginManager = FindObjectOfType<LoginManager>();
                if (loginManager == null)
                {
                    Debug.LogWarning("[CloudSaveService] LoginManager not found in scene. Creating one automatically...");

                    GameObject loginManagerObject = new GameObject("LoginManager");
                    loginManager = loginManagerObject.AddComponent<LoginManager>();

                    Debug.Log("[CloudSaveService] LoginManager created successfully.");
                }
                else
                {
                    Debug.Log("[CloudSaveService] LoginManager found in scene.");
                }
            }
        }

        /// <summary>
        /// Loads cloud save data for the current user
        /// </summary>
        public void LoadCloudSave()
        {
            if (!ValidateLogin())
                return;

            StartCoroutine(LoadCloudSaveCoroutine());
        }

        /// <summary>
        /// Saves data to cloud (overwrites existing data)
        /// </summary>
        /// <param name="saveData">Data to save</param>
        public void SaveToCloud(CloudSaveData saveData)
        {
            if (!ValidateLogin())
                return;

            saveData.last_updated = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ");
            StartCoroutine(SaveToCloudCoroutine(saveData));
        }

        /// <summary>
        /// Convenience method to save with basic parameters
        /// </summary>
        /// <param name="level">Player level</param>
        /// <param name="playerClass">Player class name</param>
        /// <param name="inventory">List of inventory items</param>
        public void SaveToCloud(int level, string playerClass, List<string> inventory)
        {
            CloudSaveData saveData = new CloudSaveData
            {
                level = level,
                @class = playerClass,
                inventory = inventory ?? new List<string>(),
                last_updated = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            SaveToCloud(saveData);
        }

        /// <summary>
        /// Gets the latest user app data
        /// </summary>
        public void GetUserAppLatest()
        {
            if (!ValidateLogin())
                return;

            StartCoroutine(GetUserAppLatestCoroutine());
        }

        /// <summary>
        /// Saves user app data
        /// </summary>
        /// <param name="level">Player level</param>
        /// <param name="score">Player score</param>
        public void SaveUserAppData(int level, int score)
        {
            if (!ValidateLogin())
                return;

            StartCoroutine(SaveUserAppDataCoroutine(level, score));
        }

        /// <summary>
        /// Gets all user app data
        /// </summary>
        public void GetUserAppAll()
        {
            if (!ValidateLogin())
                return;

            StartCoroutine(GetUserAppAllCoroutine());
        }

        /// <summary>
        /// Deletes user app data by version
        /// </summary>
        /// <param name="version">Version to delete</param>
        public void DeleteUserAppData(long version)
        {
            if (!ValidateLogin())
                return;

            StartCoroutine(DeleteUserAppDataCoroutine(version));
        }

        private bool ValidateLogin()
        {
            if (loginManager == null)
            {
                UpdateStatus("LoginManager not available");
                Debug.LogError("[CloudSaveService] LoginManager is null");
                return false;
            }

            if (string.IsNullOrEmpty(loginManager.AccessToken))
            {
                UpdateStatus("Please login first");
                Debug.LogWarning("[CloudSaveService] Access token not available");
                return false;
            }

            return true;
        }

        private IEnumerator LoadCloudSaveCoroutine()
        {
            string appId = GetAppId();
            if (string.IsNullOrEmpty(appId))
            {
                UpdateStatus("App ID not available");
                yield break;
            }

            string url = $"{BASE_URL}/{appId}";

            UpdateStatus("Loading cloud save...");
            Debug.Log($"[CloudSaveService] Loading from: {url}");

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("accesstoken", loginManager.AccessToken);
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[CloudSaveService] Load successful. Response: {request.downloadHandler.text}");

                    try
                    {
                        CloudSaveResponseWrapper fullResponse = JsonUtility.FromJson<CloudSaveResponseWrapper>(request.downloadHandler.text);

                        if (fullResponse != null && fullResponse.data?.userdata != null)
                        {
                            OnFullDataLoaded?.Invoke(fullResponse);
                            UpdateStatus("Cloud save loaded successfully");
                        }
                        else
                        {
                            Debug.LogError("[CloudSaveService] Invalid response structure");
                            UpdateStatus("Invalid response format");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[CloudSaveService] Failed to parse response: {ex.Message}");
                        Debug.LogError($"[CloudSaveService] Response was: {request.downloadHandler.text}");
                        UpdateStatus("Failed to parse cloud save data");
                    }
                }
                else
                {
                    Debug.LogError($"[CloudSaveService] Load failed: {request.error}");
                    Debug.LogError($"[CloudSaveService] Response code: {request.responseCode}");
                    Debug.LogError($"[CloudSaveService] Response: {request.downloadHandler.text}");

                    if (request.responseCode == 404)
                    {
                        UpdateStatus("No cloud save found");
                        OnFullDataLoaded?.Invoke(null);
                    }
                    else
                    {
                        UpdateStatus($"Load failed: {request.error}");
                    }
                }
            }
        }

        private IEnumerator SaveToCloudCoroutine(CloudSaveData saveData)
        {
            string appId = GetAppId();
            if (string.IsNullOrEmpty(appId))
            {
                UpdateStatus("App ID not available");
                OnSaveCompleted?.Invoke(false);
                yield break;
            }

            string url = $"{BASE_URL}/{appId}/upsert/userdata";
            string jsonData = JsonUtility.ToJson(saveData, true);

            UpdateStatus("Saving to cloud...");
            Debug.Log($"[CloudSaveService] Saving to: {url}");
            Debug.Log($"[CloudSaveService] Data: {jsonData}");

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();

                request.SetRequestHeader("accesstoken", loginManager.AccessToken);
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[CloudSaveService] Save successful. Response: {request.downloadHandler.text}");
                    UpdateStatus("Cloud save completed successfully");
                    OnSaveCompleted?.Invoke(true);
                }
                else
                {
                    Debug.LogError($"[CloudSaveService] Save failed: {request.error}");
                    Debug.LogError($"[CloudSaveService] Response code: {request.responseCode}");
                    Debug.LogError($"[CloudSaveService] Response: {request.downloadHandler.text}");
                    UpdateStatus($"Save failed: {request.error}");
                    OnSaveCompleted?.Invoke(false);
                }
            }
        }

        private IEnumerator GetUserAppLatestCoroutine()
        {
            string appId = GetAppId();
            if (string.IsNullOrEmpty(appId))
            {
                UpdateStatus("App ID not available");
                yield break;
            }

            string url = $"{USER_APP_BASE_URL}/{appId}/latest";

            UpdateStatus("Loading latest user data...");
            Debug.Log($"[CloudSaveService] Loading latest user data from: {url}");

            // Use GET method, same as LoadCloudSave implementation
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                // Set accesstoken header
                request.SetRequestHeader("accesstoken", loginManager.AccessToken);

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[CloudSaveService] User app latest load successful. Response: {request.downloadHandler.text}");

                    try
                    {
                        UserAppLatestResponse response = JsonUtility.FromJson<UserAppLatestResponse>(request.downloadHandler.text);
                        OnUserAppLatestLoaded?.Invoke(response);
                        UpdateStatus("Latest user data loaded successfully");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[CloudSaveService] Failed to parse user app latest response: {ex.Message}");
                        Debug.LogError($"[CloudSaveService] Response was: {request.downloadHandler.text}");
                        UpdateStatus("Failed to parse latest user data");
                    }
                }
                else
                {
                    Debug.LogError($"[CloudSaveService] User app latest load failed: {request.error}");
                    Debug.LogError($"[CloudSaveService] Response code: {request.responseCode}");
                    Debug.LogError($"[CloudSaveService] Response body: {request.downloadHandler.text}");
                    UpdateStatus($"Load latest user data failed: {request.error}");
                }
            }
        }

        private IEnumerator SaveUserAppDataCoroutine(int level, int score)
        {
            string appId = GetAppId();
            if (string.IsNullOrEmpty(appId))
            {
                UpdateStatus("App ID not available");
                OnUserAppSaveCompleted?.Invoke(false);
                yield break;
            }

            string url = $"{USER_APP_BASE_URL}/save";

            UserAppSaveRequest saveRequest = new UserAppSaveRequest
            {
                app_id = appId,
                data = new UserAppGameData
                {
                    level = level,
                    score = score
                }
            };

            string jsonData = JsonUtility.ToJson(saveRequest, true);

            UpdateStatus("Saving user data...");
            Debug.Log($"[CloudSaveService] Saving user data to: {url}");
            Debug.Log($"[CloudSaveService] Data: {jsonData}");

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();

                request.SetRequestHeader("accesstoken", loginManager.AccessToken);
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[CloudSaveService] User data save successful. Response: {request.downloadHandler.text}");
                    UpdateStatus("User data saved successfully");
                    OnUserAppSaveCompleted?.Invoke(true);
                }
                else
                {
                    Debug.LogError($"[CloudSaveService] User data save failed: {request.error}");
                    Debug.LogError($"[CloudSaveService] Response: {request.downloadHandler.text}");
                    UpdateStatus($"User data save failed: {request.error}");
                    OnUserAppSaveCompleted?.Invoke(false);
                }
            }
        }

        private IEnumerator GetUserAppAllCoroutine()
        {
            string appId = GetAppId();
            if (string.IsNullOrEmpty(appId))
            {
                UpdateStatus("App ID not available");
                yield break;
            }

            string url = $"{USER_APP_BASE_URL}/{appId}/all";

            UpdateStatus("Loading all user data...");
            Debug.Log($"[CloudSaveService] Loading all user data from: {url}");

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("accesstoken", loginManager.AccessToken);

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[CloudSaveService] User app all load successful. Response: {request.downloadHandler.text}");

                    try
                    {
                        string jsonArray = request.downloadHandler.text;
                        UserAppData[] dataArray = JsonHelper.FromJson<UserAppData>(jsonArray);
                        List<UserAppData> dataList = new List<UserAppData>(dataArray);

                        OnUserAppAllLoaded?.Invoke(dataList);
                        UpdateStatus($"All user data loaded successfully ({dataList.Count} records)");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[CloudSaveService] Failed to parse user app all response: {ex.Message}");
                        Debug.LogError($"[CloudSaveService] Response was: {request.downloadHandler.text}");
                        UpdateStatus("Failed to parse all user data");
                    }
                }
                else
                {
                    Debug.LogError($"[CloudSaveService] User app all load failed: {request.error}");
                    Debug.LogError($"[CloudSaveService] Response code: {request.responseCode}");
                    Debug.LogError($"[CloudSaveService] Response: {request.downloadHandler.text}");
                    UpdateStatus($"Load all user data failed: {request.error}");
                }
            }
        }

        private IEnumerator DeleteUserAppDataCoroutine(long version)
        {
            string appId = GetAppId();
            if (string.IsNullOrEmpty(appId))
            {
                UpdateStatus("App ID not available");
                OnUserAppDeleteCompleted?.Invoke(false);
                yield break;
            }

            string url = $"{USER_APP_BASE_URL}/{appId}/version/{version}";

            UpdateStatus($"Deleting user data version {version}...");
            Debug.Log($"[CloudSaveService] Deleting user data from: {url}");

            using (UnityWebRequest request = UnityWebRequest.Delete(url))
            {
                if (loginManager == null || string.IsNullOrEmpty(loginManager.AccessToken))
                {
                    Debug.LogError("[CloudSaveService] LoginManager or AccessToken is null during delete operation");
                    UpdateStatus("Authentication error during delete");
                    OnUserAppDeleteCompleted?.Invoke(false);
                    yield break;
                }

                if (request.downloadHandler == null)
                {
                    request.downloadHandler = new DownloadHandlerBuffer();
                }

                request.SetRequestHeader("accesstoken", loginManager.AccessToken);

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[CloudSaveService] User data delete successful. Response: {(request.downloadHandler?.text ?? "null")}");

                    string responseText = request.downloadHandler?.text;
                    if (!string.IsNullOrEmpty(responseText))
                    {
                        try
                        {
                            UserAppDeleteResponse deleteResponse = JsonUtility.FromJson<UserAppDeleteResponse>(responseText);
                            if (deleteResponse != null && !string.IsNullOrEmpty(deleteResponse.message))
                            {
                                UpdateStatus($"Delete successful: {deleteResponse.message}");
                                Debug.Log($"[CloudSaveService] Server message: {deleteResponse.message}");
                            }
                            else
                            {
                                UpdateStatus($"User data version {version} deleted successfully");
                            }
                        }
                        catch (System.Exception parseEx)
                        {
                            Debug.LogWarning($"[CloudSaveService] Could not parse delete response JSON: {parseEx.Message}");
                            Debug.LogWarning($"[CloudSaveService] Raw response: {responseText}");
                            UpdateStatus($"User data version {version} deleted successfully");
                        }
                    }
                    else
                    {
                        UpdateStatus($"User data version {version} deleted successfully");
                    }

                    OnUserAppDeleteCompleted?.Invoke(true);
                }
                else
                {
                    Debug.LogError($"[CloudSaveService] User data delete failed: {request.error} (Code: {request.responseCode})");
                    Debug.LogError($"[CloudSaveService] Response: {(request.downloadHandler?.text ?? "null")}");
                    UpdateStatus($"Delete user data failed: {request.error}");
                    OnUserAppDeleteCompleted?.Invoke(false);
                }
            }
        }

        private string GetAppId()
        {
            if (loginManager != null)
            {
                var appIdField = typeof(LoginManager).GetField("appId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (appIdField != null)
                {
                    string appId = appIdField.GetValue(loginManager) as string;
                    if (!string.IsNullOrEmpty(appId))
                    {
                        Debug.Log($"[CloudSaveService] Using App ID from LoginManager: {appId}");
                        return appId;
                    }
                }
            }

            LoginSample loginSample = FindObjectOfType<LoginSample>();
            if (loginSample != null && !string.IsNullOrEmpty(loginSample.appId))
            {
                Debug.Log($"[CloudSaveService] Using App ID from LoginSample: {loginSample.appId}");
                return loginSample.appId;
            }

            Debug.LogError("[CloudSaveService] App ID not found in LoginManager or LoginSample!");
            return null;
        }

        private void UpdateStatus(string message)
        {
            try
            {
                // Add null check before invoking event
                OnStatusUpdated?.Invoke(message);
                Debug.Log($"[CloudSaveService] {message}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CloudSaveService] Error in UpdateStatus: {ex.Message}");
                Debug.LogError($"[CloudSaveService] Stack trace: {ex.StackTrace}");
                // Still log the original message even if event invocation fails
                Debug.Log($"[CloudSaveService] Original message: {message}");
            }
        }

        [ContextMenu("Test Load Cloud Save")]
        public void TestLoadCloudSave()
        {
            LoadCloudSave();
        }

        [ContextMenu("Test Save Sample Data")]
        public void TestSaveSampleData()
        {
            List<string> sampleInventory = new List<string> { "sword", "shield", "potion" };
            SaveToCloud(12, "warrior", sampleInventory);
        }

        [ContextMenu("Test Get UserApp Latest")]
        public void TestGetUserAppLatest()
        {
            GetUserAppLatest();
        }

        [ContextMenu("Test Save UserApp Data")]
        public void TestSaveUserAppData()
        {
            SaveUserAppData(15, 650);
        }

        [ContextMenu("Test Get UserApp All")]
        public void TestGetUserAppAll()
        {
            GetUserAppAll();
        }

        [ContextMenu("Check Login Status")]
        public void CheckLoginStatus()
        {
            if (loginManager == null)
            {
                Debug.Log("[CloudSaveService] LoginManager: Not assigned");
                return;
            }

            Debug.Log($"[CloudSaveService] Login Status: {loginManager.IsLoggedIn}");
            Debug.Log($"[CloudSaveService] Has Token: {!string.IsNullOrEmpty(loginManager.AccessToken)}");
            Debug.Log($"[CloudSaveService] Account ID: {loginManager.AccountId}");
            Debug.Log($"[CloudSaveService] App ID: {GetAppId()}");
        }
    }

    // Helper class for JSON array parsing
    public static class JsonHelper
    {
        public static T[] FromJson<T>(string json)
        {
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>("{\"Items\":" + json + "}");
            return wrapper.Items;
        }

        [System.Serializable]
        private class Wrapper<T>
        {
            public T[] Items;
        }
    }
}