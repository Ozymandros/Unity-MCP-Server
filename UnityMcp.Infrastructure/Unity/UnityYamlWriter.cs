using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace UnityMcp.Infrastructure.Unity;

/// <summary>
/// Generates valid Unity YAML serialized files (.unity, .prefab, .mat).
/// No Unity DLL dependencies — writes the binary-compatible text format directly.
/// </summary>
public static class UnityYamlWriter
{
    // Unity Class IDs (subset used for scene authoring)
    public const int ClassId_GameObject = 1;
    public const int ClassId_Transform = 4;
    public const int ClassId_Camera = 20;
    public const int ClassId_MeshRenderer = 23;
    public const int ClassId_MeshFilter = 33;
    public const int ClassId_BoxCollider = 65;
    public const int ClassId_Light = 108;
    public const int ClassId_MonoBehaviour = 114;
    public const int ClassId_SphereCollider = 135;
    public const int ClassId_CapsuleCollider = 136;
    public const int ClassId_Rigidbody = 54;
    public const int ClassId_AudioSource = 82;

    private static long _nextFileId = 100;

    /// <summary>
    /// Reset the fileID counter (useful for deterministic tests).
    /// </summary>
    public static void ResetFileIdCounter(long startId = 100)
    {
        _nextFileId = startId;
    }

    /// <summary>
    /// Allocate a unique fileID within the current file.
    /// </summary>
    public static long NextFileId() => _nextFileId++;

    /// <summary>
    /// Write the standard Unity YAML header.
    /// </summary>
    public static string Header()
    {
        return "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n";
    }

