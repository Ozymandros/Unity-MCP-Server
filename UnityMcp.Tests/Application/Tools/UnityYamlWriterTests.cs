using System.Collections.Generic;
using NUnit.Framework;
using UnityMcp.Infrastructure.Unity;

namespace UnityMcp.Tests.Application.Tools;

[TestFixture]
public class UnityYamlWriterTests
{
    [SetUp]
    public void SetUp()
    {
        UnityYamlWriter.ResetFileIdCounter();
    }

    [Test]
    public void Header_ContainsYamlTag()
    {
        var header = UnityYamlWriter.Header();
        Assert.That(header, Does.Contain("%YAML 1.1"));
        Assert.That(header, Does.Contain("tag:unity3d.com,2011:"));
    }

    [Test]
    public void WriteScene_WithCameraAndLight_ContainsExpectedElements()
    {
        var gos = new List<GameObjectDef>
        {
            new() { Name = "Main Camera", Tag = "MainCamera", Position = new Vector3Def(0, 1, -10),
                     Components = [new ComponentDef(UnityYamlWriter.ClassId_Camera)] },
            new() { Name = "Directional Light", EulerAngles = new Vector3Def(50, -30, 0),
                     Components = [new ComponentDef(UnityYamlWriter.ClassId_Light)] },
        };

        var yaml = UnityYamlWriter.WriteScene(gos);

        Assert.That(yaml, Does.Contain("%YAML 1.1"));
        Assert.That(yaml, Does.Contain("OcclusionCullingSettings"));
        Assert.That(yaml, Does.Contain("RenderSettings"));
        Assert.That(yaml, Does.Contain("m_Name: Main Camera"));
        Assert.That(yaml, Does.Contain("m_TagString: MainCamera"));
        Assert.That(yaml, Does.Contain("m_Name: Directional Light"));
        Assert.That(yaml, Does.Contain("Camera:"));
        Assert.That(yaml, Does.Contain("Light:"));
    }

    [Test]
    public void WriteScene_WithMeshObject_ContainsMeshFilterAndRenderer()
    {
        var gos = new List<GameObjectDef>
        {
            new() { Name = "Cube", Position = new Vector3Def(0, 0.5f, 0),
                     Components = [
                         new ComponentDef(UnityYamlWriter.ClassId_MeshFilter)
                         { Properties = { ["mesh"] = "Cube" } },
                         new ComponentDef(UnityYamlWriter.ClassId_MeshRenderer),
                         new ComponentDef(UnityYamlWriter.ClassId_BoxCollider),
                     ] },
        };

        var yaml = UnityYamlWriter.WriteScene(gos);

        Assert.That(yaml, Does.Contain("MeshFilter:"));
        Assert.That(yaml, Does.Contain("MeshRenderer:"));
        Assert.That(yaml, Does.Contain("BoxCollider:"));
        Assert.That(yaml, Does.Contain("m_Name: Cube"));
    }

    [Test]
    public void WritePrefab_ContainsGameObjectAndTransform()
    {
        var go = new GameObjectDef
        {
            Name = "Enemy",
            Scale = new Vector3Def(2, 2, 2),
            Components = [
                new ComponentDef(UnityYamlWriter.ClassId_Rigidbody)
                { Properties = { ["mass"] = 5f, ["useGravity"] = true } },
            ]
        };

        var yaml = UnityYamlWriter.WritePrefab(go);

        Assert.That(yaml, Does.Contain("%YAML 1.1"));
        Assert.That(yaml, Does.Contain("m_Name: Enemy"));
        Assert.That(yaml, Does.Contain("m_LocalScale: {x: 2, y: 2, z: 2}"));
        Assert.That(yaml, Does.Contain("Rigidbody:"));
        Assert.That(yaml, Does.Contain("m_Mass: 5"));
        Assert.That(yaml, Does.Contain("m_UseGravity: 1"));
    }

    [Test]
    public void WriteMaterial_ContainsShaderAndProperties()
    {
        var mat = new MaterialDef
        {
            Name = "BluePlastic",
            Color = new ColorDef(0, 0.3f, 0.8f, 1),
            Metallic = 0.1f,
            Smoothness = 0.7f,
        };

        var yaml = UnityYamlWriter.WriteMaterial(mat);

        Assert.That(yaml, Does.Contain("%YAML 1.1"));
        Assert.That(yaml, Does.Contain("m_Name: BluePlastic"));
        Assert.That(yaml, Does.Contain("_Color:"));
        Assert.That(yaml, Does.Contain("_Metallic: 0.1"));
        Assert.That(yaml, Does.Contain("_Smoothness: 0.7"));
    }

    [Test]
    public void WriteGameObjectFragment_NoHeader()
    {
        var go = new GameObjectDef { Name = "TestGO" };
        var fragment = UnityYamlWriter.WriteGameObjectFragment(go);

        Assert.That(fragment, Does.Not.Contain("%YAML"));
        Assert.That(fragment, Does.Contain("m_Name: TestGO"));
    }

    [Test]
    public void FileIdCounter_IsSequential()
    {
        var id1 = UnityYamlWriter.NextFileId();
        var id2 = UnityYamlWriter.NextFileId();
        Assert.That(id2, Is.EqualTo(id1 + 1));
    }
}
