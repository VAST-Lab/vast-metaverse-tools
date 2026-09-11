using UnityEngine;

namespace VastMetaverseTools.Player
{
    public class PlayerCurrency : MonoBehaviour
    {
        private int _currency;

        public void AddCurrency(int amount)
        {
            _currency += amount;
        }

        public bool SpendCurrency(int amount)
        {
            if (_currency < amount) return false;
            _currency -= amount;
            return true;
        }
    }
}