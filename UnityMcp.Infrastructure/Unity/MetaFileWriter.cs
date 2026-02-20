using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace UnityMcp.Infrastructure.Unity;

/// <summary>
/// Generates Unity .meta sidecar files for every asset type.
/// Unity requires a .meta file next to every asset (file or folder) to track
/// GUIDs and importer settings. Without these, Unity regenerates them on import
/// and cross-references break.
/// </summary>
public static class MetaFileWriter
{
    /// <summary>
    /// Write a DefaultImporter .meta sidecar (used for generic text files, JSON, etc.).
    /// </summary>
    public static async Task WriteDefaultMetaAsync(string assetPath, string? guid = null, CancellationToken ct = default)
    {
        string content = $@"fileFormatVersion: 2
guid: {guid ?? NewGuid()}
DefaultImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
";
        await File.WriteAllTextAsync(assetPath + ".meta", content, ct);
    }

    /// <summary>
    /// Write a MonoImporter .meta sidecar (used for C# scripts).
    /// </summary>
    public static async Task WriteScriptMetaAsync(string assetPath, string? guid = null, CancellationToken ct = default)
    {
        string content = $@"fileFormatVersion: 2
guid: {guid ?? NewGuid()}
MonoImporter:
  externalObjects: {{}}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {{instanceID: 0}}
  userData:
  assetBundleName:
  assetBundleVariant:
";
        await File.WriteAllTextAsync(assetPath + ".meta", content, ct);
    }

    /// <summary>
    /// Write a TextureImporter .meta sidecar (used for .png, .jpg, .tga, etc.).
    /// </summary>
    public static async Task WriteTextureMetaAsync(string assetPath, string? guid = null, CancellationToken ct = default)
    {
        string content = $@"fileFormatVersion: 2
guid: {guid ?? NewGuid()}
TextureImporter:
  fileIDToRecycleName: {{}}
  externalObjects: {{}}
  serializedVersion: 12
  mipmaps:
    mipMapMode: 0
    enableMipMap: 1
    sRGBTexture: 1
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  textureFormat: 1
  maxTextureSize: 2048
  textureCompression: 1
  compressionQuality: 50
  spriteMode: 0
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaIsTransparency: 1
  platformSettings: []
  userData:
  assetBundleName:
  assetBundleVariant:
";
        await File.WriteAllTextAsync(assetPath + ".meta", content, ct);
    }

    /// <summary>
    /// Write an AudioImporter .meta sidecar (used for .mp3, .wav, .ogg, etc.).
    /// </summary>
    public static async Task WriteAudioMetaAsync(string assetPath, string? guid = null, CancellationToken ct = default)
    {
        string content = $@"fileFormatVersion: 2
guid: {guid ?? NewGuid()}
AudioImporter:
  externalObjects: {{}}
  serializedVersion: 2
  defaultSettings:
    loadType: 0
    sampleRateSetting: 0
    sampleRateOverride: 44100
    compressionFormat: 1
    quality: 1
    conversionMode: 0
  platformSettings: []
  userData:
  assetBundleName:
  assetBundleVariant:
";
        await File.WriteAllTextAsync(assetPath + ".meta", content, ct);
    }

    /// <summary>
    /// Write a folder .meta sidecar.
    /// </summary>
    public static async Task WriteFolderMetaAsync(string folderPath, string? guid = null, CancellationToken ct = default)
    {
        string content = $@"fileFormatVersion: 2
guid: {guid ?? NewGuid()}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
";
        await File.WriteAllTextAsync(folderPath + ".meta", content, ct);
    }

    /// <summary>
    /// Generate a deterministic-looking GUID (32 hex chars, no dashes).
    /// </summary>
    public static string NewGuid() => Guid.NewGuid().ToString("N");
}
