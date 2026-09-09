using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VastMetaverseTools.Runtime.Save
{
    [System.Serializable]
    public class HighScoreEntry
    {
        public string PlayerName;
        public int Score;
    }

    [System.Serializable]
    public class HighScoreData
    {
        public List<HighScoreEntry> Scores = new List<HighScoreEntry>();
    }

    public class SpaceSaveManager : MonoBehaviour
    {
        public static SpaceSaveManager Instance { get; private set; }

        private HighScoreData _highScoreData = new HighScoreData();

        private void Awake()
        {
            Instance = this;
            LoadData();
        }

        public int GetHighScore(string playerName)
        {
            var entry = _highScoreData.Scores.FirstOrDefault(x => x.PlayerName == playerName);
            return entry != null ? entry.Score : 0;
        }

        public void SetHighScore(string playerName, int score)
        {
            var entry = _highScoreData.Scores.FirstOrDefault(x => x.PlayerName == playerName);
            if (entry != null)
            {
                if (score > entry.Score)
                {
                    entry.Score = score;
                    SaveData();
                }
            }
            else
            {
                _highScoreData.Scores.Add(new HighScoreEntry { PlayerName = playerName, Score = score });
                SaveData();
            }
        }

        public List<HighScoreEntry> GetAllHighScores() => _highScoreData.Scores;

        private void LoadData()
        {
            string json = PlayerPrefs.GetString("SpaceHighScores", "");
            if (!string.IsNullOrEmpty(json))
            {
                _highScoreData = JsonUtility.FromJson<HighScoreData>(json);
            }
        }

        private void SaveData()
        {
            string json = JsonUtility.ToJson(_highScoreData);
            PlayerPrefs.SetString("SpaceHighScores", json);
            PlayerPrefs.Save();
        }
    }
}