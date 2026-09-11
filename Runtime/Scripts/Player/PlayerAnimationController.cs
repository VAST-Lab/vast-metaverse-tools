using UnityEngine;

namespace VastMetaverseTools.Player
{
    public class PlayerAnimationController : MonoBehaviour
    {
        private Animator _animator;
        private static readonly int ThrowTypeHash = Animator.StringToHash("ThrowType");
        private static readonly int PrepareThrowHash = Animator.StringToHash("ThrowPrepare");
        private static readonly int ThrowHash = Animator.StringToHash("Throw");

        public void SetAnimator(Animator animator)
        {
            _animator = animator;
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

        public void SetPrepareThrow(int type, bool preparing)
        {
            if (_animator == null) return;
            _animator.SetFloat(ThrowTypeHash, type); // Float for blend tree
            _animator.SetBool(PrepareThrowHash, preparing);
        }

        public void TriggerThrow()
        {
            if (_animator != null) _animator.SetTrigger(ThrowHash);
        }
    }
}