using UnityEngine;

namespace VastMetaverseTools.Runtime.UI
{
    public static class UIHelper
    {
        public static void SetCanvasGroupActive(CanvasGroup group, bool active)
        {
            if (group == null) return;
            group.alpha = active ? 1 : 0;
            group.interactable = active;
            group.blocksRaycasts = active;
        }

        public static void SetInteractable(CanvasGroup group, bool interactable)
        {
            if (group == null) return;
            group.alpha = interactable ? 1f : 0.5f;
            group.interactable = interactable;
            group.blocksRaycasts = true;
        }
    }
}