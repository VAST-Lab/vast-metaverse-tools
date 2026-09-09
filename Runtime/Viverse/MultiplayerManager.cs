using UnityEngine;
using System;
using System.Runtime.InteropServices;
using ViverseSDK.Login;

namespace ViverseSDK.Multiplayer
{
    [System.Serializable]
    public class RoomInfo
    {
        public string id;
        public string name;
        public int max_players;
        public int min_players;
        public string master_client_id;
        public bool is_closed;
        public int playerCount;
    }

    [System.Serializable]
    public class RoomListResult
    {
        public RoomInfo[] rooms;
    }

    [System.Serializable]
    public class PositionData
    {
        public float x, y, z, w;
    }

    [System.Serializable]
    public class NetworkSyncEvent
    {
        public int entity_type; // 1 = user, 2 = entity
        public string user_id;
        public string entity_id;
        public PositionData data;
    }

    /// <summary>
    /// MultiplayerManager - handles VIVERSE room creation/joining and real-time
    /// position sync between players. Mirrors the JS-bridge pattern used by
    /// LoginManager.cs. Requires the VIVERSE SDK to already be loaded/initialized
    /// (i.e. login flow should run first).
    /// </summary>
    public class MultiplayerManager : MonoBehaviour
    {
        [Header("Config")]
        public string appId;
        public string roomName = "TulsaUnionRoom";
        public int maxPlayers = 8;
        public int minPlayers = 1;

        public event Action OnMatchmakingReady;
        public event Action<RoomInfo> OnRoomJoined;
        public event Action OnConnected;
        public event Action<string> OnPlayerJoined;
        public event Action<string> OnPlayerLeft;
        public event Action<NetworkSyncEvent> OnRemotePositionUpdate;
        public event Action<string> OnMultiplayerErrorEvent;

        private string currentRoomId;
        private LoginManager loginManager;

        // Waits for the VIVERSE SDK to finish loading (via LoginManager) before
        // attempting to initialize multiplayer. This replaces the earlier
        // temporary auto-Initialize() test, which failed because the SDK
        // wasn't loaded yet at that point.
        private void Start()
        {
            loginManager = FindObjectOfType<LoginManager>();

            // TEMPORARY: auto-create a room once matchmaking is ready, so we can
            // test room creation without building a UI yet. Remove/replace this
            // once real player join logic exists.
            OnMatchmakingReady += HandleMatchmakingReadyTest;

            if (loginManager == null)
            {
                Debug.LogError("[MultiplayerManager] No LoginManager found in scene. " +
                                "Multiplayer requires the VIVERSE SDK to be loaded first.");
                return;
            }

            if (loginManager.IsSDKReady)
            {
                // SDK was already loaded before we got here (e.g. by something else)
                Debug.Log("[MultiplayerManager] SDK already ready, initializing immediately.");
                Initialize();
            }
            else
            {
                Debug.Log("[MultiplayerManager] Waiting for LoginManager.OnSDKReady...");
                loginManager.OnSDKReady += HandleSDKReady;

                // Kick off SDK loading. Reuses the same App Id configured on this
                // component; LoginManager itself guards against double-loading.
                loginManager.Initialize(appId);
            }
        }

        private void OnDestroy()
        {
            if (loginManager != null)
            {
                loginManager.OnSDKReady -= HandleSDKReady;
            }
        }

        private void HandleSDKReady()
        {
            Debug.Log("[MultiplayerManager] SDK ready, initializing multiplayer now.");
            Initialize();
        }

        // TEMPORARY: test-only handler, creates a room automatically with a
        // random session ID and a generic test name.
        private void HandleMatchmakingReadyTest()
        {
            string testSessionId = System.Guid.NewGuid().ToString();
            string testPlayerName = "TestPlayer_" + testSessionId.Substring(0, 6);

            Debug.Log($"[MultiplayerManager] TEST: Creating room as '{testPlayerName}' " +
                      $"(session: {testSessionId})");

            CreateOrJoinRoom(testSessionId, testPlayerName);
        }

