using UnityEngine;
using VastMetaverseTools.Interactables;

namespace VastMetaverseTools.Player
{
    public class Seat : Interactable, IOverrideInteractor
    {
        [SerializeField] private float _seatedPlayerOffset;

        public string PrimaryInteractText => "Stand up";
        public string SecondaryInteractText => null;

        public override void Interact(GameObject player)
        {
            base.Interact(player);
            if (player.TryGetComponent(out PlayerController controller) && player.TryGetComponent(out PlayerInteractor interactor))
            {
                controller.LockEverything(true);
                controller.TeleportTo(transform.position + Vector3.up * _seatedPlayerOffset, transform.rotation);
                interactor.SetOverrideInteractor(this, true);
                if (player.TryGetComponent(out PlayerAnimationController animator)) animator.SetSeated(true);
            }
        }

        public bool OnPrimaryInteract(GameObject player)
        {
            if (player.TryGetComponent(out PlayerController controller) && player.TryGetComponent(out PlayerInteractor interactor))
            {
                controller.LockEverything(false);
                controller.TeleportTo(transform.position + Vector3.up * 0.1f, transform.rotation);
                interactor.SetOverrideInteractor(this, false);
                if (player.TryGetComponent(out PlayerAnimationController animator)) animator.SetSeated(false);
            }
            return true;
        }

        public bool OnSecondaryInteract(GameObject player)
        {
            return false;
        }

#if UNITY_EDITOR
        protected override void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.2f);

            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * VerticalOffset, 0.1f);
        }
        protected override void OnDrawGizmosSelected()
        {
        }
#endif
    }
}