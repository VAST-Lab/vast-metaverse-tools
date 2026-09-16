using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace VastMetaverseTools
{
    public class Currency : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _currencyText;

        private int currency = 0;

        void Update()
        {
            _currencyText.text = "Currency: " + currency.ToString();
        }

        public void AddPoints(int points)
        {
            currency += points;
        }

        public void RemovePoints(int points)
        {
            currency -= points;
        }
    }
}
