using System.Collections;
using UnityEngine;

namespace VastMetaverseTools.Avatars
{
    public class VRM10AutoExpression : MonoBehaviour
    {
        [SerializeField] private float _wait = 0.5f;
        private Coroutine _coroutine;

        private float _happy;
        private float _angry;
        private float _sad;
        private float _relaxed;
        private float _surprised;

        public float Happy => _happy;
        public float Angry => _angry;
        public float Sad => _sad;
        public float Relaxed => _relaxed;
        public float Surprised => _surprised;

        private void SetWeight(int index, float value)
        {
            switch (index)
            {
                case 0: _happy = value; break;
                case 1: _angry = value; break;
                case 2: _sad = value; break;
                case 3: _relaxed = value; break;
                case 4: _surprised = value; break;
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
                float velocity = 0.01f;
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
