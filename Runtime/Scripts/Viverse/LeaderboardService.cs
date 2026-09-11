using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using System.Security.Cryptography;
using System.Linq;

[Serializable]
public class LeaderboardRecord
{
    public string uid;      
    public string name;    
    public float value;     
    public int rank;
    public long updatedTime; 
    
    // Properties for backwards compatibility
    public string userId => uid;
    public int score => (int)value;
}

[Serializable]
public class LeaderboardMeta
{
    public string app_id;
    public string meta_name;
    public LeaderboardDisplayName[] display_name;
    public int sort_type;
    public int update_type;
    public int data_type; // 1=Numeric, 2=Seconds, 3=Milliseconds
}

[Serializable]
public class LeaderboardDisplayName
{
    public string lang;
    public string name;
}

[Serializable]
public class LeaderboardRankingResponse
{
    public LeaderboardRecord[] ranking;  // Changed from records to ranking
    public LeaderboardMeta meta;         // Changed from string apiName to LeaderboardMeta
    public int total_count;              // Changed from totalCount to total_count

    // Properties for backwards compatibility
    public LeaderboardRecord[] records => ranking;
    public string apiName => meta?.meta_name ?? "";
    public int totalCount => total_count;
}

// Score entry model
[Serializable]
public class ScoreEntry
{
    public string name;  // Leaderboard Meta Name
    public string value; // Score value (string format)
}

// Scores payload container
[Serializable]
public class ScoresPayload
{
    public ScoreEntry[] scores;
}

// Data type validation result
public class DataTypeValidationResult
{
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; }
    public LeaderboardDataType ServerDataType { get; set; }
    public LeaderboardDataType ClientDataType { get; set; }

    public DataTypeValidationResult(bool isValid, string errorMessage = "", 
        LeaderboardDataType serverType = LeaderboardDataType.Numeric, 
        LeaderboardDataType clientType = LeaderboardDataType.Numeric)
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
        ServerDataType = serverType;
        ClientDataType = clientType;
    }
}

/// <summary>
/// HTC Leaderboard SDK Service for Unity
/// Includes create, query and secure score submission features for Production environment
/// </summary>
public class LeaderboardService : MonoBehaviour
{
    // Production environment Base API URL
    private const string PROD_BASE_HOST = "https://www.viveport.com/";
    private const string LEADERBOARD_API_URL_PREFIX = PROD_BASE_HOST + "api/vrleaderboard/v1/apps/";
    private const string IRONHIDE_API_URL_PREFIX = PROD_BASE_HOST + "api/ironhide/v1/token";

    #region Data Type Validation

    /// <summary>
    /// Convert server data_type integer to LeaderboardDataType enum
    /// Server: 1=Numeric, 2=Seconds, 3=Milliseconds
    /// Client: 1=Numeric, 2=Seconds, 3=Milliseconds
    /// </summary>
    private LeaderboardDataType ConvertServerDataTypeToEnum(int serverDataType)
    {
        switch (serverDataType)
        {
            case 1:
                return LeaderboardDataType.Numeric;
            case 2:
                return LeaderboardDataType.Seconds;
            case 3:
                return LeaderboardDataType.Milliseconds;
            default:
                Debug.LogWarning($"[LeaderboardService] Unknown server data type: {serverDataType}, defaulting to Numeric");
                return LeaderboardDataType.Numeric;
        }
    }

    /// <summary>
    /// Validate if client data type matches server data type
    /// </summary>
    public async Task<DataTypeValidationResult> ValidateDataTypeAsync(string appId, string userAuthKey, string apiName, LeaderboardDataType clientDataType)
    {
        Debug.Log($"[LeaderboardService] Validating data type for: {apiName}, Client Type: {clientDataType}");

        try
        {
            // First try to get data type from existing leaderboard records
            var rankingResponse = await GetLeaderboardRecordsForValidationAsync(appId, userAuthKey, apiName);
            
            if (rankingResponse?.meta != null)
            {
                LeaderboardDataType serverDataType = ConvertServerDataTypeToEnum(rankingResponse.meta.data_type);
                Debug.Log($"[LeaderboardService] Server data_type: {rankingResponse.meta.data_type} -> {serverDataType}");

                if (clientDataType != serverDataType)
                {
                    string errorMsg = $"Data Type Mismatch!\n" +
                                     $"Server expects: {GetDataTypeDisplayName(serverDataType)} (data_type={rankingResponse.meta.data_type})\n" +
                                     $"Client configured: {GetDataTypeDisplayName(clientDataType)}\n" +
                                     $"Please change Unity Inspector 'Data Type' setting to match server configuration.";
                    
                    Debug.LogError($"[LeaderboardService] {errorMsg}");
                    
                    return new DataTypeValidationResult(false, errorMsg, serverDataType, clientDataType);
                }

                Debug.Log($"[LeaderboardService] Data type validation passed: {clientDataType} matches server type {serverDataType}");
                return new DataTypeValidationResult(true, "", serverDataType, clientDataType);
            }
            else
            {
                // If no records exist yet, try to get meta info directly (fallback)
                Debug.LogWarning($"[LeaderboardService] No existing records found for {apiName}, cannot validate data type from ranking response");
                return new DataTypeValidationResult(false, 
                    $"Cannot validate data type for '{apiName}'. No existing records found. Please check if the API Name exists or submit a score first.");
            }
        }
        catch (Exception e)
        {
            string errorMsg = $"Data type validation failed: {e.Message}";
            Debug.LogError($"[LeaderboardService] {errorMsg}");
            return new DataTypeValidationResult(false, errorMsg);
        }
    }

