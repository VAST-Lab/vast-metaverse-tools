using UnityEngine;

namespace VastMetaverseTools.Player
{
    public class PlayerAnimationController : MonoBehaviour
    {
        [SerializeField] private Animator _animator;

        public Animator AnimatorComponent => _animator;
        public float CurrentMoveSpeed { get; private set; }
        public bool IsSeated { get; private set; }
        public event System.Action OnJumpTriggered;

        private RuntimeAnimatorController _animController;

        private static readonly int JumpHash = Animator.StringToHash("Jump");
        private static readonly int SeatedHash = Animator.StringToHash("Seated");
        private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");

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

        // Movement Functions
        public void SetMoveSpeed(float moveSpeed)
        {
            if (_animator != null) SetFloat(MoveSpeedHash, moveSpeed);
            CurrentMoveSpeed = moveSpeed;
        }
        public void SetSeated(bool seated)
        {
            if (_animator != null) SetBool(SeatedHash, seated);
            IsSeated = seated;
        }
        public void Jump()
        {
            if (_animator != null) SetTrigger(JumpHash);
            OnJumpTriggered?.Invoke();
        }
        public void SetGrounded(bool grounded)
        {
            // For jump/landed logic
        }

        // Generic Functions
        public void SetBool(int hash, bool value) { if (_animator != null) _animator.SetBool(hash, value); }
        public void SetFloat(int hash, float value) { if (_animator != null) _animator.SetFloat(hash, value); }
        public void SetTrigger(int hash) { if (_animator != null) _animator.SetTrigger(hash); }

        // Generic Functions for simplicity, dont use these if using often
        public void SetBool(string param, bool value) => SetBool(Animator.StringToHash(param), value);
        public void SetFloat(string param, float value) => SetFloat(Animator.StringToHash(param), value);
        public void SetTrigger(string param) => SetTrigger(Animator.StringToHash(param));
    }
}