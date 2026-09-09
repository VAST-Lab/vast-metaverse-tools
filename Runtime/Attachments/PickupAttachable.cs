using UnityEngine;
using VastMetaverseTools.Runtime.Interactables;

namespace VastMetaverseTools.Runtime.Attachments
{
    public class PickupAttachable : Interactable
    {
        [SerializeField] private PlayerAttachment _attachment;
        private bool _locationOccupied;

        public override bool CanInteract => base.CanInteract && !_locationOccupied;

        private void Start()
        {
            PlayerAttachmentManager.OnEquipmentAdded += HandleEquipmentAdded;
            PlayerAttachmentManager.OnEquipmentRemoved += HandleEquipmentRemoved;
            if (PlayerAttachmentManager.Exists) _locationOccupied = PlayerAttachmentManager.Instance.PlayerHasEquipment(_attachment.Location);
        }

        private void OnDestroy()
        {
            PlayerAttachmentManager.OnEquipmentAdded -= HandleEquipmentAdded;
            PlayerAttachmentManager.OnEquipmentRemoved -= HandleEquipmentRemoved;
        }

        private void HandleEquipmentAdded(AttachmentLocation location)
        {
            if (_attachment != null && location == _attachment.Location) _locationOccupied = true;
        }

        private void HandleEquipmentRemoved(AttachmentLocation location)
        {
            if (_attachment != null && location == _attachment.Location) _locationOccupied = false;
        }

        public override void Interact(GameObject player)
        {
            base.Interact(player);
            if (PlayerAttachmentManager.Instance.Equip(_attachment))
                Destroy(gameObject);
        }
    }
}