        #region JSLib Imports

        [DllImport("__Internal")] private static extern void VIVERSE_MP_Init(string gameObjectName);
        [DllImport("__Internal")] private static extern void VIVERSE_MP_CreateMatchmakingClient(string appId);
        [DllImport("__Internal")] private static extern void VIVERSE_MP_SetActor(string sessionId, string name);
        [DllImport("__Internal")] private static extern void VIVERSE_MP_CreateRoom(string roomName, int maxPlayers, int minPlayers);
        [DllImport("__Internal")] private static extern void VIVERSE_MP_JoinRoom(string roomId);
        [DllImport("__Internal")] private static extern void VIVERSE_MP_GetAvailableRooms();
        [DllImport("__Internal")] private static extern void VIVERSE_MP_InitPositionSync(string roomId, string appId, string userSessionId);
        [DllImport("__Internal")] private static extern void VIVERSE_MP_UpdateMyPosition(float x, float y, float z, float w);

        #endregion

        /// <summary>
        /// Call once, after VIVERSE SDK/login has already been initialized.
        /// </summary>
        public void Initialize()
        {
#if !UNITY_EDITOR && UNITY_WEBGL
            VIVERSE_MP_Init(gameObject.name);
            VIVERSE_MP_CreateMatchmakingClient(appId);
#else
            Debug.Log("[MultiplayerManager] Multiplayer only works in WebGL builds.");
#endif
        }

        /// <summary>
        /// Sets actor info then creates a room. For this first pass we always
        /// create a new room rather than searching for an existing one to join.
        /// </summary>
        /// <summary>
        /// Sets actor info, then checks for an existing open room to join
        /// before falling back to creating a new one. This lets multiple
        /// clients end up in the same room instead of each creating their own.
        /// </summary>
        public void CreateOrJoinRoom(string sessionId, string playerName)
        {
#if !UNITY_EDITOR && UNITY_WEBGL
            VIVERSE_MP_SetActor(sessionId, playerName);
            VIVERSE_MP_GetAvailableRooms();
#else
            Debug.Log("[MultiplayerManager] Multiplayer only works in WebGL builds.");
#endif
        }

        /// <summary>
        /// Call periodically (e.g. every 0.1-0.2s) from your PlayerController
        /// to broadcast this client's position to other players.
        /// </summary>
        public void SendMyPosition(Vector3 position, float rotationY)
        {
#if !UNITY_EDITOR && UNITY_WEBGL
            VIVERSE_MP_UpdateMyPosition(position.x, position.y, position.z, rotationY);
#endif
        }

        // ----------------------------------------------------
        // Callbacks invoked from JavaScript (names must match exactly)
        // ----------------------------------------------------

        public void OnMatchmakingClientReady(string _)
        {
            Debug.Log("[MultiplayerManager] Matchmaking client ready.");
            OnMatchmakingReady?.Invoke();
        }

        public void OnActorSet(string resultJson)
        {
            Debug.Log("[MultiplayerManager] Actor set: " + resultJson);
        }

        public void OnRoomCreated(string roomJson)
        {
            Debug.Log("[MultiplayerManager] Room created: " + roomJson);
            // NOTE: this payload wraps the room object inside a top-level
            // "room" property (e.g. {"success":true,"room":{"id":...}}),
            // unlike OnJoinedRoom's payload which has "id" at the top level.
            // OnJoinedRoom always fires first with a reliably-shaped room
            // object, so we rely on that to set currentRoomId and trigger
            // position sync - this event is just a confirmation log.
        }

        public void OnJoinedRoom(string roomJson)
        {
            Debug.Log("[MultiplayerManager] Joined room: " + roomJson);
            HandleRoomJoined(roomJson);
        }

