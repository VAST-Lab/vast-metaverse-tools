using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VastMetaverseTools.Managers;
using VastMetaverseTools.UI;

namespace VastMetaverseTools.Avatars
{
    public class CharacterCustomizer : MonoBehaviour
    {
        [SerializeField] private Button _toggleButton;
        [SerializeField] private CanvasGroup _avatarGalleryGroup;
        [SerializeField] private RectTransform _avatarIconParent;
        [SerializeField] private AvatarItem _avatarIconPrefab;
        [SerializeField] private List<AvatarReference> _avatars;

        public CanvasGroup AvatarGalleryGroup => _avatarGalleryGroup;
        public AvatarReference GetAvatar(int index) => _avatars[index];

        public int AvatarCount => _avatars != null ? _avatars.Count : 0;
        public static int SelectedAvatarIndex { get; private set; } = -1;
        public static event Action<int> OnLocalAvatarSelected;

        private OptionsManager _optionsManager;
        private bool _shown;

        private void Awake()
        {
            if (_toggleButton != null)
            {
                _toggleButton.onClick.RemoveAllListeners();
                _toggleButton.onClick.AddListener(ToggleAvatarGallery);
            }
            _avatarGalleryGroup.gameObject.SetActive(true);

            _optionsManager = GetComponent<OptionsManager>();
        }

        private void Start()
        {
            SetAvatarGalleryShown(false);
            FillAvatarIcons();
        }

        private void FillAvatarIcons()
        {
            for (int i = 0; i < _avatars.Count; i++)
            {
                int index = i;
                var image = Instantiate(_avatarIconPrefab, _avatarIconParent);
                image.Setup(_avatars[i], () => SelectAvatar(index));
            }
        }

        public void SelectAvatar(int index)
        {
            SelectedAvatarIndex = index;
            if (MetaverseManager.LocalPlayer != null)
            {
                var loader = MetaverseManager.LocalPlayer.GetComponentInChildren<AvatarLoader>();
                if (loader != null) loader.LoadAvatarReference(_avatars[index]);
            }
            OnLocalAvatarSelected?.Invoke(index);
        }

        private void ToggleAvatarGallery()
        {
            _optionsManager.ChangePanel(_avatarGalleryGroup);
            //SetAvatarGalleryShown(!_shown);
        }
        private void SetAvatarGalleryShown(bool shown)
        {
            _shown = shown;
            UIHelper.SetCanvasGroupActive(_avatarGalleryGroup, shown);
        }
    }
}