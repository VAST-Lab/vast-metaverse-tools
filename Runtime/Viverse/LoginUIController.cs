using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ViverseSDK.Login;   // Import the namespace that exposes LoginManager

/// <summary>
/// Handles the UI layer for VIVERSE login.
/// It listens to LoginManager events and updates buttons/labels accordingly.
/// </summary>
[RequireComponent(typeof(LoginManager))]
public class LoginUIController : MonoBehaviour
{
    [Header("App Settings")]
    [SerializeField]
    private string appId;         // Your VIVERSE App ID (assign in Inspector)

    [Header("UI References")]
    [SerializeField] private Button loginButton;
    [SerializeField] private Button clearDataButton;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text accountText;
    [SerializeField] private TMP_Text tokenText;

    private LoginManager loginManager;

    private void Awake()
    {
        // Grab the LoginManager component and wire up button handlers
        loginManager = GetComponent<LoginManager>();

        loginButton.onClick.AddListener(() => loginManager.StartLogin());
        clearDataButton.onClick.AddListener(() =>
        {
            loginManager.ClearLoginData();
            UpdateStatus("Tokens cleared");
        });

        // Listen to LoginManager events so the UI stays in sync with auth state
        loginManager.OnStatusUpdated += UpdateStatus;
        loginManager.OnTokenUpdated += UpdateToken;
        loginManager.OnUserInfoUpdated += UpdateAccount;
        loginManager.OnLoginStateChanged += ToggleLoginButton;
    }

    private void Start()
    {
        // Show a default status immediately
        UpdateStatus("Ready");

        // Initialize login flow using the App ID specified in Inspector
        loginManager.Initialize(appId);
    }

    private void OnDestroy()
    {
        // Always unsubscribe from events to avoid leaks / stale references
        loginManager.OnStatusUpdated -= UpdateStatus;
        loginManager.OnTokenUpdated -= UpdateToken;
        loginManager.OnUserInfoUpdated -= UpdateAccount;
        loginManager.OnLoginStateChanged -= ToggleLoginButton;
    }

    /// <summary>Updates the status label and logs to console.</summary>
    private void UpdateStatus(string message)
    {
        if (statusText) statusText.text = message;
        Debug.Log("[LoginUI] " + message);
    }

    /// <summary>Shows a truncated access token, to avoid dumping the entire value.</summary>
    private void UpdateToken(string token)
    {
        if (!tokenText) return;

        tokenText.text = string.IsNullOrEmpty(token)
            ? "Token: <empty>"
            : $"Token: {token.Substring(0, Mathf.Min(20, token.Length))}...";
    }


    /// <summary>Displays the account ID returned from the login result.</summary>
    private void UpdateAccount(string accountId)
    {
        if (!accountText) return;

        accountText.text = string.IsNullOrEmpty(accountId)
            ? "Account: <none>"
            : $"Account: {accountId}";
    }

    /// <summary>Disables the login button once a session is active.</summary>
    private void ToggleLoginButton(bool isLoggedIn)
    {
        if (loginButton) loginButton.interactable = !isLoggedIn;
    }
}