        private void HandleRoomJoined(string roomJson)
        {
            try
            {
                RoomInfo room = JsonUtility.FromJson<RoomInfo>(roomJson);
                currentRoomId = room.id;
                OnRoomJoined?.Invoke(room);

#if !UNITY_EDITOR && UNITY_WEBGL
                // Wait one frame before calling back into JS - avoids being
                // nested inside another active native call on the stack.
                StartCoroutine(StartPositionSyncNextFrame(currentRoomId));
#endif
            }
            catch (Exception ex)
            {
                Debug.LogError("[MultiplayerManager] Failed to parse room JSON: " + ex.Message);
            }
        }

        public void OnRoomActorsChanged(string actorsJson)
        {
            Debug.Log("[MultiplayerManager] Room actors changed: " + actorsJson);
        }

#if !UNITY_EDITOR && UNITY_WEBGL
        private System.Collections.IEnumerator StartPositionSyncNextFrame(string roomId)
        {
            yield return null; // wait one full frame - fully exits any active native call stack
            VIVERSE_MP_InitPositionSync(roomId, appId, null);
        }
#endif

        public void OnMultiplayerConnected(string _)
        {
            Debug.Log("[MultiplayerManager] Multiplayer connected.");
            OnConnected?.Invoke();
        }

        public void OnPlayerConnected(string sessionId)
        {
            Debug.Log("[MultiplayerManager] Player connected: " + sessionId);
            OnPlayerJoined?.Invoke(sessionId);
        }

        public void OnPlayerDisconnected(string sessionId)
        {
            Debug.Log("[MultiplayerManager] Player disconnected: " + sessionId);
            OnPlayerLeft?.Invoke(sessionId);
        }

        public void OnPositionSyncReady(string infoJson)
        {
            Debug.Log("[MultiplayerManager] Position sync ready: " + infoJson);
        }

        public void OnPositionUpdate(string dataJson)
        {
            try
            {
                NetworkSyncEvent evt = JsonUtility.FromJson<NetworkSyncEvent>(dataJson);
                OnRemotePositionUpdate?.Invoke(evt);
            }
            catch (Exception ex)
            {
                Debug.LogError("[MultiplayerManager] Failed to parse position update: " + ex.Message);
            }
        }

        public void OnPositionRemove(string dataJson)
        {
            Debug.Log("[MultiplayerManager] Position remove: " + dataJson);
        }

        public void OnAvailableRooms(string roomsJson)
        {
            Debug.Log("[MultiplayerManager] Available rooms: " + roomsJson);

            try
            {
                RoomListResult result = JsonUtility.FromJson<RoomListResult>(roomsJson);
                RoomInfo openRoom = null;

                if (result != null && result.rooms != null)
                {
                    foreach (RoomInfo room in result.rooms)
                    {
                        bool nameMatches = room.name == roomName;
                        bool hasSpace = room.playerCount < (room.max_players > 0 ? room.max_players : maxPlayers);

                        if (nameMatches && !room.is_closed && hasSpace)
                        {
                            openRoom = room;
                            break;
                        }
                    }
                }

#if !UNITY_EDITOR && UNITY_WEBGL
                if (openRoom != null)
                {
                    Debug.Log($"[MultiplayerManager] Found existing room '{openRoom.name}' " +
                              $"({openRoom.id}), joining it.");
                    VIVERSE_MP_JoinRoom(openRoom.id);
                }
                else
                {
                    Debug.Log("[MultiplayerManager] No open room found, creating a new one.");
                    VIVERSE_MP_CreateRoom(roomName, maxPlayers, minPlayers);
                }
#endif
            }
            catch (Exception ex)
            {
                Debug.LogError("[MultiplayerManager] Failed to parse available rooms, falling back to CreateRoom: " + ex.Message);
#if !UNITY_EDITOR && UNITY_WEBGL
                VIVERSE_MP_CreateRoom(roomName, maxPlayers, minPlayers);
#endif
            }
        }

        public void OnMultiplayerError(string errorMsg)
        {
            Debug.LogError("[MultiplayerManager] Error: " + errorMsg);
            OnMultiplayerErrorEvent?.Invoke(errorMsg);
        }
    }
}