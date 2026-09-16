using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace VastMetaverseTools
{
    public class Currency : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _currencyText;

        public static int userCurrency = 0;

        void Update()
        {
            _currencyText.text = "Currency: " + userCurrency.ToString();
        }

        public void AddPoints(int points)
        {
            userCurrency += points;
        }

        public void RemovePoints(int points)
        {
            userCurrency -= points;
        }
    }
}
