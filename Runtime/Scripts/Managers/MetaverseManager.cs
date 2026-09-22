using UnityEngine;
using UnityEngine.InputSystem;
using VastMetaverseTools.Player;

namespace VastMetaverseTools.Managers
{
    public class MetaverseManager : MonoBehaviour
    {
        public static MetaverseManager Instance { get; private set; }

        [SerializeField] private PlayerController _playerPrefab;
        [SerializeField] private InputActionAsset _playerInputMapping;
        [SerializeField] private bool _supportCurrency;

        public static PlayerController LocalPlayer { get; private set; }

        private void Awake()
        {
            Instance = this;
            var spawnPoint = FindAnyObjectByType<PlayerSpawnPoint>();
            var spawnPos = spawnPoint != null ? spawnPoint.GetSpawnPosition() : transform.position;
            var spawnForward = spawnPoint != null ? spawnPoint.GetSpawnForward() : transform.forward;
            spawnForward.y = 0f;
            var spawnRot = spawnForward != Vector3.zero ? Quaternion.LookRotation(spawnForward) : Quaternion.identity;
            LocalPlayer = Instantiate(_playerPrefab, spawnPos, spawnRot);
            LocalPlayer.SyncedObject.TakeOwnership();
            LocalPlayer.InputReader.SetInputMapping(_playerInputMapping);
        }

        public static void DisableLocalInput()
        {
            if (LocalPlayer == null) return;
            LocalPlayer.LockMovement(true);
            LocalPlayer.InputReader.SetInputEnabled(false);
        }

        public static void EnableLocalInput()
        {
            if (LocalPlayer == null) return;
            LocalPlayer.LockMovement(false);
            LocalPlayer.InputReader.SetInputEnabled(true);
        }

        public static void ToggleLocalAvatarVisibility()
        {
            // TODO: Just disable art
            if (Instance != null) LocalPlayer.gameObject.SetActive(!LocalPlayer.gameObject.activeSelf);
        }
    }
}
