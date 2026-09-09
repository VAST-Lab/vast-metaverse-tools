using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace VastMetaverseTools.Runtime.Quests
{
    public class QuestManager : MonoBehaviour
    {
        [SerializeField] private string _questName;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private bool _startAutomatically;
        [SerializeField] private List<QuestTask> _tasks;
        [SerializeField] private UnityEvent _onCompleteQuest;

        private void Start()
        {
            if (_startAutomatically) StartQuest();
        }

        public void StartQuest()
        {

        }
    }
}