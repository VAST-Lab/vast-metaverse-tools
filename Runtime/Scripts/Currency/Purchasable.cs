using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VastMetaverseTools
{
    public class Purchasable : MonoBehaviour
    {
        [SerializeField] private int price;

        private int currency;

        void Start()
        {
            currency = Currency.userCurrency;
        }

        private void AttemptPurchase()
        {
            if (price <= currency)
            {
                _currency.RemovePoints(price);
                Debug.Log("bought");
            }
            else
            {
                Debug.Log("Failed To Buy");
            }
        }
    }
}
