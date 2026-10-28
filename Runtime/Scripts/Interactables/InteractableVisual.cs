using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VastMetaverseTools.Managers;

namespace VastMetaverseTools.Interactables
{
    public class InteractableVisual : MonoBehaviour
    {
        [SerializeField] private GameObject _dotVisual;
        [SerializeField] private GameObject _expandedVisual;
        [SerializeField] private GameObject _altButtonVisual;
        [SerializeField] private TMP_Text _interactText;
        [SerializeField] private TMP_Text _altButtonText;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Transform _scale;
        [SerializeField] private float _minScale = 0.5f;
        [SerializeField] private float _maxScale = 1f;
        [SerializeField] private float _scaleDistOffset = 0.5f;
        [SerializeField] private float _scaleDistMultiplier = 0.5f;

        private Interactable _target;
        private Transform _cameraTransform;

        private void Awake()
        {
            if (Camera.main != null) _cameraTransform = Camera.main.transform;
            if (_scale == null) _scale = transform;
        }

        private void Update()
        {
            if (_target != null)
            {
                transform.position = _target.transform.position + Vector3.up * _target.VerticalOffset;
                if (_cameraTransform != null)
                {
                    transform.rotation = Quaternion.LookRotation(transform.position - _cameraTransform.position);
                    var dist = Vector3.Distance(transform.position, _cameraTransform.position);
                    transform.localScale = Vector3.one * Mathf.Lerp(_minScale, _maxScale, Mathf.Clamp01(dist * _scaleDistMultiplier - _scaleDistOffset)) * _target.VisualPopupScale;
                }
            }
        }

        public void SetTarget(Interactable target)
        {
            _target = target;
        }

        public void SetState(bool isClosest)
        {
            _dotVisual.SetActive(!isClosest);
            _expandedVisual.SetActive(isClosest && _target.CanKeyInteract);
            _altButtonVisual.SetActive(isClosest && !_target.CanKeyInteract);

            if (isClosest && _target != null)
            {
                _interactText.text = _target.InteractText;
                _altButtonText.text = _target.InteractText;
            }
        }

        public void ClickInteractable()
        {
            // TODO: Fix this
            _target.Interact(MetaverseManager.LocalPlayer.gameObject);
        }
    }
}