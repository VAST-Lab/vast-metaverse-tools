using TMPro;
using UnityEngine;
using VastMetaverseTools.Runtime.Badges;

namespace VastMetaverseTools.Runtime.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        public event System.Action<Badge> OnBadgeEarned;
        public event System.Action<string> OnToastReceived;

        [SerializeField] private ShopPanel _shop;
        [SerializeField] private GameObject _overridePromptsContainer;
        [SerializeField] private TMP_Text _primaryOverridePromptText;
        [SerializeField] private TMP_Text _secondaryOverridePromptText;

        private void Awake()
        {
            Instance = this;
            if (_overridePromptsContainer != null) _overridePromptsContainer.SetActive(false);
        }

        public void OpenShop()
        {
            _shop.Open();
        }

        public void ShowToast(string message)
        {
            OnToastReceived?.Invoke(message);
        }

        public void ShowBadgeEarned(Badge badge)
        {
            OnBadgeEarned?.Invoke(badge);
        }

        public void SetOverridePrompts(string primary, string secondary)
        {
            bool showPrompts = !string.IsNullOrEmpty(primary) || !string.IsNullOrEmpty(secondary);
            if (_overridePromptsContainer != null) _overridePromptsContainer.SetActive(showPrompts);

            if (_primaryOverridePromptText != null) _primaryOverridePromptText.text = string.IsNullOrEmpty(primary) ? "" : $"[E] {primary}";
            if (_secondaryOverridePromptText != null) _secondaryOverridePromptText.text = string.IsNullOrEmpty(secondary) ? "" : $"[F] {secondary}";
        }
    }
}