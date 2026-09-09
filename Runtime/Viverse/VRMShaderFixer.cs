using UnityEngine;
using VastMetaverseTools.Runtime.Avatars;

/// <summary>
/// Fixes VRM avatars appearing invisible/magenta by swapping their materials
/// from the Built-in Render Pipeline MToon10 shader (VRM10/MToon10) to the
/// URP-specific version (VRM10/Universal Render Pipeline/MToon10) once the
/// avatar finishes loading. VRM materials are generated dynamically at
/// runtime, so this has to run in code rather than being fixed by hand.
/// </summary>
public class VRMShaderFixer : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag the ViverseAvatarController object (with AvatarManager) here")]
    public AvatarManager avatarManager;

    [Header("Shader Settings")]
    [Tooltip("The Built-in Render Pipeline shader that needs to be replaced")]
    public string sourceShaderName = "VRM10/MToon10";

    [Tooltip("The URP shader to replace it with")]
    public string targetShaderName = "VRM10/Universal Render Pipeline/MToon10";

    private void OnEnable()
    {
        if (avatarManager != null)
        {
            avatarManager.OnAvatarLoaded += HandleAvatarLoaded;
        }
        else
        {
            Debug.LogError("[VRMShaderFixer] AvatarManager reference is not set in the Inspector.");
        }
    }

    private void OnDisable()
    {
        if (avatarManager != null)
        {
            avatarManager.OnAvatarLoaded -= HandleAvatarLoaded;
        }
    }

    private void HandleAvatarLoaded(GameObject avatar)
    {
        Debug.Log("[VRMShaderFixer] Avatar loaded, checking materials for shader fix: " + avatar.name);

        Shader targetShader = Shader.Find(targetShaderName);
        if (targetShader == null)
        {
            Debug.LogError($"[VRMShaderFixer] Could not find target shader '{targetShaderName}'. " +
                            "Make sure it's added to Always Included Shaders in Project Settings > Graphics.");
            return;
        }

        // Check every Renderer in the avatar's hierarchy (SkinnedMeshRenderer, MeshRenderer, etc.)
        Renderer[] renderers = avatar.GetComponentsInChildren<Renderer>(true);
        int fixedCount = 0;

        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.materials; // instances, safe to modify at runtime

            for (int i = 0; i < materials.Length; i++)
            {
                Material mat = materials[i];
                if (mat == null || mat.shader == null) continue;

                if (mat.shader.name == sourceShaderName)
                {
                    Debug.Log($"[VRMShaderFixer] Swapping shader on material '{mat.name}' " +
                              $"(renderer: {renderer.gameObject.name})");
                    mat.shader = targetShader;
                    fixedCount++;
                }
            }

            renderer.materials = materials;
        }

        Debug.Log($"[VRMShaderFixer] Fixed {fixedCount} material(s) on '{avatar.name}'.");
    }
}