    /// <summary>
    /// Write a full scene file with the given GameObjects.
    /// </summary>
    public static string WriteScene(IEnumerable<GameObjectDef> gameObjects)
    {
        var sb = new StringBuilder();
        sb.Append(Header());

        // Scene-level settings
        sb.Append(WriteSceneSettings());

        foreach (var go in gameObjects)
        {
            WriteGameObject(sb, go, 0);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Write a single GameObject definition to append to an existing scene.
    /// Returns just the YAML fragment (no header).
    /// </summary>
    public static string WriteGameObjectFragment(GameObjectDef go)
    {
        var sb = new StringBuilder();
        WriteGameObject(sb, go, 0);
        return sb.ToString();
    }

    /// <summary>
    /// Write a prefab file containing a single root GameObject.
    /// </summary>
    public static string WritePrefab(GameObjectDef rootObject)
    {
        var sb = new StringBuilder();
        sb.Append(Header());
        WriteGameObject(sb, rootObject, 0);
        return sb.ToString();
    }

    /// <summary>
    /// Write a Unity material file (.mat).
    /// </summary>
    public static string WriteMaterial(MaterialDef material)
    {
        var sb = new StringBuilder();
        sb.Append(Header());

        long matId = NextFileId();
        sb.AppendLine($"--- !u!21 &{matId}");
        sb.AppendLine("Material:");
        sb.AppendLine("  serializedVersion: 8");
        sb.AppendLine($"  m_Name: {material.Name}");
        sb.AppendLine("  m_Shader: {fileID: 46, guid: 0000000000000000f000000000000000, type: 0}");
        sb.AppendLine("  m_ValidKeywords: []");
        sb.AppendLine("  m_InvalidKeywords: []");
        sb.AppendLine("  m_LightmapFlags: 4");
        sb.AppendLine("  m_EnableInstancingVariants: 0");
        sb.AppendLine("  m_CustomRenderQueue: -1");

        // Colors
        sb.AppendLine("  m_Colors:");
        sb.AppendLine($"    - _Color: {FormatColor(material.Color)}");
        if (material.EmissionColor != null)
        {
            sb.AppendLine($"    - _EmissionColor: {FormatColor(material.EmissionColor)}");
        }

        // Floats
        sb.AppendLine("  m_Floats:");
        sb.AppendLine($"    - _Metallic: {F(material.Metallic)}");
        sb.AppendLine($"    - _Smoothness: {F(material.Smoothness)}");
        sb.AppendLine($"    - _Mode: {(int)material.RenderMode}");

        sb.AppendLine("  m_TexEnvs: []");

        return sb.ToString();
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private static void WriteGameObject(StringBuilder sb, GameObjectDef go, int depth)
    {
        long goId = NextFileId();
        long transformId = NextFileId();

        // Collect component fileIDs (transform + extras)
        var componentIds = new List<(int classId, long fileId)>
        {
            (ClassId_Transform, transformId)
        };

        // Pre-allocate IDs for all components
        var componentDefinitions = new List<(int classId, long fileId, ComponentDef def)>();
        foreach (var comp in go.Components)
        {
            long compId = NextFileId();
            componentIds.Add((comp.ClassId, compId));
            componentDefinitions.Add((comp.ClassId, compId, comp));
        }

        // --- GameObject ---
        sb.AppendLine($"--- !u!{ClassId_GameObject} &{goId}");
        sb.AppendLine("GameObject:");
        sb.AppendLine("  m_ObjectHideFlags: 0");
        sb.AppendLine("  m_CorrespondingSourceObject: {fileID: 0}");
        sb.AppendLine("  m_PrefabInstance: {fileID: 0}");
        sb.AppendLine("  m_PrefabAsset: {fileID: 0}");
        sb.AppendLine("  serializedVersion: 6");
        sb.AppendLine("  m_Component:");
        foreach (var (classId, fileId) in componentIds)
        {
            sb.AppendLine($"  - component: {{fileID: {fileId}}}");
        }
        sb.AppendLine($"  m_Layer: {go.Layer}");
        sb.AppendLine($"  m_Name: {go.Name}");
        sb.AppendLine($"  m_TagString: {go.Tag}");
        sb.AppendLine($"  m_Icon: {{fileID: 0}}");
        sb.AppendLine($"  m_NavMeshLayer: 0");
        sb.AppendLine($"  m_StaticEditorFlags: 0");
        sb.AppendLine($"  m_IsActive: {(go.IsActive ? 1 : 0)}");

        // --- Transform ---
        sb.AppendLine($"--- !u!{ClassId_Transform} &{transformId}");
        sb.AppendLine("Transform:");
        sb.AppendLine("  m_ObjectHideFlags: 0");
        sb.AppendLine("  m_CorrespondingSourceObject: {fileID: 0}");
        sb.AppendLine("  m_PrefabInstance: {fileID: 0}");
        sb.AppendLine("  m_PrefabAsset: {fileID: 0}");
        sb.AppendLine($"  m_GameObject: {{fileID: {goId}}}");
        sb.AppendLine($"  m_LocalRotation: {FormatQuaternion(go.Rotation)}");
        sb.AppendLine($"  m_LocalPosition: {FormatVector3(go.Position)}");
        sb.AppendLine($"  m_LocalScale: {FormatVector3(go.Scale)}");
        sb.AppendLine("  m_ConstrainProportionsScale: 0");
        sb.AppendLine("  m_Children: []");
        sb.AppendLine("  m_Father: {fileID: 0}");
        sb.AppendLine($"  m_LocalEulerAnglesHint: {FormatVector3(go.EulerAngles)}");

        // --- Extra components ---
        foreach (var (classId, fileId, def) in componentDefinitions)
        {
            sb.AppendLine($"--- !u!{classId} &{fileId}");
            WriteComponentBody(sb, classId, goId, def);
        }
    }

    private static void WriteComponentBody(StringBuilder sb, int classId, long goId, ComponentDef def)
    {
        switch (classId)
        {
            case ClassId_Camera:
                sb.AppendLine("Camera:");
                sb.AppendLine("  m_ObjectHideFlags: 0");
                sb.AppendLine($"  m_GameObject: {{fileID: {goId}}}");
                sb.AppendLine("  m_Enabled: 1");
                sb.AppendLine("  serializedVersion: 2");
                sb.AppendLine($"  m_ClearFlags: {def.GetInt("clearFlags", 1)}");
                sb.AppendLine($"  m_BackGroundColor: {FormatColor(def.GetColor("backgroundColor", new ColorDef(0.192f, 0.302f, 0.475f, 0.02f)))}");
                sb.AppendLine($"  m_projectionMatrixMode: 1");
                sb.AppendLine($"  m_GateFitMode: 2");
                sb.AppendLine($"  field of view: {F(def.GetFloat("fov", 60f))}");
                sb.AppendLine($"  near clip plane: {F(def.GetFloat("nearClip", 0.3f))}");
                sb.AppendLine($"  far clip plane: {F(def.GetFloat("farClip", 1000f))}");
                sb.AppendLine($"  m_Depth: {F(def.GetFloat("depth", -1f))}");
                break;

            case ClassId_Light:
                sb.AppendLine("Light:");
                sb.AppendLine("  m_ObjectHideFlags: 0");
                sb.AppendLine($"  m_GameObject: {{fileID: {goId}}}");
                sb.AppendLine("  m_Enabled: 1");
                sb.AppendLine("  serializedVersion: 10");
                sb.AppendLine($"  m_Type: {def.GetInt("type", 1)}"); // 0=Spot, 1=Directional, 2=Point
                sb.AppendLine($"  m_Color: {FormatColor(def.GetColor("color", new ColorDef(1, 0.957f, 0.839f, 1)))}");
                sb.AppendLine($"  m_Intensity: {F(def.GetFloat("intensity", 1f))}");
                sb.AppendLine($"  m_Range: {F(def.GetFloat("range", 10f))}");
                sb.AppendLine($"  m_Shadows:");
                sb.AppendLine($"    m_Type: {def.GetInt("shadowType", 2)}");
                break;

            case ClassId_MeshFilter:
                sb.AppendLine("MeshFilter:");
                sb.AppendLine("  m_ObjectHideFlags: 0");
                sb.AppendLine($"  m_GameObject: {{fileID: {goId}}}");
                // Primitive mesh references (built-in Unity meshes)
                var meshType = def.GetString("mesh", "Cube");
                var (meshFileId, meshGuid) = GetBuiltInMesh(meshType);
                sb.AppendLine($"  m_Mesh: {{fileID: {meshFileId}, guid: {meshGuid}, type: 0}}");
                break;

            case ClassId_MeshRenderer:
                sb.AppendLine("MeshRenderer:");
                sb.AppendLine("  m_ObjectHideFlags: 0");
                sb.AppendLine($"  m_GameObject: {{fileID: {goId}}}");
                sb.AppendLine("  m_Enabled: 1");
                sb.AppendLine("  m_CastShadows: 1");
                sb.AppendLine("  m_ReceiveShadows: 1");
                sb.AppendLine("  m_Materials:");
                sb.AppendLine("  - {fileID: 10303, guid: 0000000000000000f000000000000000, type: 0}");
                break;

            case ClassId_BoxCollider:
                sb.AppendLine("BoxCollider:");
                sb.AppendLine("  m_ObjectHideFlags: 0");
                sb.AppendLine($"  m_GameObject: {{fileID: {goId}}}");
                sb.AppendLine("  m_Enabled: 1");
                sb.AppendLine($"  m_IsTrigger: {(def.GetBool("isTrigger") ? 1 : 0)}");
                sb.AppendLine($"  m_Size: {FormatVector3(def.GetVector3("size", new Vector3Def(1, 1, 1)))}");
                sb.AppendLine($"  m_Center: {FormatVector3(def.GetVector3("center", new Vector3Def(0, 0, 0)))}");
                break;

            case ClassId_SphereCollider:
                sb.AppendLine("SphereCollider:");
                sb.AppendLine("  m_ObjectHideFlags: 0");
                sb.AppendLine($"  m_GameObject: {{fileID: {goId}}}");
                sb.AppendLine("  m_Enabled: 1");
                sb.AppendLine($"  m_IsTrigger: {(def.GetBool("isTrigger") ? 1 : 0)}");
                sb.AppendLine($"  m_Radius: {F(def.GetFloat("radius", 0.5f))}");
                sb.AppendLine($"  m_Center: {FormatVector3(def.GetVector3("center", new Vector3Def(0, 0, 0)))}");
                break;

            case ClassId_Rigidbody:
                sb.AppendLine("Rigidbody:");
                sb.AppendLine("  m_ObjectHideFlags: 0");
                sb.AppendLine($"  m_GameObject: {{fileID: {goId}}}");
                sb.AppendLine($"  m_Mass: {F(def.GetFloat("mass", 1f))}");
                sb.AppendLine($"  m_Drag: {F(def.GetFloat("drag", 0f))}");
                sb.AppendLine($"  m_AngularDrag: {F(def.GetFloat("angularDrag", 0.05f))}");
                sb.AppendLine($"  m_UseGravity: {(def.GetBool("useGravity", true) ? 1 : 0)}");
                sb.AppendLine($"  m_IsKinematic: {(def.GetBool("isKinematic") ? 1 : 0)}");
                break;

            case ClassId_AudioSource:
                sb.AppendLine("AudioSource:");
                sb.AppendLine("  m_ObjectHideFlags: 0");
                sb.AppendLine($"  m_GameObject: {{fileID: {goId}}}");
                sb.AppendLine("  m_Enabled: 1");
                sb.AppendLine($"  m_PlayOnAwake: {(def.GetBool("playOnAwake", true) ? 1 : 0)}");
                sb.AppendLine($"  m_Volume: {F(def.GetFloat("volume", 1f))}");
                sb.AppendLine($"  m_Loop: {(def.GetBool("loop") ? 1 : 0)}");
                break;

            default:
                // Generic/unknown component - write minimal stub
                sb.AppendLine($"MonoBehaviour:");
                sb.AppendLine("  m_ObjectHideFlags: 0");
                sb.AppendLine($"  m_GameObject: {{fileID: {goId}}}");
                sb.AppendLine("  m_Enabled: 1");
                break;
        }
    }

    private static string WriteSceneSettings()
    {
        var sb = new StringBuilder();

        sb.AppendLine("--- !u!29 &1");
        sb.AppendLine("OcclusionCullingSettings:");
        sb.AppendLine("  m_ObjectHideFlags: 0");
        sb.AppendLine("  serializedVersion: 2");

        sb.AppendLine("--- !u!104 &2");
        sb.AppendLine("RenderSettings:");
        sb.AppendLine("  m_ObjectHideFlags: 0");
        sb.AppendLine("  serializedVersion: 9");
        sb.AppendLine("  m_Fog: 0");
        sb.AppendLine("  m_FogColor: {r: 0.5, g: 0.5, b: 0.5, a: 1}");
        sb.AppendLine("  m_FogMode: 3");
        sb.AppendLine("  m_FogDensity: 0.01");
        sb.AppendLine("  m_LinearFogStart: 0");
        sb.AppendLine("  m_LinearFogEnd: 300");
        sb.AppendLine("  m_AmbientSkyColor: {r: 0.212, g: 0.227, b: 0.259, a: 1}");
        sb.AppendLine("  m_AmbientEquatorColor: {r: 0.114, g: 0.125, b: 0.133, a: 1}");
        sb.AppendLine("  m_AmbientGroundColor: {r: 0.047, g: 0.043, b: 0.035, a: 1}");
        sb.AppendLine("  m_AmbientIntensity: 1");

        sb.AppendLine("--- !u!157 &3");
        sb.AppendLine("LightmapSettings:");
        sb.AppendLine("  m_ObjectHideFlags: 0");
        sb.AppendLine("  serializedVersion: 12");

        sb.AppendLine("--- !u!196 &4");
        sb.AppendLine("NavMeshSettings:");
        sb.AppendLine("  serializedVersion: 2");
        sb.AppendLine("  m_ObjectHideFlags: 0");

        return sb.ToString();
    }

    private static (long fileId, string guid) GetBuiltInMesh(string meshType)
    {
        return meshType.ToLowerInvariant() switch
        {
            "cube" => (10202, "0000000000000000e000000000000000"),
            "sphere" => (10207, "0000000000000000e000000000000000"),
            "capsule" => (10208, "0000000000000000e000000000000000"),
            "cylinder" => (10206, "0000000000000000e000000000000000"),
            "plane" => (10209, "0000000000000000e000000000000000"),
            "quad" => (10210, "0000000000000000e000000000000000"),
            _ => (10202, "0000000000000000e000000000000000") // default to cube
        };
    }

    // Formatting helpers
    private static string F(float v) => v.ToString("G", CultureInfo.InvariantCulture);
    private static string FormatVector3(Vector3Def v) => $"{{x: {F(v.X)}, y: {F(v.Y)}, z: {F(v.Z)}}}";
    private static string FormatQuaternion(QuaternionDef q) => $"{{x: {F(q.X)}, y: {F(q.Y)}, z: {F(q.Z)}, w: {F(q.W)}}}";
    private static string FormatColor(ColorDef c) => $"{{r: {F(c.R)}, g: {F(c.G)}, b: {F(c.B)}, a: {F(c.A)}}}";
}

// -----------------------------------------------------------------------
// Data definitions (used by MCP tools and YAML writer)
// -----------------------------------------------------------------------

public record Vector3Def(float X, float Y, float Z)
{
    public static Vector3Def Zero => new(0, 0, 0);
    public static Vector3Def One => new(1, 1, 1);
}

public record QuaternionDef(float X, float Y, float Z, float W)
{
    public static QuaternionDef Identity => new(0, 0, 0, 1);
}

public record ColorDef(float R, float G, float B, float A = 1f)
{
    public static ColorDef White => new(1, 1, 1, 1);
    public static ColorDef Black => new(0, 0, 0, 1);
}

/// <summary>
/// Defines a component to attach to a GameObject.
/// Properties are stored as a string→object dictionary for flexibility.
/// </summary>
public class ComponentDef
{
    public int ClassId { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();

    public ComponentDef(int classId) => ClassId = classId;

    public float GetFloat(string key, float defaultValue = 0f)
        => Properties.TryGetValue(key, out var v) ? Convert.ToSingle(v, CultureInfo.InvariantCulture) : defaultValue;

    public int GetInt(string key, int defaultValue = 0)
        => Properties.TryGetValue(key, out var v) ? Convert.ToInt32(v, CultureInfo.InvariantCulture) : defaultValue;

    public bool GetBool(string key, bool defaultValue = false)
        => Properties.TryGetValue(key, out var v) ? Convert.ToBoolean(v, CultureInfo.InvariantCulture) : defaultValue;

    public string GetString(string key, string defaultValue = "")
        => Properties.TryGetValue(key, out var v) ? v.ToString() ?? defaultValue : defaultValue;

    public Vector3Def GetVector3(string key, Vector3Def? defaultValue = null)
        => Properties.TryGetValue(key, out var v) && v is Vector3Def vec ? vec : defaultValue ?? Vector3Def.Zero;

    public ColorDef GetColor(string key, ColorDef? defaultValue = null)
        => Properties.TryGetValue(key, out var v) && v is ColorDef col ? col : defaultValue ?? ColorDef.White;
}

/// <summary>
/// Defines a GameObject with transform, components, and metadata.
/// </summary>
public class GameObjectDef
{
    public string Name { get; set; } = "GameObject";
    public string Tag { get; set; } = "Untagged";
    public int Layer { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public Vector3Def Position { get; set; } = Vector3Def.Zero;
    public Vector3Def Scale { get; set; } = Vector3Def.One;
    public QuaternionDef Rotation { get; set; } = QuaternionDef.Identity;
    public Vector3Def EulerAngles { get; set; } = Vector3Def.Zero;
    public List<ComponentDef> Components { get; set; } = new();
}

/// <summary>
/// Defines a Unity material.
/// </summary>
public class MaterialDef
{
    public string Name { get; set; } = "New Material";
    public ColorDef Color { get; set; } = ColorDef.White;
    public ColorDef? EmissionColor { get; set; }
    public float Metallic { get; set; } = 0f;
    public float Smoothness { get; set; } = 0.5f;
    public MaterialRenderMode RenderMode { get; set; } = MaterialRenderMode.Opaque;
}

public enum MaterialRenderMode
{
    Opaque = 0,
    Cutout = 1,
    Fade = 2,
    Transparent = 3
}
