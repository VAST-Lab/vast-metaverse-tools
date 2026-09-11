using UnityEngine;
using VastMetaverseTools.Save;
using VastMetaverseTools.UI;

namespace VastMetaverseTools.Badges
{
    [CreateAssetMenu(fileName = "New Badge", menuName = "TulsaUnion/Badge")]
    public class Badge : ScriptableObject
    {
        [SerializeField] private string _name;

        public string Name => _name;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_name)) _name = name;
        }

        public void AwardBadge()
        {
            if (PlayerSaveManager.Instance != null && !PlayerSaveManager.Instance.HasBadge(_name))
            {
                PlayerSaveManager.Instance.SaveBadge(_name);
                if (UIManager.Instance != null) UIManager.Instance.ShowBadgeEarned(this);
            }
        }
    }
}