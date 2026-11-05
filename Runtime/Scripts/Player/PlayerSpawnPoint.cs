using UnityEngine;

namespace VastMetaverseTools
{
    public class PlayerSpawnPoint : MonoBehaviour
    {
        // TODO: Radius, player settings, etc.
        [SerializeField] private float _radius = 2f;

        public Vector3 GetSpawnPosition()
        {
            Vector2 randomCircle = Random.insideUnitCircle * _radius;
            Vector3 candidatePos = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
            if (Physics.Raycast(candidatePos + Vector3.up * 1.5f, Vector3.down, out RaycastHit hit, 5f, ~0, QueryTriggerInteraction.Ignore))
            {
                return hit.point + Vector3.up * 0.05f;
            }
            return candidatePos;
        }
        public Vector3 GetSpawnForward() => transform.forward;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, 0.5f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 1.5f, 0.5f);
        }
    }
}
