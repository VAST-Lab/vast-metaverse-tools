using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VastMetaverseTools.Interactables
{
    public class InteractableVisual : MonoBehaviour
    {
        [SerializeField] private GameObject _dotVisual;
        [SerializeField] private GameObject _expandedVisual;
        [SerializeField] private TMP_Text _interactText;
        [SerializeField] private Image _iconImage;

        private Interactable _target;
        private Transform _cameraTransform;

        private void Awake()
        {
            if (Camera.main != null) _cameraTransform = Camera.main.transform;
        }

        private void Update()
        {
            if (_target != null)
            {
                transform.position = _target.transform.position + Vector3.up * _target.VerticalOffset;
                if (_cameraTransform != null)
                {
                    transform.rotation = Quaternion.LookRotation(transform.position - _cameraTransform.position);
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
            _expandedVisual.SetActive(isClosest);

            if (isClosest && _target != null)
            {
                _interactText.text = _target.InteractText;
            }
        }
    }
}