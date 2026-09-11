using UnityEngine;

namespace VastMetaverseTools.Helper
{
    public class SimpleRotator : MonoBehaviour
    {
        [SerializeField] private Vector3 _rotationAxis = Vector3.up;
        [SerializeField] private float _rotationSpeed = 10f;

        private void Update()
        {
            transform.Rotate(_rotationAxis.normalized * _rotationSpeed * Time.deltaTime);
        }
    }
}