using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace VastMetaverseTools.Interactables
{
    public class InteractableVisualPoolManager : MonoBehaviour
    {
        public static InteractableVisualPoolManager Instance { get; private set; }

        [SerializeField] private InteractableVisual _visualPrefab;
        [SerializeField] private int _defaultCapacity = 20;
        [SerializeField] private int _maxSize = 100;
        [SerializeField] private bool _showAllInteractContainers;

        private ObjectPool<InteractableVisual> _pool;
        private Dictionary<Interactable, InteractableVisual> _activeVisuals;
        private List<Interactable> _staleKeys;
        private InteractableVisual _clickableVisual;

        private void Awake()
        {
            Instance = this;
            _activeVisuals = new Dictionary<Interactable, InteractableVisual>();
            _staleKeys = new List<Interactable>();

            _pool = new ObjectPool<InteractableVisual>(
                CreateVisual,
                OnGetVisual,
                OnReleaseVisual,
                OnDestroyVisual,
                true,
                _defaultCapacity,
                _maxSize
            );
        }

        public void ShowAllInteractContainers(bool show) => _showAllInteractContainers = show;

        private InteractableVisual CreateVisual()
        {
            return Instantiate(_visualPrefab, transform);
        }

        private void OnGetVisual(InteractableVisual visual)
        {
            visual.gameObject.SetActive(true);
        }

        private void OnReleaseVisual(InteractableVisual visual)
        {
            visual.gameObject.SetActive(false);
            visual.SetTarget(null);
        }

        private void OnDestroyVisual(InteractableVisual visual)
        {
            if (visual != null) Destroy(visual.gameObject);
        }

        public void UpdateVisuals(List<Interactable> visibleItems, Interactable closestItem, PlayerInteractor interactor)
        {
            _staleKeys.Clear();
            foreach (var kvp in _activeVisuals)
            {
                if (!visibleItems.Contains(kvp.Key) || !kvp.Key.CanInteract)
                {
                    _pool.Release(kvp.Value);
                    _staleKeys.Add(kvp.Key);
                }
            }

            foreach (var key in _staleKeys)
            {
                _activeVisuals.Remove(key);
            }

            foreach (var item in visibleItems)
            {
                if (!_activeVisuals.TryGetValue(item, out InteractableVisual visual))
                {
                    visual = _pool.Get();
                    visual.SetTarget(item);
                    _activeVisuals.Add(item, visual);
                }

                visual.SetState(!interactor.HasOverrideInteractor && (_showAllInteractContainers || item == closestItem));
            }
        }
    }
}