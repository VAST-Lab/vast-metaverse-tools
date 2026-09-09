using System.Collections;
//using CoffeyUtils;
using UnityEngine;
using VastMetaverseTools.Runtime.Attachments;
using VastMetaverseTools.Runtime.Interactables;

namespace VastMetaverseTools.Runtime.Player
{
    public class EatManager : Interactable
    {
        //[SerializeField] private SfxReference _sfx;

        public override bool CanInteract => base.CanInteract && PlayerAttachmentManager.HeldItem != null && PlayerAttachmentManager.HeldItem.CanEat;

        private IEnumerator Start()
        {
            yield return new WaitUntil(() => PlayerAttachmentManager.Exists);
            PlayerAttachmentManager.AddFollower(AttachmentLocation.RightHand, transform);
        }

        public override void Interact(GameObject player)
        {
            if (PlayerAttachmentManager.Instance.EatHeldItem())
            {
                //_sfx.Play();
            }
        }
    }
}