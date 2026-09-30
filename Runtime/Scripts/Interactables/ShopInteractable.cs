using UnityEngine;
using VastMetaverseTools.UI;

namespace VastMetaverseTools.Interactables
{
    public class ShopInteractable : Interactable
    {
        private void Start()
        {
            if (_interactText == "Interact") _interactText = "Open Shop";
        }

        public override void Interact(GameObject player)
        {
            UIManager.Instance.OpenShop();
        }
    }
}