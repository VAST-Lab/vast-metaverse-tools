using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ViverseSDK.Login;

public class LeaderboardSample : MonoBehaviour
{
    [Header("Leaderboard Settings")]
    public string appId = "";
    public string accessToken = "";
    public string apiName = "";  // Existing Meta name
    
    [Header("Data Type Settings")]
    [Tooltip("Select the data type for this leaderboard - MUST match server configuration\nServer: 1=Numeric, 2=Seconds, 3=Milliseconds")]
    public LeaderboardDataType dataType = LeaderboardDataType.Numeric;
    
    [Header("Score Input by Data Type")]
    [Tooltip("For Numeric: Direct score value")]
    public int numericScore = 100;
    
    [Tooltip("For Seconds: Time in seconds (e.g., 65.5 = 1 min 5.5 sec)")]
    public float secondsTime = 65.5f;
    
    [Tooltip("For Milliseconds: Time in milliseconds (e.g., 65500 = 65.5 seconds)")]
    public int millisecondsTime = 65500;

    [Header("UI References")]
    public Button uploadScoreButton;
    public Button getLeaderboardButton;
    public Button validateDataTypeButton; // New validation button
    public Transform leaderboardContainer;
    public GameObject leaderboardEntryPrefab;
    public TMP_Text statusText;

    private LeaderboardService _service;

    void Start()
    {
        _service = GetComponent<LeaderboardService>();
        if (_service == null)
        {
            _service = gameObject.AddComponent<LeaderboardService>();
        }

        SetupUIButtons();
    }

    private void SetupUIButtons()
    {
        if (uploadScoreButton != null)
            uploadScoreButton.onClick.AddListener(OnUploadScoreClicked);

        if (getLeaderboardButton != null)
            getLeaderboardButton.onClick.AddListener(OnGetLeaderboardClicked);

        if (validateDataTypeButton != null)
            validateDataTypeButton.onClick.AddListener(OnValidateDataTypeClicked);
    }

    // Get the appropriate score value based on data type
    private float GetScoreByDataType()
    {
        switch (dataType)
        {
            case LeaderboardDataType.Numeric:
                return numericScore;
            case LeaderboardDataType.Seconds:
                return secondsTime;
            case LeaderboardDataType.Milliseconds:
                return millisecondsTime;
            default:
                return numericScore; 
        }
    }

    // Get access token - use saved token from LoginManager if accessToken is empty
    private string GetAccessToken()
    {
        if (!string.IsNullOrEmpty(accessToken))
        {
            Debug.Log("[LeaderboardSample] Using accessToken from inspector");
            return accessToken;
        }

        string savedToken = PlayerPrefs.GetString("access_token", "");
        if (!string.IsNullOrEmpty(savedToken))
        {
            Debug.Log("[LeaderboardSample] Using saved access token from LoginManager");
            return savedToken;
        }

        Debug.LogWarning("[LeaderboardSample] No access token available. Please login first or set accessToken in inspector.");
        return "";
    }

    // Get App ID - use LoginSample's appId if LeaderboardSample's appId is empty
    private string GetAppId()
    {
        if (!string.IsNullOrEmpty(appId))
        {
            Debug.Log($"[LeaderboardSample] Using appId from LeaderboardSample: {appId}");
            return appId;
        }

        // Try to find LoginSample in scene and get its appId
        LoginSample loginSample = FindObjectOfType<LoginSample>();
        if (loginSample != null && !string.IsNullOrEmpty(loginSample.appId))
        {
            Debug.Log($"[LeaderboardSample] Using appId from LoginSample: {loginSample.appId}");
            return loginSample.appId;
        }

        Debug.LogError("[LeaderboardSample] No appId available. Please set appId in LeaderboardSample or ensure LoginSample is in the scene with valid appId.");
        return "";
    }

    // Validate required parameters before making API calls
    private bool ValidateParameters(out string currentAppId, out string currentAccessToken, out string currentApiName)
    {
        currentAppId = GetAppId();
        currentAccessToken = GetAccessToken();
        currentApiName = apiName;

        if (string.IsNullOrEmpty(currentAppId))
        {
            UpdateStatusText("Error: App ID is required");
            return false;
        }

        if (string.IsNullOrEmpty(currentAccessToken))
        {
            UpdateStatusText("Error: No access token available. Please login first.");
            return false;
        }

        if (string.IsNullOrEmpty(currentApiName))
        {
            Debug.LogError("Please set API Name Visit https://studio.viverse.com/upload.");
            UpdateStatusText("Error: API Name is required. Please set apiName in inspector.");
            return false;
        }

        return true;
    }

    public async void OnUploadScoreClicked()
    {
        await UploadScoreAsync();
    }

