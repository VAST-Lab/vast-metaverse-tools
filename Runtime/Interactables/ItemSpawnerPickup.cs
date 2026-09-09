using UnityEngine;
using VastMetaverseTools.Runtime.Player;

namespace VastMetaverseTools.Runtime.Interactables
{
    public class ItemSpawnerPickup : Interactable
    {
        [SerializeField] private GameObject _visual;
        [SerializeField] private GameObject _itemPrefab;
        [SerializeField] private Transform _spawnAt;
        [SerializeField] private Transform _teleportPlayerTo;

        public override void Interact(GameObject player)
        {
            base.Interact(player);
            if (_spawnAt == null) _spawnAt = transform;
            var prefab = Instantiate(_itemPrefab, _spawnAt.position, _spawnAt.rotation);
            if (prefab.TryGetComponent(out Interactable interactable) && player.TryGetComponent(out PlayerInteractor interactor))
            {
                interactable.Interact(player);
            }
            if (_teleportPlayerTo != null && player.TryGetComponent(out PlayerController controller))
            {
                controller.TeleportTo(_teleportPlayerTo);
            }
            if (_visual != null) _visual.SetActive(false);
        }

        protected override void Unlock()
        {
            base.Unlock();
            if (_visual != null) _visual.SetActive(true);
        }
    }
}