    /// <summary>
    /// Get leaderboard records specifically for data type validation (lighter call)
    /// </summary>
    private async Task<LeaderboardRankingResponse> GetLeaderboardRecordsForValidationAsync(string appId, string userAuthKey, string apiName)
    {
        string baseUrl = LEADERBOARD_API_URL_PREFIX + appId + "/metas/ranking";
        string url = $"{baseUrl}?name={apiName}&range_start=0&range_end=0&region=global&time_range=alltime&around_user=false";

        using (var req = UnityWebRequest.Get(url))
        {
            req.timeout = 15; // Shorter timeout for validation
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("accesstoken", userAuthKey);

            var operation = req.SendWebRequest();
            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (req.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = req.downloadHandler.text;
                Debug.Log($"[LeaderboardService] Validation call success. Response: {jsonResponse}");

                try
                {
                    return JsonUtility.FromJson<LeaderboardRankingResponse>(jsonResponse);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[LeaderboardService] Failed to parse validation response: {e.Message}");
                    return null;
                }
            }
            else
            {
                Debug.LogWarning($"[LeaderboardService] Validation call failed. Error: {req.error}. Status Code: {req.responseCode}");
                return null;
            }
        }
    }

    private string GetDataTypeDisplayName(LeaderboardDataType dataType)
    {
        switch (dataType)
        {
            case LeaderboardDataType.Numeric:
                return "Numeric (Points/Score)";
            case LeaderboardDataType.Seconds:
                return "Seconds (Time Format)";
            case LeaderboardDataType.Milliseconds:
                return "Milliseconds (Time Format)";
            default:
                return dataType.ToString();
        }
    }

    #endregion

    #region Get Leaderboard Records
    /// <summary>
    /// Get ranking records for specified Leaderboard 
    /// </summary>
    public async Task<LeaderboardRankingResponse> GetLeaderboardRecordsAsync(string appId, string userAuthKey, string apiName, int rangeStart = 0, int rangeEnd = 3)
    {
        Debug.Log($"[LeaderboardService] Attempting to get leaderboard records for: {apiName}...");

        string baseUrl = LEADERBOARD_API_URL_PREFIX + appId + "/metas/ranking";
        string url = $"{baseUrl}?name={apiName}&range_start={rangeStart}&range_end={rangeEnd}&region=global&time_range=alltime&around_user=false";

        using (var req = UnityWebRequest.Get(url))
        {
            req.timeout = 30; // Add timeout
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("accesstoken", userAuthKey);

            var operation = req.SendWebRequest();
            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (req.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = req.downloadHandler.text;
                Debug.Log($"[LeaderboardService] GetLeaderboardRecordsAsync Success. Response: {jsonResponse}");

                try
                {
                    LeaderboardRankingResponse response = JsonUtility.FromJson<LeaderboardRankingResponse>(jsonResponse);
                    
                    // Log parsed data for debugging
                    if (response != null && response.ranking != null)
                    {
                        Debug.Log($"[LeaderboardService] Successfully parsed {response.ranking.Length} records");
                        Debug.Log($"[LeaderboardService] API name: {response.apiName}, Total count: {response.totalCount}");
                        
                        // Log server data type for debugging
                        if (response.meta != null)
                        {
                            LeaderboardDataType serverType = ConvertServerDataTypeToEnum(response.meta.data_type);
                            Debug.Log($"[LeaderboardService] Server data_type: {response.meta.data_type} ({serverType})");
                        }
                        
                        // Log first few entries for verification
                        for (int i = 0; i < Math.Min(3, response.ranking.Length); i++)
                        {
                            var record = response.ranking[i];
                            Debug.Log($"[LeaderboardService] Record {i}: Rank={record.rank}, Name={record.name}, Value={record.value}");
                        }
                    }
                    
                    return response;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[LeaderboardService] JSON Parsing Failed: {e.Message}");
                    Debug.LogError($"[LeaderboardService] Raw JSON: {jsonResponse}");
                    return null;
                }
            }
            else
            {
                Debug.LogError($"[LeaderboardService] GetLeaderboardRecordsAsync Failed. Error: {req.error}. Status Code: {req.responseCode}. Response: {req.downloadHandler.text}");
                return null;
            }
        }
    }

