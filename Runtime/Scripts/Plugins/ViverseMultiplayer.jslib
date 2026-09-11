mergeInto(LibraryManager.library, {

    _mpGameObjectName: null,

    // ----------------------------------------------------
    // Init the Play SDK client (requires VIVERSE SDK already loaded via login flow)
    // ----------------------------------------------------
    VIVERSE_MP_Init: function (gameObjectNameStr) {
        this._mpGameObjectName = UTF8ToString(gameObjectNameStr);
        console.log("[ViverseMultiplayer] Init, GameObject: " + this._mpGameObjectName);

        if (!globalThis.viverse) {
            console.error("[ViverseMultiplayer] VIVERSE SDK not loaded yet.");
            SendMessage(this._mpGameObjectName, 'OnMultiplayerError', "SDK not loaded");
            return;
        }

        try {
            globalThis.playClient = new globalThis.viverse.play();
            console.log("[ViverseMultiplayer] playClient created.");
        } catch (error) {
            console.error("[ViverseMultiplayer] Failed to create playClient", error);
            SendMessage(this._mpGameObjectName, 'OnMultiplayerError', "Failed to init play client: " + error.message);
        }
    },

    // ----------------------------------------------------
    // Create the matchmaking client and wire up its events
    // ----------------------------------------------------
    VIVERSE_MP_CreateMatchmakingClient: async function (appIdStr) {
        var appId = UTF8ToString(appIdStr);
        var gameObjectName = this._mpGameObjectName;

        if (!globalThis.playClient) {
            SendMessage(gameObjectName, 'OnMultiplayerError', "playClient not initialized");
            return;
        }

        try {
            globalThis.matchmakingClient = await globalThis.playClient.newMatchmakingClient(appId);
            console.log("[ViverseMultiplayer] matchmakingClient created.");

            globalThis.matchmakingClient.on("onJoinRoom", function (room) {
                SendMessage(gameObjectName, 'OnJoinedRoom', JSON.stringify(room));
            });

            globalThis.matchmakingClient.on("onRoomActorChange", function (actors) {
                SendMessage(gameObjectName, 'OnRoomActorsChanged', JSON.stringify(actors));
            });

            globalThis.matchmakingClient.on("onError", function (error) {
                SendMessage(gameObjectName, 'OnMultiplayerError', "Matchmaking error: " + error.message);
            });

            // IMPORTANT: the matchmaking client's WebSocket is still opening
            // right after construction. Calling setActor/createRoom before it
            // finishes connecting throws "Still in CONNECTING state". We must
            // wait for its onConnect event before telling C# it's safe to proceed.
            // Note: this client uses .on('onConnect', cb) - different naming
            // from multiplayerClient's .onConnected(cb) used elsewhere.
            globalThis.matchmakingClient.on("onConnect", function () {
                console.log("[ViverseMultiplayer] matchmakingClient connected (onConnect fired).");
                SendMessage(gameObjectName, 'OnMatchmakingClientReady', '');
            });
        } catch (error) {
            console.error("[ViverseMultiplayer] Failed to create matchmaking client", error);
            SendMessage(gameObjectName, 'OnMultiplayerError', "Failed to create matchmaking client: " + error.message);
        }
    },

    // ----------------------------------------------------
    // Set the player's session info before creating/joining a room
    // ----------------------------------------------------
    VIVERSE_MP_SetActor: async function (sessionIdStr, nameStr) {
        var sessionId = UTF8ToString(sessionIdStr);
        var name = UTF8ToString(nameStr);
        var gameObjectName = this._mpGameObjectName;

        if (!globalThis.matchmakingClient) {
            SendMessage(gameObjectName, 'OnMultiplayerError', "matchmakingClient not initialized");
            return;
        }

        try {
            var result = await globalThis.matchmakingClient.setActor({
                session_id: sessionId,
                name: name,
                properties: {}
            });
            SendMessage(gameObjectName, 'OnActorSet', JSON.stringify(result));
        } catch (error) {
            SendMessage(gameObjectName, 'OnMultiplayerError', "setActor failed: " + error.message);
        }
    },

    // ----------------------------------------------------
    // Create a room
    // ----------------------------------------------------
    VIVERSE_MP_CreateRoom: async function (roomNameStr, maxPlayers, minPlayers) {
        var roomName = UTF8ToString(roomNameStr);
        var gameObjectName = this._mpGameObjectName;

        if (!globalThis.matchmakingClient) {
            SendMessage(gameObjectName, 'OnMultiplayerError', "matchmakingClient not initialized");
            return;
        }

        try {
            var room = await globalThis.matchmakingClient.createRoom({
                name: roomName,
                mode: 'team',
                maxPlayers: maxPlayers,
                minPlayers: minPlayers,
                properties: {}
            });
            SendMessage(gameObjectName, 'OnRoomCreated', JSON.stringify(room));
        } catch (error) {
            SendMessage(gameObjectName, 'OnMultiplayerError', "createRoom failed: " + error.message);
        }
    },

    // ----------------------------------------------------
    // Join an existing room by ID
    // ----------------------------------------------------
    VIVERSE_MP_JoinRoom: async function (roomIdStr) {
        var roomId = UTF8ToString(roomIdStr);
        var gameObjectName = this._mpGameObjectName;

        if (!globalThis.matchmakingClient) {
            SendMessage(gameObjectName, 'OnMultiplayerError', "matchmakingClient not initialized");
            return;
        }

        try {
            var room = await globalThis.matchmakingClient.joinRoom(roomId);
            SendMessage(gameObjectName, 'OnJoinedRoom', JSON.stringify(room));
        } catch (error) {
            SendMessage(gameObjectName, 'OnMultiplayerError', "joinRoom failed: " + error.message);
        }
    },

    // ----------------------------------------------------
    // List currently available rooms
    // ----------------------------------------------------
    VIVERSE_MP_GetAvailableRooms: function () {
        var gameObjectName = this._mpGameObjectName;

        if (!globalThis.matchmakingClient) {
            SendMessage(gameObjectName, 'OnMultiplayerError', "matchmakingClient not initialized");
            return;
        }

        try {
            globalThis.matchmakingClient.getAvailableRooms().then(function (result) {
                // Normalize to a consistent { rooms: [...] } shape regardless
                // of whether the SDK returns a raw array or a wrapped object -
                // makes parsing reliable on the C# side (JsonUtility can't
                // parse a bare top-level JSON array).
                var rooms = Array.isArray(result) ? result : (result.rooms || result.data || []);
                SendMessage(gameObjectName, 'OnAvailableRooms', JSON.stringify({ rooms: rooms }));
            });
        } catch (error) {
            SendMessage(gameObjectName, 'OnMultiplayerError', "getAvailableRooms failed: " + error.message);
        }
    },

    // ----------------------------------------------------
    // Start real-time position sync once inside a room
    // ----------------------------------------------------
    VIVERSE_MP_InitPositionSync: async function (roomIdStr, appIdStr, userSessionIdStr) {
        var roomId = UTF8ToString(roomIdStr);
        var appId = UTF8ToString(appIdStr);
        var userSessionId = userSessionIdStr ? UTF8ToString(userSessionIdStr) : undefined;
        var gameObjectName = this._mpGameObjectName;

        try {
            var options = {
                modules: {
                    networkSync: { enabled: true, desc: 'position sync' }
                }
            };

            globalThis.multiplayerClient = new globalThis.play.MultiplayerClient(roomId, appId, userSessionId);
            var info = await globalThis.multiplayerClient.init(options);

            globalThis.multiplayerClient.onConnected(function () {
                SendMessage(gameObjectName, 'OnMultiplayerConnected', '');
            });

            // These two are "who's online" presence tracking, not required
            // for position sync itself. Guard them individually so that if
            // one is missing/renamed in the current SDK build, it doesn't
            // abort the critical networksync setup below.
            try {
                globalThis.multiplayerClient.onClientConnected(function (userSessionID) {
                    SendMessage(gameObjectName, 'OnPlayerConnected', userSessionID);
                });
            } catch (error) {
                console.warn("[ViverseMultiplayer] onClientConnected not available: " + error.message);
            }

            try {
                globalThis.multiplayerClient.onClientDisconnected(function (userSessionID) {
                    SendMessage(gameObjectName, 'OnPlayerDisconnected', userSessionID);
                });
            } catch (error) {
                console.warn("[ViverseMultiplayer] onClientDisconnected not available: " + error.message);
            }

            globalThis.multiplayerClient.networksync.onNotifyPositionUpdate(function (data) {
                SendMessage(gameObjectName, 'OnPositionUpdate', JSON.stringify(data));
            });

            globalThis.multiplayerClient.networksync.onNotifyRemove(function (data) {
                SendMessage(gameObjectName, 'OnPositionRemove', JSON.stringify(data));
            });

            SendMessage(gameObjectName, 'OnPositionSyncReady', JSON.stringify(info));
        } catch (error) {
            console.error("[ViverseMultiplayer] InitPositionSync failed", error);
            SendMessage(gameObjectName, 'OnMultiplayerError', "InitPositionSync failed: " + error.message);
        }
    },

    // ----------------------------------------------------
    // Send my current position to the server
    // ----------------------------------------------------
    VIVERSE_MP_UpdateMyPosition: function (x, y, z, w) {
        if (!globalThis.multiplayerClient) return;
        try {
            globalThis.multiplayerClient.networksync.updateMyPosition({ x: x, y: y, z: z, w: w });
        } catch (error) {
            console.error("[ViverseMultiplayer] updateMyPosition failed", error);
        }
    }
});
