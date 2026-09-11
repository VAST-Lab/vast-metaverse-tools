using UnityEngine;

namespace VastMetaverseTools.Avatars
{
    [CreateAssetMenu(fileName = "New Avatar Reference", menuName = "TulsaUnion/AvatarReference")]
    public class AvatarReference : ScriptableObject
    {
        [SerializeField] private string _name;
        [SerializeField] private Sprite _icon;
        [SerializeField] private GameObject _avatarPrefab;

        public string Name => _name;
        public Sprite Icon => _icon;
        public GameObject Prefab => _avatarPrefab;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_name)) _name = name;
        }
    }
}