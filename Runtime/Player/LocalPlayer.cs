using UnityEngine;
using VastMetaverseTools.Runtime.Interactables;

namespace VastMetaverseTools.Runtime.Player
{
    public static class LocalPlayer
    {
        public static GameObject Instance { get; set; }
        public static PlayerController Controller { get; set; }
        public static PlayerInput Input { get; set; }
        public static PlayerInteractor Interactor { get; set; }
    }
}