    #endregion

    #region Score Submission

    /// <summary>
    /// Submit score to existing Leaderboard Meta (Legacy method - backwards compatibility)
    /// </summary>
    public async Task<bool> SubmitScoreToExistingMetaAsync(string appId, string userAuthKey, string apiName, int scoreValue)
    {
        Debug.Log($"[LeaderboardService] Starting score submission to existing meta: {apiName}, score: {scoreValue}");

        ScoreEntry[] scores = new ScoreEntry[]
        {
            new ScoreEntry { name = apiName, value = scoreValue.ToString() }
        };

        return await SubmitScoreSecureAsync(appId, userAuthKey, scores);
    }

    /// <summary>
    /// Submit score with specific data type formatting and automatic validation
    /// </summary>
    public async Task<bool> SubmitScoreWithDataTypeAsync(string appId, string userAuthKey, string apiName, float scoreValue, LeaderboardDataType dataType)
    {
        Debug.Log($"[LeaderboardService] Starting score submission with data type validation: {dataType}, meta: {apiName}, score: {scoreValue}");

        // First validate data type against server configuration
        var validationResult = await ValidateDataTypeAsync(appId, userAuthKey, apiName, dataType);
        
        if (!validationResult.IsValid)
        {
            Debug.LogError($"[LeaderboardService] Score submission blocked due to data type mismatch: {validationResult.ErrorMessage}");
            return false;
        }

        Debug.Log($"[LeaderboardService] Data type validation passed, proceeding with score submission");

        string formattedScore = FormatScoreForSubmission(scoreValue, dataType);
        
        ScoreEntry[] scores = new ScoreEntry[]
        {
            new ScoreEntry { name = apiName, value = formattedScore }
        };

        Debug.Log($"[LeaderboardService] Formatted score for submission: {formattedScore}");
        
        return await SubmitScoreSecureAsync(appId, userAuthKey, scores);
    }

    /// <summary>
    /// Format score value based on data type for API submission
    /// </summary>
    private string FormatScoreForSubmission(float value, LeaderboardDataType dataType)
    {
        switch (dataType)
        {
            case LeaderboardDataType.Numeric:
                // For numeric scores, use the value as-is
                return value.ToString();
            
            case LeaderboardDataType.Seconds:
                // For seconds, convert to milliseconds for more precision
                return value.ToString();

            case LeaderboardDataType.Milliseconds:
                // For milliseconds, use the value as-is (should already be in milliseconds)
                return value.ToString();

            default:
                return value.ToString();
        }
    }

    #endregion

    [Serializable]
    private class SessionTokenResponse
    {
        public string token;
        public string key;
    }

