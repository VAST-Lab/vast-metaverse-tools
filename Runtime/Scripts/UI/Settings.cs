using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VastMetaverseTools.UI;


namespace VastMetaverseTools
{
    public class Settings : MonoBehaviour
    {
        [SerializeField] private Button _toggleButton;
        [SerializeField] public CanvasGroup _settingsPanelGroup;

        private OptionsManager _optionsManager;

        private bool _shown;

        private void Awake()
        {
            if (_toggleButton != null)
            {
                _toggleButton.onClick.RemoveAllListeners();
                _toggleButton.onClick.AddListener(ToggleSettingsPanel);
            }
            _settingsPanelGroup.gameObject.SetActive(true);

            _optionsManager = GetComponent<OptionsManager>();
        }

        private void Start()
        {
            SetSettingsPanelShown(false);
        }

        private void ToggleSettingsPanel()
        {
            _optionsManager.ChangePanel(_settingsPanelGroup);
            //SetSettingsPanelShown(!_shown);
        }

        private void SetSettingsPanelShown(bool shown)
        {
            _shown = shown;
            UIHelper.SetCanvasGroupActive(_settingsPanelGroup, shown);
        }
    }
}