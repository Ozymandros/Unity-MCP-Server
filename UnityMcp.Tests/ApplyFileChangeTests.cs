using System.Text.Json;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System;
using System.IO.Abstractions.TestingHelpers;
using UnityMcp.Infrastructure.Services;
using UnityMcp.Core.Interfaces;

namespace UnityMcp.Tests
{
    public class ApplyFileChangeTests
    {
        [Test]
        public async Task ApplyFileChange_CreateOnly_CreatesFileAndFailsIfExists()
        {
            var fs = new MockFileSystem(new Dictionary<string, MockFileData>());
            var proc = Substitute.For<UnityMcp.Core.Interfaces.IProcessRunner>();
            var logger = Substitute.For<ILogger<FileUnityService>>();

            var svc = new FileUnityService(logger, proc, fs);
            string project = "C:\\fakeproj";
            fs.AddDirectory(project);

            string fileName = "Assets/Scripts/Test.cs";
            string content = "// test";

            string res1 = await svc.ApplyFileChangeAsync(project, fileName, content, IUnityService.AgentEditMode.CreateOnly, CancellationToken.None);
            var doc1 = JsonDocument.Parse(res1);
            Assert.IsTrue(doc1.RootElement.GetProperty("success").GetBoolean());
            Assert.IsTrue(fs.FileExists(PathCombine(project, "Assets/Scripts/Test.cs")));

            // second attempt with CreateOnly should fail
            string res2 = await svc.ApplyFileChangeAsync(project, fileName, "// changed", IUnityService.AgentEditMode.CreateOnly, CancellationToken.None);
            var doc2 = JsonDocument.Parse(res2);
            Assert.IsFalse(doc2.RootElement.GetProperty("success").GetBoolean());
        }

        private static string PathCombine(string a, string b) => (a + "/" + b).Replace('/', System.IO.Path.DirectorySeparatorChar);
    }
}
