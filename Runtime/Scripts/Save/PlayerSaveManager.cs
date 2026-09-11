using System.Collections.Generic;
using UnityEngine;

namespace VastMetaverseTools.Save
{
    public class PlayerSaveManager : MonoBehaviour
    {
        public static PlayerSaveManager Instance { get; private set; }

        private HashSet<string> _earnedBadges = new HashSet<string>();
        private HashSet<string> _completedQuests = new HashSet<string>();

        private void Awake()
        {
            Instance = this;
            LoadData();
        }

        public bool HasBadge(string badgeId) => _earnedBadges.Contains(badgeId);

        public void SaveBadge(string badgeId)
        {
            if (_earnedBadges.Add(badgeId))
            {
                SaveData();
            }
        }

        public bool IsQuestCompleted(string questId) => _completedQuests.Contains(questId);

        public void SaveQuestCompletion(string questId)
        {
            if (_completedQuests.Add(questId))
            {
                SaveData();
            }
        }

        private void LoadData()
        {
            string badges = PlayerPrefs.GetString("PlayerBadges", "");
            _earnedBadges = new HashSet<string>(badges.Split(','));

            string quests = PlayerPrefs.GetString("PlayerQuests", "");
            _completedQuests = new HashSet<string>(quests.Split(','));
        }

        private void SaveData()
        {
            PlayerPrefs.SetString("PlayerBadges", string.Join(",", _earnedBadges));
            PlayerPrefs.SetString("PlayerQuests", string.Join(",", _completedQuests));
            PlayerPrefs.Save();
        }
    }
}