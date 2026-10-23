using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using VastMetaverseTools.Networking;

namespace VastMetaverseTools.Player
{
    // Hides the local player's model when the camera gets too close, so you
    // never see inside it. Its shadow stays, so the player still feels present.
    public class HideWhenCameraClose : MonoBehaviour
    {
        [Tooltip("Hide the model when the camera is closer than this (meters).")]
        [SerializeField] private float _hideDistance = 0.8f;
        [Tooltip("Show it again once the camera is farther than this. Keep it a bit bigger than Hide Distance so it doesn't flicker.")]
        [SerializeField] private float _showDistance = 1.0f;
        [Tooltip("Roughly chest height, so distance is measured from the body instead of the feet.")]
        [SerializeField] private float _centerHeight = 1.0f;

        private readonly List<Renderer> _renderers = new List<Renderer>();
        private readonly Dictionary<Renderer, ShadowCastingMode> _originalShadows = new Dictionary<Renderer, ShadowCastingMode>();
        private NetworkSyncedObject _syncObject;
        private bool _hidden;

        private void Awake()
        {
            _syncObject = GetComponent<NetworkSyncedObject>();
        }

        private void LateUpdate()
        {
            // Only hide your own character, never other players'.
            if (_syncObject != null && !_syncObject.IsOwner) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            float dist = Vector3.Distance(cam.transform.position, transform.position + Vector3.up * _centerHeight);
            if (!_hidden && dist < _hideDistance) _hidden = true;
            else if (_hidden && dist > _showDistance) _hidden = false;

            // Re-collect every frame so it keeps working after switching avatars.
            GetComponentsInChildren(true, _renderers);
            foreach (Renderer r in _renderers)
            {
                if (!_originalShadows.ContainsKey(r)) _originalShadows[r] = r.shadowCastingMode;
                r.shadowCastingMode = _hidden ? ShadowCastingMode.ShadowsOnly : _originalShadows[r];
            }
        }
    }
}