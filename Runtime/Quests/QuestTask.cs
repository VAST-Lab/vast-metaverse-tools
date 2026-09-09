using UnityEngine;
using VastMetaverseTools.Runtime.Save;
using VastMetaverseTools.Runtime.UI;

namespace VastMetaverseTools.Runtime.Quests
{
    [CreateAssetMenu(fileName = "New Quest Task", menuName = "TulsaUnion/QuestTask")]
    public class QuestTask : ScriptableObject
    {
        public event System.Action OnCompleteTask = delegate { };

        public void CompleteTask()
        {
            if (PlayerSaveManager.Instance != null && !PlayerSaveManager.Instance.IsQuestCompleted(name))
            {
                PlayerSaveManager.Instance.SaveQuestCompletion(name);
                if (UIManager.Instance != null) UIManager.Instance.ShowToast($"Quest Task Completed: {name}");
                OnCompleteTask?.Invoke();
            }
        }
    }
}