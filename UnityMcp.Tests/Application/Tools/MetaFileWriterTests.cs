using System.Threading.Tasks;
using System.IO.Abstractions.TestingHelpers;
using NUnit.Framework;
using UnityMcp.Infrastructure.Services;
using UnityMcp.Infrastructure.Unity;

namespace UnityMcp.Tests.Application.Tools;

[TestFixture]
public class MetaFileWriterTests
{
    private MockFileSystem _mockFs = null!;
    private MetaFileWriter _writer = null!;

    [SetUp]
    public void SetUp()
    {
        _mockFs = new MockFileSystem();
        _mockFs.Directory.CreateDirectory(@"C:\project");
        _writer = new MetaFileWriter(_mockFs);
    }

    [Test]
    public async Task WriteScriptMeta_CreatesValidMetaFile()
    {
        string scriptPath = @"C:\project\Test.cs";
        _mockFs.File.WriteAllText(scriptPath, "class Test {}");
        await _writer.WriteScriptMetaAsync(scriptPath);

        string metaPath = scriptPath + ".meta";
        Assert.That(_mockFs.File.Exists(metaPath), Is.True);
        string content = _mockFs.File.ReadAllText(metaPath);
        Assert.That(content, Does.Contain("MonoImporter:"));
        Assert.That(content, Does.Contain("fileFormatVersion: 2"));
        Assert.That(content, Does.Contain("guid:"));
    }

    [Test]
    public async Task WriteDefaultMeta_CreatesValidMetaFile()
    {
        string txtPath = @"C:\project\readme.txt";
        _mockFs.File.WriteAllText(txtPath, "hello");
        await _writer.WriteDefaultMetaAsync(txtPath);

        string content = _mockFs.File.ReadAllText(txtPath + ".meta");
        Assert.That(content, Does.Contain("DefaultImporter:"));
    }

    [Test]
    public async Task WriteTextureMeta_ContainsTextureImporter()
    {
        string imgPath = @"C:\project\img.png";
        _mockFs.File.WriteAllBytes(imgPath, new byte[] { 0x89, 0x50, 0x4E, 0x47 });
        await _writer.WriteTextureMetaAsync(imgPath);

        string content = _mockFs.File.ReadAllText(imgPath + ".meta");
        Assert.That(content, Does.Contain("TextureImporter:"));
        Assert.That(content, Does.Contain("maxTextureSize: 2048"));
    }

    [Test]
    public async Task WriteAudioMeta_ContainsAudioImporter()
    {
        string audioPath = @"C:\project\clip.mp3";
        _mockFs.File.WriteAllBytes(audioPath, new byte[] { 0xFF, 0xFB });
        await _writer.WriteAudioMetaAsync(audioPath);

        string content = _mockFs.File.ReadAllText(audioPath + ".meta");
        Assert.That(content, Does.Contain("AudioImporter:"));
        Assert.That(content, Does.Contain("sampleRateOverride: 44100"));
    }

    [Test]
    public async Task WriteFolderMeta_ContainsFolderAsset()
    {
        string folder = @"C:\project\MyFolder";
        _mockFs.Directory.CreateDirectory(folder);
        await _writer.WriteFolderMetaAsync(folder);

        string content = _mockFs.File.ReadAllText(folder + ".meta");
        Assert.That(content, Does.Contain("folderAsset: yes"));
        Assert.That(content, Does.Contain("DefaultImporter:"));
    }

    [Test]
    public void NewGuid_Returns32HexChars()
    {
        string guid = MetaFileWriter.NewGuid();
        Assert.That(guid.Length, Is.EqualTo(32));
        Assert.That(guid, Does.Match("^[0-9a-f]{32}$"));
    }
}
