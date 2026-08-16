namespace UnityMcp.Core.Interfaces;

/// <summary>Asset create/read/move/delete, materials, and typed saves.</summary>
public interface IUnityAssetService
{
    Task CreateScriptAsync(string projectPath, string fileName, string scriptName, string? content = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> ListAssetsAsync(string projectPath, string folderName, string searchPattern = "*", CancellationToken cancellationToken = default);
    Task CreateAssetAsync(string projectPath, string fileName, string content, CancellationToken cancellationToken = default);
    Task CreateMaterialAsync(string projectPath, string fileName, string materialJson, CancellationToken cancellationToken = default);
    Task<string> ReadAssetAsync(string projectPath, string fileName, CancellationToken cancellationToken = default);
    Task DeleteAssetAsync(string projectPath, string fileName, CancellationToken cancellationToken = default);
    Task SaveScriptAsync(string projectPath, string fileName, string content, CancellationToken cancellationToken = default);
    Task SaveTextAssetAsync(string projectPath, string fileName, string content, CancellationToken cancellationToken = default);
    Task SaveTextureAsync(string projectPath, string fileName, string base64Data, CancellationToken cancellationToken = default);
    Task SaveAudioAsync(string projectPath, string fileName, string base64Data, CancellationToken cancellationToken = default);
    Task<string> GetAssetMetadataAsync(string projectPath, string fileName, CancellationToken cancellationToken = default);
    Task<string> ListAssetMetadataAsync(string projectPath, string folderName = "Assets", string searchPattern = "*", CancellationToken cancellationToken = default);
    Task<string> MoveAssetAsync(string projectPath, string sourceFileName, string destinationFileName, CancellationToken cancellationToken = default);
    Task<string> UpdateMaterialPropertiesAsync(string projectPath, string fileName, string propertiesJson, CancellationToken cancellationToken = default);
    Task<string> AssignMaterialTextureAsync(string projectPath, string materialFileName, string textureFileName, string propertyName, CancellationToken cancellationToken = default);
    Task<string> CreateScriptableObjectAsync(string projectPath, string fileName, string typeName, string? propertiesJson = null, CancellationToken cancellationToken = default);
    Task<string> ReadSerializedAssetAsync(string projectPath, string fileName, CancellationToken cancellationToken = default);
    Task<string> UpdateSerializedAssetAsync(string projectPath, string fileName, string propertiesJson, CancellationToken cancellationToken = default);
}
