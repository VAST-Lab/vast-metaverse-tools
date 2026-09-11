using UnityEngine;

namespace VastMetaverseTools.Helper
{
    public class AnimRandomStartOffset : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private string _triggerToSet = "Start";
        [SerializeField] private float _maxDelay = 1f;

        private void OnValidate()
        {
            if (_animator == null) _animator = GetComponent<Animator>();
        }

        private void Start()
        {
            Invoke(nameof(StartAnim), Random.Range(0, _maxDelay));
        }

        private void StartAnim()
        {
            if (_animator != null) _animator.SetTrigger(_triggerToSet);
        }
    }
}