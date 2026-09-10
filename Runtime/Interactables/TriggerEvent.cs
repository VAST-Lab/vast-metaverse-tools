using System.Collections;
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
        [SerializeField] private float _showHideScaleDuration = 0.25f;
        [SerializeField] private UnityEvent _enterEvent;
        [SerializeField] private UnityEvent _exitEvent;

        private Coroutine _showAnimationRoutine;
        private Coroutine _hideAnimationRoutine;

        private void Awake()
        {
            if (_showWhenInRange != null)
            {
                _showWhenInRange.transform.localScale = Vector3.zero;
                _showWhenInRange.SetActive(false);
            }

            if (_hideWhenInRange != null)
            {
                _hideWhenInRange.transform.localScale = Vector3.one;
                _hideWhenInRange.SetActive(true);
            }
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
                if (_showWhenInRange != null)
                {
                    if (_showAnimationRoutine != null) StopCoroutine(_showAnimationRoutine);
                    _showAnimationRoutine = StartCoroutine(AnimateScale(_showWhenInRange, show ? Vector3.one : Vector3.zero, show));
                }

                if (_hideWhenInRange != null)
                {
                    if (_hideAnimationRoutine != null) StopCoroutine(_hideAnimationRoutine);
                    _hideAnimationRoutine = StartCoroutine(AnimateScale(_hideWhenInRange, show ? Vector3.zero : Vector3.one, !show));
                }
            }
            else
            {
                SetShownImmediate(show);
            }
        }

        private void SetShownImmediate(bool show)
        {
            if (_showWhenInRange != null)
            {
                _showWhenInRange.transform.localScale = show ? Vector3.one : Vector3.zero;
                _showWhenInRange.SetActive(show);
            }

            if (_hideWhenInRange != null)
            {
                _hideWhenInRange.transform.localScale = show ? Vector3.zero : Vector3.one;
                _hideWhenInRange.SetActive(!show);
            }
        }

        private IEnumerator AnimateScale(GameObject target, Vector3 targetScale, bool endActiveState)
        {
            target.SetActive(true);

            Vector3 startScale = target.transform.localScale;
            float elapsed = 0f;

            while (elapsed < _showHideScaleDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _showHideScaleDuration);
                target.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                yield return null;
            }

            target.transform.localScale = targetScale;
            target.SetActive(endActiveState);
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