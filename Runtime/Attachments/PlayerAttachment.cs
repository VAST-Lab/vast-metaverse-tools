using Sirenix.OdinInspector;
using UnityEngine;

namespace VastMetaverseTools.Runtime.Attachments
{
    public class PlayerAttachment : MonoBehaviour
    {
        [SerializeField] private AttachmentLocation _location;
        [SerializeField] private Vector3 _positionOffset;
        [SerializeField] private Vector3 _rotationOffset;
        [SerializeField] private Sprite _icon;
        [SerializeField] private GameObject _dropPrefab;
        [SerializeField] private bool _canTrash;
        [SerializeField] private bool _canEat;
        [SerializeField, ShowIf(nameof(_canEat))] private PlayerAttachment _itemAfterEating;

        public AttachmentLocation Location => _location;
        public Vector3 PositionOffset => _positionOffset;
        public Vector3 RotationOffset => _rotationOffset;
        public Sprite Icon => _icon;
        public GameObject DropPrefab => _dropPrefab;
        public bool CanTrash => _canTrash;
        public bool CanEat => _canEat;
        public PlayerAttachment ItemAfterEating => _itemAfterEating;
    }
}