    public async void OnGetLeaderboardClicked()
    {
        await GetLeaderboardAsync();
    }

    public async void OnValidateDataTypeClicked()
    {
        await ValidateDataTypeAsync();
    }

    /// <summary>
    /// Validate data type against server configuration
    /// </summary>
    private async Task ValidateDataTypeAsync()
    {
        UpdateStatusText("Validating data type...");

        if (!ValidateParameters(out string currentAppId, out string currentAccessToken, out string currentApiName))
            return;

        var validationResult = await _service.ValidateDataTypeAsync(currentAppId, currentAccessToken, currentApiName, dataType);

        if (validationResult.IsValid)
        {
            UpdateStatusText($"Data type validation passed: {dataType}");
            Debug.Log($"[LeaderboardSample] Data type matches server configuration: {dataType}");
        }
        else
        {
            UpdateStatusText($" Data Type Error");
            Debug.LogError($"[LeaderboardSample] Data type validation failed:");
            Debug.LogError($"[LeaderboardSample] {validationResult.ErrorMessage}");
        }
    }

    private async Task UploadScoreAsync()
    {
        UpdateStatusText("Uploading score with data type validation...");

        if (!ValidateParameters(out string currentAppId, out string currentAccessToken, out string currentApiName))
            return;

        float scoreValue = GetScoreByDataType();
        
        // SubmitScoreWithDataTypeAsync now includes automatic validation
        bool success = await _service.SubmitScoreWithDataTypeAsync(
            currentAppId,
            currentAccessToken,
            currentApiName,
            scoreValue,
            dataType
        );

        if (success)
        {
            string formattedScore = FormatScoreDisplay(scoreValue, dataType);
            UpdateStatusText($"Score uploaded successfully! Score: {formattedScore}");
            Debug.Log($"[LeaderboardSample] Score submission successful! Meta: {currentApiName}, Score: {formattedScore}, Type: {dataType}");
        }
        else
        {
            UpdateStatusText("Failed to upload score (may be blocked by data type mismatch)");
            Debug.LogError($"[LeaderboardSample] Score submission failed. Meta: {currentApiName}, Score: {scoreValue}, Type: {dataType}");
        }
    }

    private async Task GetLeaderboardAsync()
    {
        UpdateStatusText("Fetching leaderboard...");

        if (!ValidateParameters(out string currentAppId, out string currentAccessToken, out string currentApiName))
            return;

        var recordsResponse = await _service.GetLeaderboardRecordsAsync(
            currentAppId,
            currentAccessToken,
            currentApiName,
            0, 9 // Get top 10 records
        );

        if (recordsResponse != null && recordsResponse.ranking != null && recordsResponse.ranking.Length > 0)
        {
            // Check data type from response
            if (recordsResponse.meta != null)
            {
                LeaderboardDataType serverDataType = (LeaderboardDataType)recordsResponse.meta.data_type;
                if (serverDataType != dataType)
                {
                    string warning = $"Warning: Server data type ({serverDataType}) differs from client setting ({dataType})";
                    Debug.LogWarning($"[LeaderboardSample] {warning}");
                    UpdateStatusText(warning);
                    return;
                }
            }

            DisplayLeaderboard(recordsResponse);
            UpdateStatusText($"Leaderboard loaded - {recordsResponse.ranking.Length} players");
            Debug.Log($"[LeaderboardSample] Successfully retrieved {recordsResponse.ranking.Length} records");
        }
        else
        {
            UpdateStatusText("Failed to retrieve leaderboard data or no data found");
            Debug.LogWarning("[LeaderboardSample] Failed to retrieve records or no records found.");
        }
    }

    private void DisplayLeaderboard(LeaderboardRankingResponse response)
    {
        if (leaderboardContainer == null)
        {
            Debug.LogWarning("[LeaderboardSample] Leaderboard container not assigned");
            UpdateStatusText("Error: Leaderboard container not assigned");
            return;
        }

        ClearLeaderboardDisplay();

        var sortedRanking = response.ranking;
        Array.Sort(sortedRanking, (x, y) => x.rank.CompareTo(y.rank));

        foreach (var record in sortedRanking)
        {
            CreateLeaderboardEntry(record);
        }

        Debug.Log("[LeaderboardSample] Leaderboard display completed");
    }

