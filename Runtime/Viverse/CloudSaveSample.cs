using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ViverseSDK.Login;

namespace ViverseSDK.CloudSave
{
    /// <summary>
    /// CloudSaveSample - Demo UI for cloud save functionality
    /// Shows how to integrate CloudSaveService with LoginManager
    /// </summary>
    public class CloudSaveSample : MonoBehaviour
    {
        [Header("Services")]
        public LoginSample loginSample;
        public CloudSaveService cloudSaveService;

        [Header("Cloud Save UI References")]
        public Button loadButton;
        public Button saveButton;
        public TMP_Text statusText;

        [Header("UserApp API UI References")]
        public Button getUserAppLatestButton;
        public Button saveUserAppButton;
        public Button getUserAppAllButton;
        public Button deleteUserAppButton;

        [Header("Sample Data UI")]
        public TMP_InputField levelInput;
        public TMP_InputField scoreInput;
        public TMP_InputField versionInput;

        [Header("Sample Data")]
        public int sampleLevel = 12;
        public string sampleClass = "warrior";
        public List<string> sampleInventory = new List<string> { "sword", "shield", "potion" };
        public int sampleScore = 600;

        [Header("Current Cloud Data Display")]
        [SerializeField] private CloudSaveDisplayData currentDisplayData;

        [Header("Current UserApp Data Display")]
        [SerializeField] private UserAppDisplayData currentUserAppData;

        [System.Serializable]
        public class CloudSaveDisplayData
        {
            [Header("Current Cloud Save")]
            public int level;
            public string className;
            public List<string> inventory = new List<string>();
            public string lastUpdated;
            public string recordId;
            public string userId;
            public string appId;
            public string createdAt;
            public string updatedAt;

            public void UpdateFromFullResponse(CloudSaveService.CloudSaveResponseWrapper response)
            {
                if (response?.data?.userdata != null)
                {
                    var userData = response.data.userdata;
                    level = userData.level;
                    className = userData.@class;
                    inventory = userData.inventory ?? new List<string>();
                    lastUpdated = userData.last_updated;
                    recordId = response.id;
                    userId = response.user_id;
                    appId = response.app_id;
                    createdAt = response.created_at;
                    updatedAt = response.updated_at;
                }
            }

            public void Clear()
            {
                level = 0;
                className = "";
                inventory = new List<string>();
                lastUpdated = "";
                recordId = "";
                userId = "";
                appId = "";
                createdAt = "";
                updatedAt = "";
            }

            public string GetDetailedDisplayString()
            {
                if (string.IsNullOrEmpty(className))
                    return "No cloud data loaded";

                string inventoryText = inventory != null && inventory.Count > 0
                    ? string.Join(", ", inventory)
                    : "Empty";

                return $"=== CLOUD SAVE DATA ===\n" +
                       $"Record ID: {recordId}\n" +
                       $"User ID: {userId}\n" +
                       $"App ID: {appId}\n" +
                       $"\n=== GAME DATA ===\n" +
                       $"Level: {level}\n" +
                       $"Class: {className}\n" +
                       $"Inventory: [{inventoryText}]\n" +
                       $"\n=== TIMESTAMPS ===\n" +
                       $"Last Updated: {lastUpdated}\n" +
                       $"Created At: {createdAt}\n" +
                       $"Updated At: {updatedAt}";
            }

            public string GetSummary()
            {
                if (string.IsNullOrEmpty(className))
                    return "No data";

                return $"Lv.{level} {className} ({inventory?.Count ?? 0} items)";
            }
        }

        [System.Serializable]
        public class UserAppDisplayData
        {
            [Header("Latest UserApp Data")]
            public int latestLevel;
            public int latestScore;
            public long latestVersion;
            public string latestCreatedAt;

            [Header("All UserApp Data")]
            public List<UserAppRecordDisplay> allRecords = new List<UserAppRecordDisplay>();

