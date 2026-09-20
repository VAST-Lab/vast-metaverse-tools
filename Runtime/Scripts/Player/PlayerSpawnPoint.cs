using UnityEngine;

namespace VastMetaverseTools
{
    public class PlayerSpawnPoint : MonoBehaviour
    {
        // TODO: Radius, player settings, etc.
        [SerializeField] private float _radius = 2f;

        public Vector3 GetSpawnPosition() => transform.position;
        public Vector3 GetSpawnForward() => transform.forward;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, 0.5f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 1.5f, 0.5f);
        }
    }
}