    public async Task<bool> SubmitScoreSecureAsync(string appId, string userAuthKey, ScoreEntry[] scores)
    {
        Debug.Log($"[LeaderboardService] Starting secure score submission for {appId}...");

        try
        {
           
            using (var rsaProvider = new RSACryptoServiceProvider(2048))
            {
                string publicKeyXml = rsaProvider.ToXmlString(false);

                
                var tokenResponse = await GetSessionTokenAsync(appId, userAuthKey, publicKeyXml);
                if (tokenResponse == null) return false;

               
                byte[] encryptedKeyBytes = Convert.FromBase64String(tokenResponse.key);
                byte[] symmetricKeyBytes = rsaProvider.Decrypt(encryptedKeyBytes, false);
                string symmetricKey = Encoding.UTF8.GetString(symmetricKeyBytes);

            
                string encryptedData = EncryptJsonData(symmetricKey, scores);

              
                bool postSuccess = await PostLeaderboardRecordAsync(
                    appId,
                    userAuthKey,
                    tokenResponse.token,
                    encryptedData
                );

                return postSuccess;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[LeaderboardService] SubmitScoreSecureAsync critical failure: {e.Message}");
            return false;
        }
    }

    private async Task<SessionTokenResponse> GetSessionTokenAsync(string appId, string userAuthKey, string publicKeyXml)
    {
        Debug.Log("[Ironhide] Getting session token...");

        string url = $"{IRONHIDE_API_URL_PREFIX}?app_id={appId}&skip_ua=true";

        using (var req = UnityWebRequest.Get(url))
        {
            req.timeout = 30; // Add timeout
            req.SetRequestHeader("x-htc-public-key", publicKeyXml);
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("accesstoken", userAuthKey);

            var operation = req.SendWebRequest();
            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (req.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = req.downloadHandler.text;
                Debug.Log($"[Ironhide] Token acquisition success. Response: {jsonResponse}");
                return JsonUtility.FromJson<SessionTokenResponse>(jsonResponse);
            }
            else
            {
                Debug.LogError($"[Ironhide] Token acquisition failed. Error: {req.error}. Status Code: {req.responseCode}. Response: {req.downloadHandler.text}");
                return null;
            }
        }
    }

   
    private string EncryptJsonData(string symmetricKey, ScoreEntry[] scores)
    {
        Debug.Log("[Crypto] Encrypting score data...");

        ScoresPayload payload = new ScoresPayload { scores = scores };
        string jsonString = JsonUtility.ToJson(payload);
        byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonString);

        Debug.Log($"[Crypto] JSON to encrypt: {jsonString}");

        byte[] symmetricKeyBytes = Encoding.UTF8.GetBytes(symmetricKey);  
        byte[] keyBytes = Convert.FromBase64String(symmetricKey);         
        byte[] ivBytes = symmetricKeyBytes.Take(16).ToArray();          

        Debug.Log($"[Crypto] Key length: {keyBytes.Length}, IV length: {ivBytes.Length}");

        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = keyBytes;
            aesAlg.IV = ivBytes;
            aesAlg.Mode = CipherMode.CBC;
            aesAlg.Padding = PaddingMode.PKCS7;

            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

            using (var msEncrypt = new System.IO.MemoryStream())
            {
                using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    csEncrypt.Write(jsonBytes, 0, jsonBytes.Length);
                }
                byte[] encryptedBytes = msEncrypt.ToArray();
                string result = Convert.ToBase64String(encryptedBytes);

                Debug.Log($"[Crypto] Encryption complete. Encrypted data length: {result.Length}");
                return result;
            }
        }
    }

    // Score submission request model
    [Serializable]
    private class ScoreSubmissionRequest
    {
        public string scores;
    }

    // Submit encrypted score data to Leaderboard API
    private async Task<bool> PostLeaderboardRecordAsync(string appId, string userAuthKey, string sessionToken, string encryptedData)
    {
        Debug.Log("[Leaderboard] Posting encrypted record...");

        string url = LEADERBOARD_API_URL_PREFIX + appId;
        Debug.Log($"[Leaderboard] Posting to URL: {url}");

        // Use Serializable class instead of anonymous object
        ScoreSubmissionRequest requestData = new ScoreSubmissionRequest
        {
            scores = encryptedData
        };

        string jsonPayload = JsonUtility.ToJson(requestData);
        byte[] jsonToSend = new UTF8Encoding().GetBytes(jsonPayload);

        Debug.Log($"[Leaderboard] Request payload: {jsonPayload}");

        using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            req.timeout = 30; // Add timeout
            req.uploadHandler = new UploadHandlerRaw(jsonToSend);
            req.downloadHandler = new DownloadHandlerBuffer();

            req.SetRequestHeader("Token", sessionToken);
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("accesstoken", userAuthKey);

            var operation = req.SendWebRequest();
            while (!operation.isDone)
            {
                await Task.Yield();
            }

            Debug.Log($"[Leaderboard] Response Code: {req.responseCode}");
            Debug.Log($"[Leaderboard] Response Body: {req.downloadHandler.text}");

            if (req.responseCode == 201)
            {
                Debug.Log("[Leaderboard] Score submission success (HTTP 201).");
                return true;
            }
            else
            {
                Debug.LogError($"[Leaderboard] Score submission failed. Status Code: {req.responseCode}. Response: {req.downloadHandler.text}");
                return false;
            }
        }
    }
}

public enum LeaderboardDataType
{
    Numeric = 1,
    Seconds = 2,
    Milliseconds = 3
}