            public void UpdateFromLatest(CloudSaveService.UserAppLatestResponse response)
            {
                if (response != null)
                {
                    latestLevel = response.data.level;
                    latestScore = response.data.score;
                    latestVersion = response.version;
                    latestCreatedAt = response.created_at;
                }
            }

            public void UpdateFromAll(List<CloudSaveService.UserAppData> dataList)
            {
                allRecords.Clear();
                if (dataList != null)
                {
                    foreach (var data in dataList)
                    {
                        allRecords.Add(new UserAppRecordDisplay
                        {
                            level = data.data.level,
                            score = data.data.score,
                            version = data.version,
                            createdAt = data.created_at
                        });
                    }
                }
            }

            public void Clear()
            {
                latestLevel = 0;
                latestScore = 0;
                latestVersion = 0;
                latestCreatedAt = "";
                allRecords.Clear();
            }

            public string GetLatestDisplayString()
            {
                if (latestVersion == 0)
                    return "No user app data loaded";

                return $"=== LATEST USER DATA ===\n" +
                       $"Level: {latestLevel}\n" +
                       $"Score: {latestScore}\n" +
                       $"Version: {latestVersion}\n" +
                       $"Created At: {latestCreatedAt}";
            }

            public string GetAllDisplayString()
            {
                if (allRecords.Count == 0)
                    return "No user app data records";

                string result = $"=== ALL USER DATA ({allRecords.Count} records) ===\n";
                for (int i = 0; i < allRecords.Count; i++)
                {
                    var record = allRecords[i];
                    result += $"\nRecord {i + 1}:\n" +
                             $"Level: {record.level}, Score: {record.score}\n" +
                             $"Version: {record.version}\n" +
                             $"Created: {record.createdAt}";
                }
                return result;
            }
        }

        [System.Serializable]
        public class UserAppRecordDisplay
        {
            public int level;
            public int score;
            public long version;
            public string createdAt;
        }

        void Start()
        {
            currentDisplayData = new CloudSaveDisplayData();
            currentUserAppData = new UserAppDisplayData();

            if (loginSample == null)
                loginSample = FindObjectOfType<LoginSample>();

            CheckCloudSaveServiceAvailability();
            SetupUI();
            SubscribeToEvents();
            UpdateSampleDataUI();
        }

        private void CheckCloudSaveServiceAvailability()
        {
            if (cloudSaveService == null)
            {
                cloudSaveService = FindObjectOfType<CloudSaveService>();
            }

            if (cloudSaveService == null)
            {
                Debug.LogWarning("[CloudSaveSample] CloudSaveService not found in scene. Creating CloudSaveService automatically...");

                GameObject cloudSaveServiceObject = new GameObject("CloudSaveService");
                cloudSaveService = cloudSaveServiceObject.AddComponent<CloudSaveService>();

                Debug.Log("[CloudSaveSample] CloudSaveService created successfully. Cloud save functionality is now available.");
            }
            else
            {
                Debug.Log("[CloudSaveSample] CloudSaveService found in scene. Cloud save functionality is available.");
            }
        }

        private void SetupUI()
        {
            // Cloud Save UI
            if (loadButton != null)
                loadButton.onClick.AddListener(OnLoadButtonClick);

            if (saveButton != null)
                saveButton.onClick.AddListener(OnSaveButtonClick);

            // UserApp API UI
            if (getUserAppLatestButton != null)
                getUserAppLatestButton.onClick.AddListener(OnGetUserAppLatestButtonClick);

            if (saveUserAppButton != null)
                saveUserAppButton.onClick.AddListener(OnSaveUserAppButtonClick);

            if (getUserAppAllButton != null)
                getUserAppAllButton.onClick.AddListener(OnGetUserAppAllButtonClick);

            if (deleteUserAppButton != null)
                deleteUserAppButton.onClick.AddListener(OnDeleteUserAppButtonClick);

            if (statusText != null)
                statusText.text = "Ready - Please login first";
        }

