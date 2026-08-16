using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace UnityMcp.Bridge
{
    /// <summary>
    /// Batch-mode entry point invoked via -executeMethod UnityMcp.Bridge.BatchRunner.Run
    /// </summary>
    public static class BatchRunner
    {
        public static void Run()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            string tempDir = Path.Combine(projectRoot, "Temp", "UnityMcp");
            string requestPath = Path.Combine(tempDir, "request.json");
            string resultPath = Path.Combine(tempDir, "result.json");

            try
            {
                if (!File.Exists(requestPath))
                {
                    File.WriteAllText(resultPath, CommandDispatcher.Fail("Batch.MissingRequest", "Temp/UnityMcp/request.json was not found."));
                    return;
                }

                string requestJson = File.ReadAllText(requestPath);
                string resultJson = CommandDispatcher.Dispatch(requestJson, requireToken: false);
                File.WriteAllText(resultPath, resultJson);
            }
            catch (Exception ex)
            {
                File.WriteAllText(resultPath, CommandDispatcher.Fail("Batch.Exception", ex.Message));
            }
        }
    }
}
