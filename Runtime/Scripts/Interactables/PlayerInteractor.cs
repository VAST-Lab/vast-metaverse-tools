using System;
using System.Collections.Generic;
using UnityEngine;
using VastMetaverseTools.Attachments;
using VastMetaverseTools.Player;
using VastMetaverseTools.UI;

namespace VastMetaverseTools.Interactables
{
    public interface IOverrideInteractor
    {
        string PrimaryInteractText { get; }
        string SecondaryInteractText { get; }
        bool OnPrimaryInteract(GameObject player);
        bool OnSecondaryInteract(GameObject player);
    }

    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private int _maxVisibleInteractables = 10;

        private IOverrideInteractor _overrideInteractor;
        private Interactable _current;
        private List<Interactable> _visibleInteractables = new List<Interactable>();
        private List<InteractableDistance> _nearbyInteractables = new List<InteractableDistance>();
        private bool _checkUpdateUI;

        private struct InteractableDistance : IComparable<InteractableDistance>
        {
            public Interactable Interactable;
            public float SqrDistance;

            public readonly int CompareTo(InteractableDistance other) => SqrDistance.CompareTo(other.SqrDistance);
        }

        private void Start()
        {
            if (LocalPlayer.Input != null)
            {
                LocalPlayer.Input.OnPrimaryInteract += HandlePrimaryInteract;
                LocalPlayer.Input.OnSecondaryInteract += HandleSecondaryInteract;
            }
        }

        private void OnDestroy()
        {
            if (LocalPlayer.Input != null)
            {
                LocalPlayer.Input.OnPrimaryInteract -= HandlePrimaryInteract;
                LocalPlayer.Input.OnSecondaryInteract -= HandleSecondaryInteract;
            }
        }

        private void Update()
        {
            UpdateInteractables();

            if (_checkUpdateUI && _overrideInteractor == null)
            {
                UIManager.Instance.SetOverridePrompts(PlayerAttachmentManager.HeldItem != null ? "Drop" : null, null);
            }
        }

        public void SetOverrideInteractor(IOverrideInteractor interactor, bool set)
        {
            if (set)
            {
                _overrideInteractor = interactor;
                UIManager.Instance.SetOverridePrompts(_overrideInteractor.PrimaryInteractText, _overrideInteractor.SecondaryInteractText);
            }
            else if (_overrideInteractor == interactor)
            {
                ClearOverridePrompts();
            }
        }

        private void UpdateInteractables()
        {
            _nearbyInteractables.Clear();
            _visibleInteractables.Clear();
            Interactable closest = null;

            Vector3 position = transform.position;

            foreach (Interactable interactable in Interactable.All)
            {
                if (!interactable.CanInteract) continue;

                float sqrDistance = (position - interactable.transform.position).sqrMagnitude - interactable.DistancePriorityOffset;
                float visRadiusSqr = interactable.VisibilityRadius * interactable.VisibilityRadius;

                if (sqrDistance <= visRadiusSqr)
                {
                    _nearbyInteractables.Add(new InteractableDistance
                    {
                        Interactable = interactable,
                        SqrDistance = sqrDistance
                    });
                }
            }

            _nearbyInteractables.Sort();

            int count = Mathf.Min(_nearbyInteractables.Count, _maxVisibleInteractables);
            for (int i = 0; i < count; i++)
            {
                var item = _nearbyInteractables[i];
                _visibleInteractables.Add(item.Interactable);

                if (closest == null)
                {
                    float intRadiusSqr = item.Interactable.InteractRadius * item.Interactable.InteractRadius;
                    if (item.SqrDistance <= intRadiusSqr)
                    {
                        closest = item.Interactable;
                    }
                }
            }

            if (_current != closest)
            {
                _current = closest;
            }

            if (InteractableVisualPoolManager.Instance != null)
            {
                InteractableVisualPoolManager.Instance.UpdateVisuals(_visibleInteractables, _current);
            }
        }

        private void HandlePrimaryInteract()
        {
            if (_overrideInteractor != null)
            {
                if (_overrideInteractor.OnPrimaryInteract(gameObject))
                {
                    ClearOverridePrompts();
                }
            }
            else if (_current != null)
            {
                _current.Interact(gameObject);
            }
            else if (PlayerAttachmentManager.HeldItem != null)
            {
                PlayerAttachmentManager.Instance.DropHeldItem();
            }
            _checkUpdateUI = true;
        }

        private void HandleSecondaryInteract()
        {
            if (_overrideInteractor != null)
            {
                if (_overrideInteractor.OnSecondaryInteract(gameObject))
                {
                    ClearOverridePrompts();
                }
            }
            _checkUpdateUI = true;
        }

        private void ClearOverridePrompts()
        {
            _overrideInteractor = null;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetOverridePrompts(PlayerAttachmentManager.HeldItem != null ? "Drop" : null, null);
            }
        }
    }
}