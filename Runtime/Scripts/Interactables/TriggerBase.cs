using UnityEngine;
using VastMetaverseTools.Player;

namespace VastMetaverseTools.Interactables
{
    public abstract class TriggerBase : MonoBehaviour
    {
        // TODO: Handle multiple players (Dont update until all players left trigger, etc.)
        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out PlayerController player))
            {
                UpdateTrigger(true, player);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent(out PlayerController player))
            {
                UpdateTrigger(false, player);
            }
        }

        protected abstract void UpdateTrigger(bool insideTrigger, PlayerController player);
    }
}
