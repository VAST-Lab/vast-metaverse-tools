# VAST Lab Metaverse Toolkit (`com.vastlab.vast-metaverse-tools`)

A core foundation Unity package providing player locomotion, avatar handling, environmental interactables, and metaverse services for VAST Lab projects.

## Engine & Dependencies
- **Engine:** Unity 6 (6000.6+)
- **Key Dependencies:** UniVRM (`com.vrmc.vrm`, `com.vrmc.gltf`), TextMeshPro
- **Integrations:** VIVERSE SDK (Cloud Save, Leaderboards, Identity)

## Major Features & Capabilities
- **Player & Camera Rig:** 3rd-person character controller, customizable camera manager with collision passthrough, jump/movement animations, and mobile/desktop input readers.
- **Avatar System:** Mixamo-ready character rigs and runtime VRM avatar importing with automatic shader repair (`VRMShaderFixer`).
- **Interactive Environment Framework:** Comprehensive interaction system for inspectable objects, item pickups, seating triggers (`SitInteractable`), and spatial dialogue markers.
- **Quest & Progression System:** Objective tracking with modular quest tasks and persistent local/cloud save managers (`PlayerSaveManager`, `SpaceSaveManager`).
- **VIVERSE & UI Tools:** In-game pause/settings menu (volume, sensitivity, fullscreen), currency wallet, shop interfaces, and HTC VIVERSE cloud integrations.

## Installation
Add via Unity Package Manager using Git URL:
```
https://github.com/VAST-Lab/vast-metaverse-tools.git
```
