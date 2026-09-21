using UnityEngine;
using VastMetaverseTools.Interactables;
using VastMetaverseTools.Networking;
using VastMetaverseTools.Player;

namespace VastMetaverseTools.Managers
{
    public class MetaverseManager : MonoBehaviour
    {
        public static MetaverseManager Instance { get; private set; }

        [SerializeField] private NetworkSyncedObject _playerPrefab;
        [SerializeField] private bool _supportCurrency;

        public static PlayerController LocalPlayer { get; private set; }
        public static PlayerInput LocalPlayerInput { get; private set; }
        public static PlayerInteractor LocalPlayerInteractor { get; private set; }

        private void Awake()
        {
            Instance = this;
            var spawnPoint = FindFirstObjectByType<PlayerSpawnPoint>();
            var spawnPos = spawnPoint != null ? spawnPoint.GetSpawnPosition() : transform.position;
            var spawnForward = spawnPoint != null ? spawnPoint.GetSpawnForward() : transform.forward;
            spawnForward.y = 0f;
            var spawnRot = spawnForward != Vector3.zero ? Quaternion.LookRotation(spawnForward) : Quaternion.identity;
            var player = Instantiate(_playerPrefab, spawnPos, spawnRot);
            player.TakeOwnership();
            LocalPlayer = player.GetComponent<PlayerController>();
            LocalPlayerInput = player.GetComponent<PlayerInput>();
            LocalPlayerInteractor = player.GetComponent<PlayerInteractor>();
        }

        public static void DisableLocalInput()
        {
            if (LocalPlayer != null) LocalPlayer.LockMovement(true);
            if (LocalPlayerInput != null) LocalPlayerInput.SetInputEnabled(false);
        }

        public static void EnableLocalInput()
        {
            if (LocalPlayer != null) LocalPlayer.LockMovement(false);
            if (LocalPlayerInput != null) LocalPlayerInput.SetInputEnabled(true);
        }

        public static void ToggleLocalAvatarVisibility()
        {
            // TODO: Just disable art
            if (Instance != null) LocalPlayer.gameObject.SetActive(!LocalPlayer.gameObject.activeSelf);
        }
    }
}
