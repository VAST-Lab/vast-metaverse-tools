using System.Collections;
using UnityEngine;

namespace VastMetaverseTools.Avatars
{
    public class VRM10AIUEO : MonoBehaviour
    {
        [SerializeField] private float _wait = 0.5f;
        private Coroutine _coroutine;

        private float _aa;
        private float _ih;
        private float _ou;
        private float _ee;
        private float _oh;

        public float Aa => _aa;
        public float Ih => _ih;
        public float Ou => _ou;
        public float Ee => _ee;
        public float Oh => _oh;

        private void SetWeight(int index, float value)
        {
            switch (index)
            {
                case 0: _aa = value; break;
                case 1: _ih = value; break;
                case 2: _ou = value; break;
                case 3: _ee = value; break;
                case 4: _oh = value; break;
            }
        }

        private IEnumerator RoutineNest(int index, float velocity, float waitTime)
        {
            for (float value = 0.0f; value <= 1.0f; value += velocity)
            {
                SetWeight(index, value);
                yield return null;
            }

            SetWeight(index, 1.0f);
            yield return new WaitForSeconds(waitTime);

            for (float value = 1.0f; value >= 0; value -= velocity)
            {
                SetWeight(index, value);
                yield return null;
            }

            SetWeight(index, 0);
            yield return new WaitForSeconds(waitTime * 2);
        }

        private IEnumerator Routine()
        {
            while (true)
            {
                yield return new WaitForSeconds(1.0f);
                float velocity = 0.1f;
                yield return RoutineNest(0, velocity, _wait);
                yield return RoutineNest(1, velocity, _wait);
                yield return RoutineNest(2, velocity, _wait);
                yield return RoutineNest(3, velocity, _wait);
                yield return RoutineNest(4, velocity, _wait);
            }
        }

        private void OnEnable()
        {
            _coroutine = StartCoroutine(Routine());
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
