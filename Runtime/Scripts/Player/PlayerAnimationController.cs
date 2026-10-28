using UnityEngine;

namespace VastMetaverseTools.Player
{
    public class PlayerAnimationController : MonoBehaviour
    {
        [SerializeField] private Animator _animator;

        private RuntimeAnimatorController _animController;

        public void SetAnimationController(RuntimeAnimatorController controller)
        {
            if (controller == null) return;
            _animController = controller;
            if (_animator != null) _animator.runtimeAnimatorController = _animController;
        }

        public void SetAnimator(Animator animator)
        {
            _animator = animator;
            if (_animator != null) _animator.runtimeAnimatorController = _animController;
        }

        public void SetMoveSpeed(float moveSpeed)
        {
            if (_animator != null) _animator.SetFloat("MoveSpeed", moveSpeed);
        }

        public void SetSeated(bool seated)
        {
            if (_animator != null) _animator.SetBool("Seated", seated);
        }
        public void SetGrounded(bool grounded)
        {
            // For jump/landed logic
        }

        public void Jump()
        {
            if (_animator != null) _animator.SetTrigger("Jump");
        }

        public void SetBool(string param, bool value)
        {
            if (_animator != null) _animator.SetBool(param, value);
        }

        public void SetFloat(string param, float value)
        {
            if (_animator != null) _animator.SetFloat(param, value);
        }

        public void SetTrigger(string param)
        {
            if (_animator != null) _animator.SetTrigger(param);
        }
    }
}