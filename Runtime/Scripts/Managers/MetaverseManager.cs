using UnityEngine;
using UnityEngine.InputSystem;
using VastMetaverseTools.Player;

namespace VastMetaverseTools.Managers
{
    public class MetaverseManager : MonoBehaviour
    {
        public static MetaverseManager Instance { get; private set; }

        [SerializeField] private PlayerController _playerPrefab;
        [SerializeField] private RuntimeAnimatorController _playerAnimController;
        [SerializeField] private InputActionAsset _playerInputMapping;
        [SerializeField] private string _websiteUrl;
        [SerializeField] private bool _supportCurrency;

        public static string WebsiteUrl => Instance._websiteUrl;
        public static PlayerController LocalPlayer { get; private set; }
        public static bool DeferInitialPlayerSpawn { get; set; }

        public RuntimeAnimatorController PlayerAnimController => _playerAnimController;
        public InputActionAsset PlayerInputMapping => _playerInputMapping;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (!DeferInitialPlayerSpawn && LocalPlayer == null)
            {
                SpawnSingleplayer();
            }
        }

        public void GetSpawnTransform(out Vector3 position, out Quaternion rotation)
        {
            var spawnPoint = FindAnyObjectByType<PlayerSpawnPoint>();
            position = spawnPoint != null ? spawnPoint.GetSpawnPosition() : transform.position;
            var spawnForward = spawnPoint != null ? spawnPoint.GetSpawnForward() : transform.forward;
            spawnForward.y = 0f;
            rotation = spawnForward != Vector3.zero ? Quaternion.LookRotation(spawnForward) : Quaternion.identity;
        }

        public void SpawnSingleplayer()
        {
            GetSpawnTransform(out Vector3 spawnPos, out Quaternion spawnRot);
            var player = Instantiate(_playerPrefab, spawnPos, spawnRot);
            RegisterLocalPlayer(player);
        }

        public void RegisterLocalPlayer(PlayerController player)
        {
            if (LocalPlayer != null && LocalPlayer != player)
            {
                Destroy(LocalPlayer.gameObject);
            }
            LocalPlayer = player;
            LocalPlayer.SyncedObject.TakeOwnership();
            LocalPlayer.InputReader.SetInputMapping(_playerInputMapping);
            LocalPlayer.AnimController.SetAnimationController(_playerAnimController);
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
            if (Instance != null) LocalPlayer.TogglePlayerVisualsShown();
        }

        public static PlatformType GetPlatform()
        {
#if UNITY_WEBGL
            return PlatformType.Web;
#else
            return PlatformType.PC;
#endif
        }
    }

    public enum PlatformType
    {
        Unknown,
        PC,
        Web,
        Mobile,
        Quest,
        OpenXR
    }
}