        private void SubscribeToEvents()
        {
            if (cloudSaveService != null)
            {
                // Cloud Save Events
                cloudSaveService.OnFullDataLoaded += OnFullCloudDataLoaded;
                cloudSaveService.OnSaveCompleted += OnSaveCompleted;

                // UserApp API Events
                cloudSaveService.OnUserAppLatestLoaded += OnUserAppLatestLoaded;
                cloudSaveService.OnUserAppAllLoaded += OnUserAppAllLoaded;
                cloudSaveService.OnUserAppSaveCompleted += OnUserAppSaveCompleted;
                cloudSaveService.OnUserAppDeleteCompleted += OnUserAppDeleteCompleted;
            }

            if (loginSample != null)
            {
                var loginManager = loginSample.GetComponent<LoginManager>();
                if (loginManager != null)
                {
                    loginManager.OnLoginStateChanged += OnLoginStateChanged;
                }
            }
        }

        private void OnDestroy()
        {
            if (cloudSaveService != null)
            {
                // Cloud Save Events
                cloudSaveService.OnFullDataLoaded -= OnFullCloudDataLoaded;
                cloudSaveService.OnSaveCompleted -= OnSaveCompleted;

                // UserApp API Events
                cloudSaveService.OnUserAppLatestLoaded -= OnUserAppLatestLoaded;
                cloudSaveService.OnUserAppAllLoaded -= OnUserAppAllLoaded;
                cloudSaveService.OnUserAppSaveCompleted -= OnUserAppSaveCompleted;
                cloudSaveService.OnUserAppDeleteCompleted -= OnUserAppDeleteCompleted;
            }

            if (loginSample != null)
            {
                var loginManager = loginSample.GetComponent<LoginManager>();
                if (loginManager != null)
                {
                    loginManager.OnLoginStateChanged -= OnLoginStateChanged;
                }
            }
        }

        private void UpdateSampleDataUI()
        {
            if (levelInput != null)
                levelInput.text = sampleLevel.ToString();

            if (scoreInput != null)
                scoreInput.text = sampleScore.ToString();
        }

        // Cloud Save Button Handlers
        public void OnLoadButtonClick()
        {
            if (!ValidateServices())
                return;

            Debug.Log("[CloudSaveSample] Loading cloud save data...");
            UpdateStatus("Loading cloud save...");
            cloudSaveService.LoadCloudSave();
        }

        public void OnSaveButtonClick()
        {
            if (!ValidateServices())
                return;

            CloudSaveService.CloudSaveData saveData = GetDataFromUI();

            Debug.Log($"[CloudSaveSample] Saving data - Level: {saveData.level}, Class: {saveData.@class}");
            UpdateStatus($"Saving Level {saveData.level} {saveData.@class}...");
            cloudSaveService.SaveToCloud(saveData);
        }

        // UserApp API Button Handlers
        public void OnGetUserAppLatestButtonClick()
        {
            if (!ValidateServices())
                return;

            Debug.Log("[CloudSaveSample] Getting latest user app data...");
            UpdateStatus("Getting latest user data...");
            cloudSaveService.GetUserAppLatest();
        }

        public void OnSaveUserAppButtonClick()
        {
            if (!ValidateServices())
                return;

            int level = GetLevelFromInput();
            int score = GetScoreFromInput();

            Debug.Log($"[CloudSaveSample] Saving user app data - Level: {level}, Score: {score}");
            UpdateStatus($"Saving user data Level: {level}, Score: {score}...");
            cloudSaveService.SaveUserAppData(level, score);
        }

        public void OnGetUserAppAllButtonClick()
        {
            if (!ValidateServices())
                return;

            Debug.Log("[CloudSaveSample] Getting all user app data...");
            UpdateStatus("Getting all user data...");
            cloudSaveService.GetUserAppAll();
        }

