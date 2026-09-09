using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VastMetaverseTools.Runtime.UI;

namespace VastMetaverseTools.Runtime.Avatars
{
    public class CharacterCustomizer : MonoBehaviour
    {
        [SerializeField] private Button _toggleButton;
        [SerializeField] private CanvasGroup _avatarGalleryGroup;
        [SerializeField] private RectTransform _avatarIconParent;
        [SerializeField] private AvatarItem _avatarIconPrefab;
        [SerializeField] private List<AvatarReference> _avatars;

        public AvatarReference GetAvatar(int index) => _avatars[index];
        private bool _shown;

        private void Awake()
        {
            if (_toggleButton != null)
            {
                _toggleButton.onClick.RemoveAllListeners();
                _toggleButton.onClick.AddListener(ToggleAvatarGallery);
            }
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
            var avatarLoader = FindFirstObjectByType<AvatarLoader>();
            if (avatarLoader != null) avatarLoader.LoadAvatarReference(_avatars[index]);
        }

        private void ToggleAvatarGallery() => SetAvatarGalleryShown(!_shown);
        private void SetAvatarGalleryShown(bool shown)
        {
            _shown = shown;
            UIHelper.SetCanvasGroupActive(_avatarGalleryGroup, shown);
        }
    }
}