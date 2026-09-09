using UnityEngine;
using VastMetaverseTools.Runtime.UI;

namespace VastMetaverseTools.Runtime.Interactables
{
    public class ShopInteractable : Interactable
    {
        public override void Interact(GameObject player)
        {
            UIManager.Instance.OpenShop();
        }
    }
}