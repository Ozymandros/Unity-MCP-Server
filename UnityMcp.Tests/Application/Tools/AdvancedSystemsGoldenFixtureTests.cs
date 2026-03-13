using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using UnityMcp.Core.Interfaces;
using UnityMcp.Infrastructure.Services;
using System.IO.Abstractions.TestingHelpers;

namespace UnityMcp.Tests.Application.Tools;

[TestFixture]
public class AdvancedSystemsGoldenFixtureTests
{
    private MockFileSystem _mockFs = null!;
    private FileUnityService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _mockFs = new MockFileSystem();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<FileUnityService>>();
        var processRunner = Substitute.For<IProcessRunner>();
        _service = new FileUnityService(logger, processRunner, _mockFs);
    }

    private static string GetFixturePath(string relativePath) =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "AdvancedSystems", relativePath);

    private static void AssertResultMatchesFixture(string actualJson, string fixturePath)
    {
        using var actual = JsonDocument.Parse(actualJson);
        string fixtureJson = File.ReadAllText(fixturePath);
        using var expected = JsonDocument.Parse(fixtureJson);
        var actualRoot = actual.RootElement;
        var expectedRoot = expected.RootElement;
        Assert.That(actualRoot.GetProperty("success").GetBoolean(), Is.EqualTo(expectedRoot.GetProperty("success").GetBoolean()));
        if (expectedRoot.TryGetProperty("path", out var pathExp))
        {
            Assert.That(actualRoot.TryGetProperty("path", out var pathActual), Is.True);
            string expectedPath = pathExp.GetString()!;
            string actualPath = pathActual.GetString() ?? "";
            Assert.That(actualPath.Replace('\\', '/'), Does.EndWith(expectedPath.Replace('\\', '/')),
                "Actual path should end with expected path.");
        }
        if (expectedRoot.TryGetProperty("errors", out var errorsExp) && errorsExp.GetArrayLength() > 0)
        {
            Assert.That(actualRoot.TryGetProperty("errors", out var errorsActual), Is.True);
            Assert.That(errorsActual.GetArrayLength(), Is.GreaterThanOrEqualTo(1));
            string expectedCode = errorsExp[0].GetProperty("code").GetString()!;
            Assert.That(actualJson, Does.Contain(expectedCode));
        }
    }

    [Test]
    public async Task CreateAdvancedAnimatorAsync_ValidDefinition_MatchesGoldenSuccess()
    {
        string proj = await _service.ScaffoldProjectAsync("Golden", @"C:\output");
        const string json = "{\"layers\":[{\"name\":\"Base Layer\",\"defaultState\":\"Idle\",\"states\":[{\"name\":\"Idle\",\"clip\":null}],\"subStateMachines\":[]}]}";
        string result = await _service.CreateAdvancedAnimatorAsync(proj, "Assets/Animations/CharacterAdvanced.animator.json", json);
        AssertResultMatchesFixture(result, GetFixturePath("advanced_animator_success.json"));
    }

    [Test]
    public async Task CreateAdvancedAnimatorAsync_InvalidJson_MatchesGoldenError()
    {
        string proj = await _service.ScaffoldProjectAsync("GoldenInvalid", @"C:\output");
        string result = await _service.CreateAdvancedAnimatorAsync(proj, "Assets/Animations/Bad.animator.json", "{ invalid }");
        AssertResultMatchesFixture(result, GetFixturePath("advanced_animator_invalid_json.json"));
    }

    [Test]
    public async Task CreateAdvancedAnimatorAsync_InvalidStructure_MatchesGoldenError()
    {
        string proj = await _service.ScaffoldProjectAsync("GoldenBadStruct", @"C:\output");
        const string json = "{\"layers\":[{\"name\":\"Base Layer\",\"defaultState\":\"MissingState\",\"states\":[],\"subStateMachines\":[]}]}";
        string result = await _service.CreateAdvancedAnimatorAsync(proj, "Assets/Animations/Bad.animator.json", json);
        AssertResultMatchesFixture(result, GetFixturePath("advanced_animator_invalid_structure.json"));
    }

    [Test]
    public async Task CreateTimelineAsync_ValidDefinition_MatchesGoldenSuccess()
    {
        string proj = await _service.ScaffoldProjectAsync("GoldenTimeline", @"C:\output");
        const string json = "{\"name\":\"IntroCutscene\",\"tracks\":[{\"type\":\"Animation\",\"binding\":\"Player\",\"clips\":[{\"name\":\"IntroPose\",\"clip\":\"Assets/Animations/IntroPose.anim\",\"start\":0.0,\"duration\":2.0}]}]}";
        string result = await _service.CreateTimelineAsync(proj, "Assets/Timelines/IntroCutscene.timeline.json", json);
        AssertResultMatchesFixture(result, GetFixturePath("timeline_success.json"));
    }

    [Test]
    public async Task CreateTimelineAsync_InvalidJson_MatchesGoldenError()
    {
        string proj = await _service.ScaffoldProjectAsync("GoldenTimelineInvalid", @"C:\output");
        string result = await _service.CreateTimelineAsync(proj, "Assets/Timelines/Bad.timeline.json", "{ invalid }");
        AssertResultMatchesFixture(result, GetFixturePath("timeline_invalid_json.json"));
    }

    [Test]
    public async Task CreateVfxAssetAsync_ValidDefinition_MatchesGoldenSuccess()
    {
        string proj = await _service.ScaffoldProjectAsync("GoldenVfx", @"C:\output");
        const string json = "{\"name\":\"ExplosionSmall\",\"duration\":1.0,\"looping\":false,\"startLifetime\":0.5,\"startSpeed\":5.0,\"startSize\":1.0,\"startColor\":{\"r\":1,\"g\":0.6,\"b\":0.1,\"a\":1},\"emission\":{\"rateOverTime\":0,\"bursts\":[{\"time\":0.0,\"count\":50}]},\"shape\":{\"type\":\"Sphere\",\"radius\":0.5}}";
        string result = await _service.CreateVfxAssetAsync(proj, "Assets/VFX/ExplosionSmall.vfx.json", json);
        AssertResultMatchesFixture(result, GetFixturePath("vfx_success.json"));
    }

    [Test]
    public async Task CreateVfxAssetAsync_InvalidJson_MatchesGoldenError()
    {
        string proj = await _service.ScaffoldProjectAsync("GoldenVfxInvalid", @"C:\output");
        string result = await _service.CreateVfxAssetAsync(proj, "Assets/VFX/Bad.vfx.json", "{ invalid }");
        AssertResultMatchesFixture(result, GetFixturePath("vfx_invalid_json.json"));
    }

    [Test]
    public async Task CreateVfxAssetAsync_InvalidParameters_MatchesGoldenError()
    {
        string proj = await _service.ScaffoldProjectAsync("GoldenVfxParams", @"C:\output");
        const string json = "{\"name\":\"Bad\",\"duration\":-1.0,\"looping\":false,\"startLifetime\":0.5,\"startSpeed\":5.0,\"startSize\":1.0,\"startColor\":{\"r\":1,\"g\":0.6,\"b\":0.1,\"a\":1},\"emission\":{\"rateOverTime\":0,\"bursts\":[]},\"shape\":{\"type\":\"Sphere\",\"radius\":0.5}}";
        string result = await _service.CreateVfxAssetAsync(proj, "Assets/VFX/Bad.vfx.json", json);
        AssertResultMatchesFixture(result, GetFixturePath("vfx_invalid_parameters.json"));
    }

    [Test]
    public async Task CreatePhysicsSetupAsync_ValidDefinition_MatchesGoldenSuccess()
    {
        string proj = await _service.ScaffoldProjectAsync("GoldenPhysics", @"C:\output");
        const string json = "{\"name\":\"HumanoidRagdoll\",\"bones\":[{\"name\":\"Hips\",\"colliderType\":\"Capsule\",\"mass\":10.0},{\"name\":\"Spine\",\"colliderType\":\"Box\",\"mass\":8.0}],\"joints\":[]}";
        string result = await _service.CreatePhysicsSetupAsync(proj, "Assets/Physics/HumanoidRagdoll.physics.json", json);
        AssertResultMatchesFixture(result, GetFixturePath("physics_success.json"));
    }

    [Test]
    public async Task CreatePhysicsSetupAsync_InvalidJson_MatchesGoldenError()
    {
        string proj = await _service.ScaffoldProjectAsync("GoldenPhysicsInvalid", @"C:\output");
        string result = await _service.CreatePhysicsSetupAsync(proj, "Assets/Physics/Bad.physics.json", "{ invalid }");
        AssertResultMatchesFixture(result, GetFixturePath("physics_invalid_json.json"));
    }

    [Test]
    public async Task CreatePhysicsSetupAsync_InvalidReference_MatchesGoldenError()
    {
        string proj = await _service.ScaffoldProjectAsync("GoldenPhysicsRef", @"C:\output");
        const string json = "{\"name\":\"Ragdoll\",\"bones\":[{\"name\":\"Hips\",\"colliderType\":\"Capsule\",\"mass\":10.0}],\"joints\":[{\"name\":\"SpineToHead\",\"type\":\"Hinge\",\"bone\":\"Spine\",\"connectedBodyName\":\"Head\"}]}";
        string result = await _service.CreatePhysicsSetupAsync(proj, "Assets/Physics/Ragdoll.physics.json", json);
        AssertResultMatchesFixture(result, GetFixturePath("physics_invalid_reference.json"));
    }
}
