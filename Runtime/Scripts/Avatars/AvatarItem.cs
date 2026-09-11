using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VastMetaverseTools.Avatars
{
    [RequireComponent(typeof(Button))]
    public class AvatarItem : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _nameText;

        private Button _button;
        private Action _onClick;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (_iconImage == null) _iconImage = GetComponent<Image>();
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(OnClick);
        }

        public void Setup(AvatarReference avatar, Action onClick)
        {
            _onClick = onClick;
            if (_iconImage != null) _iconImage.sprite = avatar.Icon;
            if (_nameText != null) _nameText.text = avatar.Name;
        }

        private void OnClick()
        {
            _onClick?.Invoke();
        }
    }
}