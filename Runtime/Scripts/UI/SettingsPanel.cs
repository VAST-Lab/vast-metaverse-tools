using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VastMetaverseTools.UI;


namespace VastMetaverseTools
{
    public class SettingsPanel : MonoBehaviour
    {
        [SerializeField] private Button _toggleButton;
        [SerializeField] private CanvasGroup _settingsPanelGroup;

        private bool _shown;

        private void Awake()
        {
            if (_toggleButton != null)
            {
                _toggleButton.onClick.RemoveAllListeners();
                _toggleButton.onClick.AddListener(ToggleSettingsPanel);
            }
            _settingsPanelGroup.gameObject.SetActive(true);
        }

        private void Start()
        {
            SetSettingsPanelShown(false);
        }

        private void ToggleSettingsPanel() => SetSettingsPanelShown(!_shown);

        private void SetSettingsPanelShown(bool shown)
        {
            _shown = shown;
            UIHelper.SetCanvasGroupActive(_settingsPanelGroup, shown);
        }
    }
}