using System.ComponentModel;
using ModelContextProtocol.Server;
using UnityMcp.Core.Interfaces;

namespace UnityMcp.Application.Tools;

/// <summary>Asset metadata, movement, linting, and material-resource tools.</summary>
public static partial class UnityTools
{
    [McpServerTool(Name = "unity_get_asset_metadata"), Description("Return asset metadata including type, GUID, .meta status, and GUID dependencies.")]
    public static Task<string> GetAssetMetadata(
        IUnityService unityService,
        [Description("Project root path")] string projectPath,
        [Description("Asset file name/path under the project")] string fileName,
        CancellationToken cancellationToken = default)
        => unityService.GetAssetMetadataAsync(projectPath, fileName, cancellationToken);

    [McpServerTool(Name = "unity_list_asset_metadata"), Description("List assets under a folder with metadata and GUID dependency information.")]
    public static Task<string> ListAssetMetadata(
        IUnityService unityService,
        [Description("Project root path")] string projectPath,
        [Description("Folder name/path under the project")] string folderName = "Assets",
        [Description("Search pattern")] string pattern = "*",
        CancellationToken cancellationToken = default)
        => unityService.ListAssetMetadataAsync(projectPath, folderName, pattern, cancellationToken);

    [McpServerTool(Name = "unity_move_asset"), Description("Move or rename an asset while preserving its .meta sidecar.")]
    public static Task<string> MoveAsset(
        IUnityService unityService,
        [Description("Project root path")] string projectPath,
        [Description("Source asset file name/path")] string sourceFileName,
        [Description("Destination asset file name/path")] string destinationFileName,
        CancellationToken cancellationToken = default)
        => unityService.MoveAssetAsync(projectPath, sourceFileName, destinationFileName, cancellationToken);

    [McpServerTool(Name = "unity_update_material_properties"), Description("Update selected material YAML properties such as name, color, metallic, or smoothness.")]
    public static Task<string> UpdateMaterialProperties(
        IUnityService unityService,
        [Description("Project root path")] string projectPath,
        [Description("Material file name/path")] string fileName,
        [Description("JSON object containing properties to update")] string propertiesJson,
        CancellationToken cancellationToken = default)
        => unityService.UpdateMaterialPropertiesAsync(projectPath, fileName, propertiesJson, cancellationToken);

    [McpServerTool(Name = "unity_assign_material_texture"), Description("Assign a texture GUID reference to a material texture property.")]
    public static Task<string> AssignMaterialTexture(
        IUnityService unityService,
        [Description("Project root path")] string projectPath,
        [Description("Material file name/path")] string materialFileName,
        [Description("Texture file name/path")] string textureFileName,
        [Description("Material property name, e.g. _MainTex")] string propertyName = "_MainTex",
        CancellationToken cancellationToken = default)
        => unityService.AssignMaterialTextureAsync(projectPath, materialFileName, textureFileName, propertyName, cancellationToken);

    [McpServerTool(Name = "unity_lint_project"), Description("Run static Unity project lint checks for missing .meta files, broken GUID references, and missing scripts.")]
    public static Task<string> LintProject(
        IUnityService unityService,
        [Description("Project root path")] string projectPath,
        CancellationToken cancellationToken = default)
        => unityService.LintProjectAsync(projectPath, cancellationToken);
}