    private void CreateLeaderboardEntry(LeaderboardRecord record)
    {
        GameObject entryObj;

        if (leaderboardEntryPrefab != null)
        {
            entryObj = Instantiate(leaderboardEntryPrefab, leaderboardContainer);
        }
        else
        {
            // Create simple text entry if no prefab
            entryObj = new GameObject($"Entry_Rank{record.rank}");
            entryObj.transform.SetParent(leaderboardContainer);
            var rectTransform = entryObj.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(400, 30);
            var tmpText = entryObj.AddComponent<TMP_Text>();
            tmpText.fontSize = 18;
            tmpText.color = Color.white;
        }

        // Update text components with formatted score based on data type
        TMP_Text[] tmpTextComponents = entryObj.GetComponentsInChildren<TMP_Text>();
        Text[] textComponents = entryObj.GetComponentsInChildren<Text>();

        string formattedScore = FormatScoreDisplay(record.value, dataType);
        string displayText = $"#{record.rank + 1} - {record.name} - {formattedScore}";

        if (tmpTextComponents.Length >= 3)
        {
            tmpTextComponents[0].text = $"#{record.rank + 1}";
            tmpTextComponents[1].text = record.name;
            tmpTextComponents[2].text = formattedScore;
        }
        else if (tmpTextComponents.Length == 1)
        {
            tmpTextComponents[0].text = displayText;
        }
        else if (textComponents.Length >= 3)
        {
            textComponents[0].text = $"#{record.rank + 1}";
            textComponents[1].text = record.name;
            textComponents[2].text = formattedScore;
        }
        else if (textComponents.Length == 1)
        {
            textComponents[0].text = displayText;
        }
    }

    private string FormatScoreDisplay(float value, LeaderboardDataType displayDataType)
    {
        switch (displayDataType)
        {
            case LeaderboardDataType.Numeric:
                return $"{value:F0} pts";
            
            case LeaderboardDataType.Seconds:
                // Convert seconds to mm:ss.ff format
                int minutes = Mathf.FloorToInt(value / 60f);
                float seconds = value % 60f;
                return $"{minutes:00}:{seconds:00.00}";
            
            case LeaderboardDataType.Milliseconds:
                // Convert milliseconds to mm:ss.fff format
                int totalMs = Mathf.FloorToInt(value);
                int mins = totalMs / 60000;
                int secs = (totalMs % 60000) / 1000;
                int ms = totalMs % 1000;
                return $"{mins:00}:{secs:00}.{ms:000}";
            
            default:
                return $"{value:F0} pts";
        }
    }

    private void ClearLeaderboardDisplay()
    {
        if (leaderboardContainer == null) return;

        for (int i = leaderboardContainer.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(leaderboardContainer.GetChild(i).gameObject);
        }
    }

    private void UpdateStatusText(string message)
    {
        if (statusText != null)
            statusText.text = message;
        Debug.Log($"[LeaderboardSample] Status: {message}");
    }

    // Context menu methods for testing
    [ContextMenu("Submit Score")]
    public async void TestSubmitScore()
    {
        await UploadScoreAsync();
    }

    [ContextMenu("Get Leaderboard")]
    public async void TestGetLeaderboard()
    {
        await GetLeaderboardAsync();
    }

    [ContextMenu("Validate Data Type")]
    public async void TestValidateDataType()
    {
        await ValidateDataTypeAsync();
    }

    [ContextMenu("Check Parameters")]
    public void CheckAllParameters()
    {
        string currentAppId = GetAppId();
        string currentAccessToken = GetAccessToken();
        string currentApiName = apiName;

        Debug.Log($"[LeaderboardSample] App ID: {(string.IsNullOrEmpty(currentAppId) ? "Missing" : $" {currentAppId}")}");
        Debug.Log($"[LeaderboardSample] Access Token: {(string.IsNullOrEmpty(currentAccessToken) ? "Missing" : " Available")}");
        Debug.Log($"[LeaderboardSample] API Name: {(string.IsNullOrEmpty(currentApiName) ? "Missing" : $" {currentApiName}")}");
        Debug.Log($"[LeaderboardSample] Data Type: {dataType}");
        Debug.Log($"[LeaderboardSample] Current Score: {FormatScoreDisplay(GetScoreByDataType(), dataType)}");

        if (ValidateParameters(out _, out _, out _))
        {
            UpdateStatusText("All parameters are valid ");
        }
        else
        {
            UpdateStatusText("Some parameters are missing ");
        }
    }

    [ContextMenu("Test All Data Types")]
    public void TestAllDataTypes()
    {
        Debug.Log($"[LeaderboardSample] Numeric: {FormatScoreDisplay(numericScore, LeaderboardDataType.Numeric)}");
        Debug.Log($"[LeaderboardSample] Seconds: {FormatScoreDisplay(secondsTime, LeaderboardDataType.Seconds)}");
        Debug.Log($"[LeaderboardSample] Milliseconds: {FormatScoreDisplay(millisecondsTime, LeaderboardDataType.Milliseconds)}");
    }
}