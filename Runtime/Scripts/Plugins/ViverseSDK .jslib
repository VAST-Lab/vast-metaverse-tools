mergeInto(LibraryManager.library, {
    
    _clientConfig: null,
    _gameObjectName: null, // 
    
    // ----------------------------------------------------
    // New: Function to dynamically load the VIVERSE SDK
    // ----------------------------------------------------
    VIVERSE_LoadSDK: function (gameObjectNameStr) {
        var gameObjectName = UTF8ToString(gameObjectNameStr);
        this._gameObjectName = UTF8ToString(gameObjectNameStr);
        var script = document.createElement('script');
        console.log("gameObjectName " + gameObjectName);

        if (globalThis.viverse) {
            console.log("VIVERSE SDK already loaded.");
            return;
        }

        console.log("Dynamically loading VIVERSE SDK...");


        script.onload = function() {
            console.log("VIVERSE SDK loaded successfully. ");
            SendMessage(gameObjectName, 'OnSDKLoaded', ''); 
        };

        script.onerror = function() {
            console.error("Failed to load VIVERSE SDK.");
            SendMessage(gameObjectName, 'OnSDKLoadFailed', '');
        };

        
        script.type = 'text/javascript';
        script.defer = true;
        script.src = "https://www.viverse.com/static-assets/viverse-sdk/1.3.2/index.umd.cjs";
   

        // Append the script tag to the HTML <head>
        document.head.appendChild(script);
    },
    
    // ----------------------------------------------------
    // Initialize the SDK Client (Modified to accept Game Object Name)
    // ----------------------------------------------------
    VIVERSE_InitializeClient: function (clientIdStr, domainStr, cookieDomainStr, gameObjectNameStr) {
        // Convert C# strings to JavaScript strings
        var clientId = UTF8ToString(clientIdStr);
        var domain = UTF8ToString(domainStr);
        // Handle cookieDomain - if empty string from C#, set as null/undefined
        var cookieDomain = cookieDomainStr && UTF8ToString(cookieDomainStr).length > 0 ? UTF8ToString(cookieDomainStr) : null;
        

        console.log("VIVERSE SDK: Initializing client with ID:", clientId);
        console.log("VIVERSE SDK: Domain:", domain);
        console.log("VIVERSE SDK: Cookie Domain:", cookieDomain || "null (using current subdomain)");
        console.log("VIVERSE SDK: Callbacks directed to Game Object:", this._gameObjectName); // Log the dynamic name

        // Store client config globally
        var clientConfig = {
            clientId: clientId,
            domain: domain
        };
        
        if (cookieDomain) {
            clientConfig.cookieDomain = cookieDomain;
        }
        
        // Store config for later use
        this._clientConfig = clientConfig;

        // Initialize the client
        globalThis.viverseClient = new globalThis.viverse.client(clientConfig);

        // NOTE: The redundant window.addEventListener('load', ...) logic has been removed here.
        // C# will now immediately call VIVERSE_CheckAuth() after this function completes.
    },

    // ----------------------------------------------------
    // Trigger Login via VIVERSE Worlds
    // ----------------------------------------------------
    VIVERSE_LoginWithWorlds: function (stateStr) {
        if (!globalThis.viverseClient) {
             console.error("VIVERSE SDK: Client not initialized for login. Call VIVERSE_InitializeClient first.");
             
             SendMessage(this._gameObjectName, 'HandleLoginFailure', "Client not initialized for login");
             return;
        }
        
        var state = stateStr ? UTF8ToString(stateStr) : undefined;
        var options = state ? { state: state } : {};

        console.log("VIVERSE SDK: Requesting login with state:", state);
        
        // Trigger the login redirect
        globalThis.viverseClient.loginWithWorlds(options);
    },

    // ----------------------------------------------------
    // Check for Existing Authentication (Optional)
    // ----------------------------------------------------
    VIVERSE_CheckAuth: async function() {
        if (!globalThis.viverseClient) {
            console.error("VIVERSE SDK: Client not initialized. Call VIVERSE_InitializeClient first.");
           
            SendMessage(this._gameObjectName, 'HandleLoginFailure', "Client not initialized");
            return;
        }
        
        try {
            const result = await globalThis.viverseClient.checkAuth();
            if (result) {
                console.log("VIVERSE SDK: Auth check successful", result);
                var resultJson = JSON.stringify(result);
               
                SendMessage(this._gameObjectName, 'HandleLoginSuccess', resultJson);
            } else {
                console.log("VIVERSE SDK: No existing token found");
              
                SendMessage(this._gameObjectName, 'HandleLoginFailure', "No existing token found");
            }
        } catch (error) {
            console.error("VIVERSE SDK: Error checking auth", error);
            SendMessage(this._gameObjectName, 'HandleLoginFailure', "Auth check error: " + error.message);
        }
    }
});