        public void OnDeleteUserAppButtonClick()
        {
            if (!ValidateServices())
                return;

            long version = GetVersionFromInput();
            if (version <= 0)
            {
                UpdateStatus("Please enter a valid version number to delete");
                return;
            }

            Debug.Log($"[CloudSaveSample] Deleting user app data version: {version}");
            UpdateStatus($"Deleting user data version: {version}...");
            cloudSaveService.DeleteUserAppData(version);
        }

        private CloudSaveService.CloudSaveData GetDataFromUI()
        {
            CloudSaveService.CloudSaveData data = new CloudSaveService.CloudSaveData();

            if (levelInput != null && int.TryParse(levelInput.text, out int level))
                data.level = level;
            else
                data.level = sampleLevel;

            if (HasCloudData())
            {
                data.@class = currentDisplayData.className;
                data.inventory = new List<string>(currentDisplayData.inventory);
            }
            else
            {
                data.@class = sampleClass;
                data.inventory = new List<string>(sampleInventory);
            }

            return data;
        }

        private int GetLevelFromInput()
        {
            if (levelInput != null && int.TryParse(levelInput.text, out int level))
                return level;
            return sampleLevel;
        }

        private int GetScoreFromInput()
        {
            if (scoreInput != null && int.TryParse(scoreInput.text, out int score))
                return score;
            return sampleScore;
        }

        private long GetVersionFromInput()
        {
            if (versionInput != null && long.TryParse(versionInput.text, out long version))
                return version;
            return 0;
        }

        private bool ValidateServices()
        {
            if (cloudSaveService == null)
            {
                UpdateStatus("CloudSaveService not found");
                Debug.LogError("[CloudSaveSample] CloudSaveService is not assigned");
                return false;
            }

            if (loginSample == null || !loginSample.IsLoggedIn())
            {
                UpdateStatus("Please login first");
                Debug.LogWarning("[CloudSaveSample] User is not logged in");
                return false;
            }

            return true;
        }

        private void UpdateStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;

            Debug.Log($"[CloudSaveSample] Status: {message}");
        }

        // Cloud Save Event Handlers
        private void OnFullCloudDataLoaded(CloudSaveService.CloudSaveResponseWrapper fullResponse)
        {
            if (fullResponse != null && fullResponse.data?.userdata != null)
            {
                currentDisplayData.UpdateFromFullResponse(fullResponse);

                if (levelInput != null)
                    levelInput.text = fullResponse.data.userdata.level.ToString();

                UpdateStatus(currentDisplayData.GetDetailedDisplayString());

                Debug.Log($"[CloudSaveSample] Full cloud data loaded and displayed - {currentDisplayData.GetSummary()}");
            }
            else
            {
                currentDisplayData.Clear();
                UpdateStatus("No cloud save found");
                Debug.Log("[CloudSaveSample] No cloud data available");
            }
        }

        private void OnSaveCompleted(bool success)
        {
            if (success)
            {
                Debug.Log("[CloudSaveSample] Cloud save completed successfully");

                var savedData = GetDataFromUI();
                currentDisplayData.level = savedData.level;
                currentDisplayData.className = savedData.@class;
                currentDisplayData.inventory = savedData.inventory ?? new List<string>();
                currentDisplayData.lastUpdated = System.DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ");

                UpdateStatus("Cloud save completed successfully");
            }
            else
            {
                UpdateStatus("Cloud save failed");
                Debug.LogError("[CloudSaveSample] Cloud save failed");
            }
        }

        // UserApp API Event Handlers
        private void OnUserAppLatestLoaded(CloudSaveService.UserAppLatestResponse response)
        {
            if (response != null)
            {
                currentUserAppData.UpdateFromLatest(response);
                UpdateStatus(currentUserAppData.GetLatestDisplayString());
                Debug.Log($"[CloudSaveSample] Latest user app data loaded - Level: {response.data.level}, Score: {response.data.score}");
            }
            else
            {
                UpdateStatus("No latest user app data found");
            }
        }

