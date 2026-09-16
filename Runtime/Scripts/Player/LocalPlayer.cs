using UnityEngine;
using VastMetaverseTools.Interactables;

namespace VastMetaverseTools.Player
{
    public static class LocalPlayer
    {
        public static GameObject Instance { get; set; }
        public static PlayerController Controller { get; set; }
        public static PlayerInput Input { get; set; }
        public static PlayerInteractor Interactor { get; set; }

        public static void DisableInput()
        {
            if (Controller != null) Controller.LockMovement(true);
            if (Input != null) Input.SetInputEnabled(false);
        }

        public static void EnableInput()
        {
            if (Controller != null) Controller.LockMovement(false);
            if (Input != null) Input.SetInputEnabled(true);
        }

        public static void ToggleLocalAvatarVisibility()
        {
            // TODO: Just disable art
            if (Instance != null) Instance.SetActive(!Instance.activeSelf);
        }
    }
}