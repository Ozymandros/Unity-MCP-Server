namespace UnityMcp.Core.Interfaces;

/// <summary>Scene and prefab graph operations.</summary>
public interface IUnitySceneService
{
    Task CreateSceneAsync(string projectPath, string fileName, CancellationToken cancellationToken = default);
    Task CreateGameObjectAsync(string projectPath, string fileName, string gameObjectName, CancellationToken cancellationToken = default);
    Task CreateDetailedSceneAsync(string projectPath, string fileName, string sceneJson, CancellationToken cancellationToken = default);
    Task AddGameObjectToSceneAsync(string projectPath, string fileName, string gameObjectJson, CancellationToken cancellationToken = default);
    Task CreatePrefabAsync(string projectPath, string fileName, string prefabJson, CancellationToken cancellationToken = default);
    Task<string> ListSceneObjectsAsync(string projectPath, string fileName, CancellationToken cancellationToken = default);
    Task<string> RenameSceneObjectAsync(string projectPath, string fileName, string objectPath, string newName, CancellationToken cancellationToken = default);
    Task<string> RemoveSceneObjectAsync(string projectPath, string fileName, string objectPath, CancellationToken cancellationToken = default);
    Task<string> DiffSceneFilesAsync(string projectPath, string fileNameA, string fileNameB, CancellationToken cancellationToken = default);
    Task<string> AttachScriptAsync(string projectPath, string fileName, string objectPath, string scriptFileName, CancellationToken cancellationToken = default);
    Task<string> InstantiatePrefabAsync(string projectPath, string sceneFileName, string prefabFileName, string instanceName, CancellationToken cancellationToken = default);
    Task<string> SaveObjectAsPrefabAsync(string projectPath, string sceneFileName, string objectPath, string prefabFileName, CancellationToken cancellationToken = default);
    Task<string> CreateDefaultSceneAsync(string projectPath, string sceneName, CancellationToken cancellationToken = default);

    // Native hierarchy ops (Editor-backed when available)
    Task<string> AddSceneGameObjectAsync(string projectPath, string fileName, string parentPath, string objectName, CancellationToken cancellationToken = default);
    Task<string> ReparentSceneGameObjectAsync(string projectPath, string fileName, string objectPath, string newParentPath, CancellationToken cancellationToken = default);
    Task<string> GetSceneObjectPropertiesAsync(string projectPath, string fileName, string objectPath, CancellationToken cancellationToken = default);
    Task<string> SetSceneObjectPropertiesAsync(string projectPath, string fileName, string objectPath, string propertiesJson, CancellationToken cancellationToken = default);
    Task<string> SetSceneObjectActiveAsync(string projectPath, string fileName, string objectPath, bool active, CancellationToken cancellationToken = default);
    Task<string> ListComponentsAsync(string projectPath, string fileName, string objectPath, CancellationToken cancellationToken = default);
    Task<string> AddComponentAsync(string projectPath, string fileName, string objectPath, string componentType, CancellationToken cancellationToken = default);
    Task<string> RemoveComponentAsync(string projectPath, string fileName, string objectPath, string componentType, int componentIndex = 0, CancellationToken cancellationToken = default);
    Task<string> GetComponentPropertiesAsync(string projectPath, string fileName, string objectPath, string componentType, int componentIndex = 0, CancellationToken cancellationToken = default);
    Task<string> SetComponentPropertiesAsync(string projectPath, string fileName, string objectPath, string componentType, string propertiesJson, int componentIndex = 0, CancellationToken cancellationToken = default);
    Task<string> SetComponentEnabledAsync(string projectPath, string fileName, string objectPath, string componentType, bool enabled, int componentIndex = 0, CancellationToken cancellationToken = default);
}