        private void OnUserAppAllLoaded(List<CloudSaveService.UserAppData> dataList)
        {
            currentUserAppData.UpdateFromAll(dataList);
            UpdateStatus(currentUserAppData.GetAllDisplayString());
            Debug.Log($"[CloudSaveSample] All user app data loaded - {dataList?.Count ?? 0} records");
        }

        private void OnUserAppSaveCompleted(bool success)
        {
            if (success)
            {
                UpdateStatus("User app data saved successfully");
                Debug.Log("[CloudSaveSample] User app data save completed successfully");
            }
            else
            {
                UpdateStatus("User app data save failed");
                Debug.LogError("[CloudSaveSample] User app data save failed");
            }
        }

        private void OnUserAppDeleteCompleted(bool success)
        {
            if (success)
            {
                // Clear the version input field after successful deletion to prevent re-use
                if (versionInput != null)
                {
                    versionInput.text = "";
                }

                // The status message is already set by CloudSaveService based on server response
                Debug.Log("[CloudSaveSample] User app data delete completed successfully");

                // Auto-refresh the all data to show current state after deletion
                if (cloudSaveService != null)
                {
                    Debug.Log("[CloudSaveSample] Refreshing user app data after successful deletion...");
                    // Add a small delay before refreshing to ensure server has processed the deletion
                    StartCoroutine(RefreshDataAfterDelete());
                }
            }
            else
            {
                // Status message is already set by CloudSaveService
                Debug.LogError("[CloudSaveSample] User app data delete failed");
            }
        }

        // Helper coroutine to refresh data after deletion
        private System.Collections.IEnumerator RefreshDataAfterDelete()
        {
            // Wait a moment for server to process the deletion
            yield return new WaitForSeconds(0.5f);

            // Refresh the data to show current state
            if (cloudSaveService != null)
            {
                cloudSaveService.GetUserAppAll();
            }
        }

        private void OnLoginStateChanged(bool isLoggedIn)
        {
            // Update all button interactability based on login state
            if (loadButton != null)
                loadButton.interactable = isLoggedIn;

            if (saveButton != null)
                saveButton.interactable = isLoggedIn;

            if (getUserAppLatestButton != null)
                getUserAppLatestButton.interactable = isLoggedIn;

            if (saveUserAppButton != null)
                saveUserAppButton.interactable = isLoggedIn;

            if (getUserAppAllButton != null)
                getUserAppAllButton.interactable = isLoggedIn;

            if (deleteUserAppButton != null)
                deleteUserAppButton.interactable = isLoggedIn;

            if (isLoggedIn)
            {
                UpdateStatus("Logged in - Ready to use cloud save");
            }
            else
            {
                UpdateStatus("Please login to use cloud save");
                currentDisplayData.Clear();
                currentUserAppData.Clear();
            }
        }

        private string GetAppIdFromService()
        {
            if (loginSample != null && !string.IsNullOrEmpty(loginSample.appId))
                return loginSample.appId;
            return "8yra9vgedr";
        }

        /// <summary>
        /// Get current level from cloud save data
        /// </summary>
        public int GetCurrentLevel() => currentDisplayData.level;

        /// <summary>
        /// Get current class name from cloud save data
        /// </summary>
        public string GetCurrentClassName() => currentDisplayData.className;

        /// <summary>
        /// Get current inventory items from cloud save data
        /// </summary>
        public List<string> GetCurrentInventory() => new List<string>(currentDisplayData.inventory);

        /// <summary>
        /// Get last updated timestamp from cloud save data
        /// </summary>
        public string GetCurrentLastUpdated() => currentDisplayData.lastUpdated;

        /// <summary>
        /// Get summary of current cloud save data
        /// </summary>
        public string GetCurrentDataSummary() => currentDisplayData.GetSummary();

        /// <summary>
        /// Check if cloud save data is available
        /// </summary>
        public bool HasCloudData() => !string.IsNullOrEmpty(currentDisplayData.className);

        /// <summary>
        /// Get current user app level
        /// </summary>
        public int GetCurrentUserAppLevel() => currentUserAppData.latestLevel;

