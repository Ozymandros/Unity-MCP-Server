using System.ComponentModel;
using ModelContextProtocol.Server;
using UnityMcp.Core.Interfaces;

namespace UnityMcp.Application.Tools;

/// <summary>VFX/particles and physics setups.</summary>
public static partial class UnityTools
{
    [McpServerTool(Name = "unity_create_physics_setup"), Description(
        "Creates a physics setup asset (ragdoll/joint rig) from JSON. " +
        "See PhysicsContracts.RagdollSetupContract. Returns JSON: success, path, message, errors.")]
    public static async Task<string> CreatePhysicsSetup(
        IUnityService unityService,
        [Description("Project root path")]
        string projectPath,
        [Description("Physics asset path, e.g. Assets/Physics/HumanoidRagdoll.physics.json")]
        string fileName,
        [Description("Physics setup JSON (RagdollSetupContract schema)")]
        string physicsJson,
        CancellationToken cancellationToken = default)
    {
        return await unityService.CreatePhysicsSetupAsync(projectPath, fileName, physicsJson, cancellationToken);
    }

    [McpServerTool(Name = "unity_create_vfx_asset"), Description(
        "Creates a particle VFX asset from JSON (ParticleEffectContract schema). " +
        "Returns JSON: success, path, message, errors (validation failures use Vfx.InvalidJson / Vfx.InvalidParameters codes).")]
    public static async Task<string> CreateVfxAsset(
        IUnityService unityService,
        [Description("Project root path")]
        string projectPath,
        [Description("VFX asset path, e.g. Assets/VFX/ExplosionSmall.vfx.json")]
        string fileName,
        [Description("VFX JSON (ParticleEffectContract schema: name, duration, looping, startLifetime/Speed/Size, startColor, emission, shape)")]
        string vfxJson,
        CancellationToken cancellationToken = default)
    {
        return await unityService.CreateVfxAssetAsync(projectPath, fileName, vfxJson, cancellationToken);
    }
}
