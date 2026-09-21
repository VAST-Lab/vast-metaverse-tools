using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using VastMetaverseTools.Avatars;
using VastMetaverseTools.UI;

namespace VastMetaverseTools
{
    public class OptionsManager : MonoBehaviour
    {
        private CharacterCustomizer _characterCustomizer;
        private CanvasGroup _avatarPanel;

        private Settings _settings;
        private CanvasGroup _settingsPanel;

        private List<CanvasGroup> _canvasGroups = new List<CanvasGroup>();

        private void Awake()
        {
            _characterCustomizer = GetComponent<CharacterCustomizer>();
            _avatarPanel = _characterCustomizer._avatarGalleryGroup;
            _canvasGroups.Add(_avatarPanel);

            _settings = GetComponent<Settings>();
            _settingsPanel = _settings._settingsPanelGroup;
            _canvasGroups.Add(_settingsPanel);
        }

        private void Start()
        {
            Debug.Log(_canvasGroups.Count);
        }

        public void ChangePanel(CanvasGroup shownPanel)
        {
            if (shownPanel.interactable)
            {
                UIHelper.SetCanvasGroupActive(shownPanel, false);
                return;
            }

            foreach (CanvasGroup canvasGroup in _canvasGroups)
            {
                UIHelper.SetCanvasGroupActive(canvasGroup, false);
            }

            UIHelper.SetCanvasGroupActive(shownPanel, true);
        }
    }
}
