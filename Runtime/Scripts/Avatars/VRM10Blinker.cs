using System.Collections;
using UnityEngine;

namespace VastMetaverseTools.Avatars
{
    public class VRM10Blinker : MonoBehaviour
    {
        [SerializeField] private float _interval = 5.0f;
        [SerializeField] private float _closingTime = 0.06f;
        [SerializeField] private float _openingSeconds = 0.03f;
        [SerializeField] private float _closeSeconds = 0.1f;

        private Coroutine _coroutine;
        private float _nextRequest;
        private bool _request;
        private float _blinkValue;

        public bool Request
        {
            get { return _request; }
            set
            {
                if (Time.time < _nextRequest) return;
                _request = value;
                _nextRequest = Time.time + 1.0f;
            }
        }

        public float BlinkValue => _blinkValue;

        private IEnumerator BlinkRoutine()
        {
            while (true)
            {
                float waitTime = Time.time + Random.value * _interval;

                while (waitTime > Time.time)
                {
                    if (_request)
                    {
                        _request = false;
                        break;
                    }
                    yield return null;
                }

                float value = 0.0f;
                float closeSpeed = 1.0f / _closeSeconds;

                while (true)
                {
                    value += Time.deltaTime * closeSpeed;
                    if (value >= 1.0f) break;

                    _blinkValue = value;
                    yield return null;
                }

                _blinkValue = 1.0f;
                yield return new WaitForSeconds(_closingTime);

                value = 1.0f;
                float openSpeed = 1.0f / _openingSeconds;

                while (true)
                {
                    value -= Time.deltaTime * openSpeed;
                    if (value < 0) break;

                    _blinkValue = value;
                    yield return null;
                }

                _blinkValue = 0f;
            }
        }

        private void OnEnable()
        {
            _coroutine = StartCoroutine(BlinkRoutine());
        }

        private void OnDisable()
        {
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
                _coroutine = null;
            }
        }
    }
}
