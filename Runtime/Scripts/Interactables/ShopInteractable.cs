using UnityEngine;
using VastMetaverseTools.UI;

namespace VastMetaverseTools.Interactables
{
    public class ShopInteractable : Interactable
    {
        public override void Interact(GameObject player)
        {
            UIManager.Instance.OpenShop();
        }
    }
}