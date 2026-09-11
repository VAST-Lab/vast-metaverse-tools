using System.Collections.Generic;
using UnityEngine;

namespace VastMetaverseTools.Attachments
{
    public enum AttachmentLocation
    {
        None,
        RightHand,
        RightFoot,
        //LeftHand,
        //LeftFoot,
        Hat
    }

    public class PlayerAttachmentManager : MonoBehaviour
    {
        public static PlayerAttachmentManager Instance { get; private set; }
        public static bool Exists => Instance != null;

        private PlayerAttachment _heldItem;
        public static PlayerAttachment HeldItem => Exists ? Instance._heldItem : null;
        public static event System.Action<AttachmentLocation> OnEquipmentAdded = delegate { };
        public static event System.Action<AttachmentLocation> OnEquipmentRemoved = delegate { };

        private Dictionary<AttachmentLocation, PlayerAttachment> _attachments;
        private Dictionary<AttachmentLocation, Transform> _attachmentTransforms;
        private static Dictionary<AttachmentLocation, HashSet<Transform>> _followers = new Dictionary<AttachmentLocation, HashSet<Transform>>();

        public bool PlayerHasEquipment(AttachmentLocation location) => _attachments.ContainsKey(location);

        private void Awake()
        {
            Instance = this;
            _attachments = new Dictionary<AttachmentLocation, PlayerAttachment>();
            _attachmentTransforms = new Dictionary<AttachmentLocation, Transform>();
        }

        private void Update()
        {
            UpdateAttachments();
            UpdateFollowers();
        }

        #region Attachments

        public bool GetAttachmentTransform(AttachmentLocation location, out Transform transform) =>
            _attachmentTransforms.TryGetValue(location, out transform);

        public bool TryGetHeldItem(out PlayerAttachment item) =>
            _attachments.TryGetValue(AttachmentLocation.RightHand, out item);

        private void UpdateAttachments()
        {
            foreach ((var location, var obj) in _attachments)
            {
                if (_attachmentTransforms.TryGetValue(location, out Transform t))
                {
                    obj.transform.SetPositionAndRotation(t.TransformPoint(obj.PositionOffset), t.rotation * Quaternion.Euler(obj.RotationOffset));
                }
            }
        }

        public bool Equip(PlayerAttachment attachment)
        {
            var location = attachment.Location;
            if (_attachments.ContainsKey(location))
                return false;

            var obj = Instantiate(attachment, transform);
            _attachments.Add(location, obj);

            if (location == AttachmentLocation.RightHand) _heldItem = obj;

            OnEquipmentAdded?.Invoke(location);
            return true;
        }

        public void DropHeldItem(bool spawnItem = true) => Drop(AttachmentLocation.RightHand, spawnItem);
        public void Drop(AttachmentLocation location, bool spawnItem = true)
        {
            if (_attachments.ContainsKey(location))
            {
                var attachment = _attachments[location];

                if (spawnItem)
                    Instantiate(attachment.DropPrefab, attachment.transform.position, attachment.transform.rotation);

                _attachments.Remove(location);
                Destroy(attachment.gameObject);

                _heldItem = null;
                OnEquipmentRemoved?.Invoke(location);
            }
        }

        public bool EatHeldItem()
        {
            var location = AttachmentLocation.RightHand;
            if (_attachments.TryGetValue(location, out var item) && item.CanEat)
            {
                _attachments.Remove(location);
                _heldItem = null;

                OnEquipmentRemoved?.Invoke(location);

                if (item.ItemAfterEating != null)
                {
                    Equip(item.ItemAfterEating);
                }

                Destroy(item.gameObject);
                return true;
            }
            return false;
        }

        public void SetAttachmentTransform(AttachmentLocation location, Transform t)
        {
            if (_attachmentTransforms.ContainsKey(location)) _attachmentTransforms[location] = t;
            else _attachmentTransforms.Add(location, t);
        }

        #endregion

        #region Followers

        private void UpdateFollowers()
        {
            foreach (var kvp in _followers)
            {
                kvp.Value.RemoveWhere(x => x == null);
                if (_attachmentTransforms.TryGetValue(kvp.Key, out Transform t))
                {
                    foreach (var follower in kvp.Value)
                    {
                        follower.SetPositionAndRotation(t.position, t.rotation);
                    }
                }
            }
        }

        public static void AddFollower(AttachmentLocation location, Transform follower)
        {
            if (location == AttachmentLocation.None) return;
            if (!_followers.ContainsKey(location)) _followers[location] = new HashSet<Transform>();
            _followers[location].Add(follower);
        }

        public static void RemoveFollower(AttachmentLocation location, Transform follower)
        {
            if (_followers.ContainsKey(location)) _followers[location].Remove(follower);
        }

        #endregion
    }
}