using UnityEngine;
using UnityEngine.UI;
using VastMetaverseTools.Managers;
using VastMetaverseTools.Player;
using VastMetaverseTools.UI;


namespace VastMetaverseTools
{
    public class Settings : MonoBehaviour
    {
        private const string VolumePrefKey = "MasterVolume";
        private const float MinSensitivityMultiplier = 0.25f;
        private const float MaxSensitivityMultiplier = 3f;

        [SerializeField] private Button _toggleButton;
        [SerializeField] private CanvasGroup _settingsPanelGroup;

        [Header("Controls (found by name under this object if left empty)")]
        [SerializeField] private Slider _volumeSlider;
        [SerializeField] private Slider _sensitivitySlider;
        [SerializeField] private Toggle _cameraLockToggle;
        [SerializeField] private Toggle _fullscreenToggle;

        public CanvasGroup SettingsPanelGroup => _settingsPanelGroup;

        private OptionsManager _optionsManager;

        private bool _shown;
        private bool _lastFullScreen;

        private void Awake()
        {
            if (_toggleButton != null)
            {
                _toggleButton.onClick.RemoveAllListeners();
                _toggleButton.onClick.AddListener(ToggleSettingsPanel);
            }
            _settingsPanelGroup.gameObject.SetActive(true);

            _optionsManager = GetComponent<OptionsManager>();

            _volumeSlider = FindControl(_volumeSlider, "VolumeSlider");
            _sensitivitySlider = FindControl(_sensitivitySlider, "SensitivitySlider");
            _cameraLockToggle = FindControl(_cameraLockToggle, "CameraLockToggle");
            _fullscreenToggle = FindControl(_fullscreenToggle, "FullscreenToggle");
        }

        private void Start()
        {
            SetSettingsPanelShown(false);
            SetUpControls();
        }

        private void Update()
        {
            // Fullscreen can also be left with Esc, so keep the checkbox in step with the window.
            if (_fullscreenToggle == null || Screen.fullScreen == _lastFullScreen) return;
            _lastFullScreen = Screen.fullScreen;
            _fullscreenToggle.SetIsOnWithoutNotify(_lastFullScreen);
        }

        private void SetUpControls()
        {
            float volume = PlayerPrefs.GetFloat(VolumePrefKey, 1f);
            AudioListener.volume = volume;
            if (_volumeSlider != null)
            {
                _volumeSlider.minValue = 0f;
                _volumeSlider.maxValue = 1f;
                _volumeSlider.SetValueWithoutNotify(volume);
                _volumeSlider.onValueChanged.AddListener(SetVolume);
            }

            if (_sensitivitySlider != null)
            {
                _sensitivitySlider.minValue = MinSensitivityMultiplier;
                _sensitivitySlider.maxValue = MaxSensitivityMultiplier;
                _sensitivitySlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(PlayerController.SensitivityPrefKey, 1f));
                _sensitivitySlider.onValueChanged.AddListener(SetSensitivity);
            }

            if (_cameraLockToggle != null)
            {
                _cameraLockToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt(PlayerController.CameraLockPrefKey, 0) == 1);
                _cameraLockToggle.onValueChanged.AddListener(SetCameraLock);
            }

            if (_fullscreenToggle != null)
            {
                _lastFullScreen = Screen.fullScreen;
                _fullscreenToggle.SetIsOnWithoutNotify(_lastFullScreen);
                _fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
            }
        }

        private void SetVolume(float volume)
        {
            AudioListener.volume = volume;
            PlayerPrefs.SetFloat(VolumePrefKey, volume);
            PlayerPrefs.Save();
        }

        private void SetSensitivity(float multiplier)
        {
            PlayerPrefs.SetFloat(PlayerController.SensitivityPrefKey, multiplier);
            PlayerPrefs.Save();
            if (MetaverseManager.LocalPlayer != null) MetaverseManager.LocalPlayer.SetSensitivityMultiplier(multiplier);
        }

        private void SetCameraLock(bool locked)
        {
            PlayerPrefs.SetInt(PlayerController.CameraLockPrefKey, locked ? 1 : 0);
            PlayerPrefs.Save();
            if (MetaverseManager.LocalPlayer != null) MetaverseManager.LocalPlayer.SetCameraLock(locked);
        }

        // Browsers only allow fullscreen from a click, which this is; the editor's Game view ignores it.
        private void SetFullscreen(bool fullscreen)
        {
            Screen.fullScreen = fullscreen;
        }

        private T FindControl<T>(T current, string objectName) where T : Component
        {
            if (current != null) return current;
            foreach (T candidate in GetComponentsInChildren<T>(true))
            {
                if (candidate.name == objectName) return candidate;
            }
            return null;
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