        /// <summary>
        /// Get current user app score
        /// </summary>
        public int GetCurrentUserAppScore() => currentUserAppData.latestScore;

        /// <summary>
        /// Get current user app version
        /// </summary>
        public long GetCurrentUserAppVersion() => currentUserAppData.latestVersion;

        [ContextMenu("Test Load Cloud Save")]
        public void TestLoadCloudSave()
        {
            OnLoadButtonClick();
        }

        [ContextMenu("Test Save Sample Data")]
        public void TestSaveSampleData()
        {
            OnSaveButtonClick();
        }

        [ContextMenu("Test Get UserApp Latest")]
        public void TestGetUserAppLatest()
        {
            OnGetUserAppLatestButtonClick();
        }

        [ContextMenu("Test Save UserApp Data")]
        public void TestSaveUserAppData()
        {
            OnSaveUserAppButtonClick();
        }

        [ContextMenu("Test Get UserApp All")]
        public void TestGetUserAppAll()
        {
            OnGetUserAppAllButtonClick();
        }

        [ContextMenu("Display Current Cloud Data")]
        public void DisplayCurrentCloudData()
        {
            Debug.Log($"[CloudSaveSample] Current Cloud Data:\n{currentDisplayData.GetDetailedDisplayString()}");
            UpdateStatus(currentDisplayData.GetDetailedDisplayString());
        }

        [ContextMenu("Display Current UserApp Data")]
        public void DisplayCurrentUserAppData()
        {
            Debug.Log($"[CloudSaveSample] Current UserApp Data:\n{currentUserAppData.GetLatestDisplayString()}");
            UpdateStatus(currentUserAppData.GetLatestDisplayString());
        }

        [ContextMenu("Clear Display Data")]
        public void ClearDisplayData()
        {
            currentDisplayData.Clear();
            currentUserAppData.Clear();
            UpdateStatus("Display data cleared");
        }

        [ContextMenu("Check Services Status")]
        public void CheckServicesStatus()
        {
            Debug.Log($"[CloudSaveSample] LoginSample: {(loginSample != null ? "Found" : "Missing")}");
            Debug.Log($"[CloudSaveSample] CloudSaveService: {(cloudSaveService != null ? "Found" : "Missing")}");

            if (loginSample != null)
            {
                Debug.Log($"[CloudSaveSample] Login Status: {loginSample.IsLoggedIn()}");
                Debug.Log($"[CloudSaveSample] Has Token: {!string.IsNullOrEmpty(loginSample.GetAccessToken())}");
            }

            Debug.Log($"[CloudSaveSample] Current Cloud Data: {currentDisplayData.GetSummary()}");
            UpdateStatus($"Services Status:\nLoginSample: {(loginSample != null ? "Found" : "Missing")}\nCloudSaveService: {(cloudSaveService != null ? "Found" : "Missing")}\nCurrent Data: {currentDisplayData.GetSummary()}");
        }

        [ContextMenu("Check CloudSaveService Availability")]
        public void ManualCheckCloudSaveServiceAvailability()
        {
            CheckCloudSaveServiceAvailability();
        }

        [ContextMenu("Force Create CloudSaveService")]
        public void ForceCreateCloudSaveService()
        {
            CloudSaveService existingService = FindObjectOfType<CloudSaveService>();
            if (existingService != null)
            {
                Debug.Log("[CloudSaveSample] CloudSaveService already exists in scene.");
                UpdateStatus("CloudSaveService already exists");
                return;
            }

            GameObject cloudSaveServiceObject = new GameObject("CloudSaveService");
            CloudSaveService newCloudSaveService = cloudSaveServiceObject.AddComponent<CloudSaveService>();

            cloudSaveService = newCloudSaveService;
            SubscribeToEvents();

            Debug.Log("[CloudSaveSample] CloudSaveService created manually. Cloud save functionality is now available.");
            UpdateStatus("CloudSaveService created successfully");
        }
    }
}