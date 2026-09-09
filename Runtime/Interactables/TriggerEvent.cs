using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using VastMetaverseTools.Runtime.Player;

namespace VastMetaverseTools.Runtime.Interactables
{
    public class TriggerEvent : MonoBehaviour
    {
        [SerializeField] private GameObject _showWhenInRange;
        [SerializeField] private GameObject _hideWhenInRange;
        [SerializeField] private bool _showHideScaleAnimation;
        [SerializeField, ShowIf(nameof(_showHideScaleAnimation))] private float _showHideScaleDuration = 0.25f;
        [SerializeField] private UnityEvent _enterEvent;
        [SerializeField] private UnityEvent _exitEvent;

        private void Awake()
        {
            if (_showWhenInRange != null) _showWhenInRange.SetActive(false);
            if (_hideWhenInRange != null) _hideWhenInRange.SetActive(true);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out PlayerController player))
            {
                SetShown(true);
                _enterEvent?.Invoke();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent(out PlayerController player))
            {
                SetShown(false);
                _exitEvent?.Invoke();
            }
        }

        private void SetShown(bool show)
        {
            if (_showHideScaleAnimation)
            {
                if (_showWhenInRange != null) _showWhenInRange.transform
                        .DOScale(show ? 1f : 0f, _showHideScaleDuration)
                        .OnComplete(() => _showWhenInRange.SetActive(show));

                if (_hideWhenInRange != null) _hideWhenInRange.transform
                        .DOScale(show ? 0f : 1f, _showHideScaleDuration)
                        .OnComplete(() => _hideWhenInRange.SetActive(show));
            }
            else
            {
                SetShownImmediate(show);
            }
        }

        private void SetShownImmediate(bool show)
        {
            if (_showWhenInRange != null) _showWhenInRange.SetActive(show);
            if (_hideWhenInRange != null) _hideWhenInRange.SetActive(!show);
        }

        /*
        [SerializeField] private SpatialTriggerEvent _spatial;

        private void OnValidate()
        {
            _spatial = GetComponent<SpatialTriggerEvent>();
            if (_spatial != null)
            {
                _enterEvent = _spatial.onEnterEvent.unityEvent;
                _exitEvent = _spatial.onExitEvent.unityEvent;
            }
        }
        */
    }
}