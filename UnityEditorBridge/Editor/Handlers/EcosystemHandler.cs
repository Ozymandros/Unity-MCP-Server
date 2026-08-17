using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace UnityMcp.Bridge.Handlers
{
    public static class EcosystemHandler
    {
        public static string ResolvePackages(Dictionary<string, object> args)
        {
            var request = Client.Resolve();
            if (!Wait(request))
                return CommandDispatcher.Fail("Packages.ResolveFailed", request.Error?.message ?? "Package resolve failed.");
            return CommandDispatcher.Ok("Packages resolved.");
        }

        public static string SearchPackages(Dictionary<string, object> args)
        {
            string query = CommandDispatcher.GetString(args, "query");
            var request = Client.Search(query);
            if (!Wait(request))
                return CommandDispatcher.Fail("Packages.SearchFailed", request.Error?.message ?? "Package search failed.");

            var results = (request.Result ?? Array.Empty<UnityEditor.PackageManager.PackageInfo>())
                .Take(25)
                .Select(p => new Dictionary<string, object>
                {
                    ["name"] = p.name,
                    ["displayName"] = p.displayName,
                    ["version"] = p.version,
                    ["description"] = p.description,
                })
                .Cast<object>()
                .ToList();

            return CommandDispatcher.Ok("Package search completed.", new Dictionary<string, object>
            {
                ["results"] = results,
            });
        }

        public static string ConfigureProjectSettings(Dictionary<string, object> args)
        {
            var props = args.ContainsKey("settings") ? args["settings"] as Dictionary<string, object> : args;
            if (props == null)
                return CommandDispatcher.Fail("ProjectSettings.Invalid", "settings object required.");

            if (props.ContainsKey("companyName"))
                PlayerSettings.companyName = Convert.ToString(props["companyName"]);
            if (props.ContainsKey("productName"))
                PlayerSettings.productName = Convert.ToString(props["productName"]);

            if (props.ContainsKey("tags") && props["tags"] is List<object> tags)
            {
                // TagManager is not fully public; write via SerializedObject on ProjectSettings/TagManager.asset
                var tagAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
                if (tagAssets != null && tagAssets.Length > 0)
                {
                    var so = new SerializedObject(tagAssets[0]);
                    var tagsProp = so.FindProperty("tags");
                    if (tagsProp != null && tagsProp.isArray)
                    {
                        tagsProp.arraySize = tags.Count;
                        for (int i = 0; i < tags.Count; i++)
                            tagsProp.GetArrayElementAtIndex(i).stringValue = Convert.ToString(tags[i]);
                        so.ApplyModifiedProperties();
                    }
                }
            }

            AssetDatabase.SaveAssets();
            return CommandDispatcher.Ok("Project settings updated.");
        }

        private static bool Wait(Request request)
        {
            while (!request.IsCompleted)
            {
                System.Threading.Thread.Sleep(50);
            }
            return request.Status == StatusCode.Success;
        }
    }
}
