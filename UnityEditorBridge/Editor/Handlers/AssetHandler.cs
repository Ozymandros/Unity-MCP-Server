using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityMcp.Bridge.Handlers
{
    public static class AssetHandler
    {
        public static string GetMetadata(Dictionary<string, object> args)
        {
            string path = CommandDispatcher.GetString(args, "fileName");
            if (!File.Exists(path) && !AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path))
                return CommandDispatcher.Fail("Asset.NotFound", "Asset not found: " + path);

            string guid = AssetDatabase.AssetPathToGUID(path);
            var deps = AssetDatabase.GetDependencies(path, false)
                .Where(d => !string.Equals(d, path, StringComparison.OrdinalIgnoreCase))
                .ToList();
            var type = AssetDatabase.GetMainAssetTypeAtPath(path);

            return CommandDispatcher.Ok("Asset metadata read.", new Dictionary<string, object>
            {
                ["path"] = path,
                ["guid"] = guid,
                ["type"] = type?.Name ?? "Unknown",
                ["hasMeta"] = File.Exists(path + ".meta"),
                ["dependencies"] = deps,
            });
        }

        public static string Move(Dictionary<string, object> args)
        {
            string source = CommandDispatcher.GetString(args, "sourceFileName");
            string destination = CommandDispatcher.GetString(args, "destinationFileName");
            string error = AssetDatabase.MoveAsset(source, destination);
            if (!string.IsNullOrEmpty(error))
                return CommandDispatcher.Fail("Asset.MoveFailed", error);
            AssetDatabase.SaveAssets();
            return CommandDispatcher.Ok("Asset moved.", new Dictionary<string, object>
            {
                ["source"] = source,
                ["destination"] = destination,
            });
        }

        public static string Lint(Dictionary<string, object> args)
        {
            var issues = new List<object>();
            string[] all = AssetDatabase.GetAllAssetPaths()
                .Where(p => p.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            var namesByParent = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (string path in all)
            {
                if (path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                    continue;

                string meta = path + ".meta";
                if (File.Exists(Path.GetFullPath(path)) && !File.Exists(meta) && !Directory.Exists(path))
                {
                    issues.Add(Issue("Lint.MissingMeta", "Missing .meta for " + path, "Reimport the asset or regenerate the .meta sidecar."));
                }

                string guid = AssetDatabase.AssetPathToGUID(path);
                if (string.IsNullOrEmpty(guid) && File.Exists(path))
                    issues.Add(Issue("Lint.MissingGuid", "Asset has no GUID: " + path));

                foreach (string dep in AssetDatabase.GetDependencies(path, false))
                {
                    if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(dep)) && dep.StartsWith("Assets/"))
                        issues.Add(Issue("Lint.BrokenDependency", path + " references missing asset " + dep));
                }

                string parent = Path.GetDirectoryName(path)?.Replace('\\', '/') ?? "";
                string name = Path.GetFileName(path);
                if (!namesByParent.TryGetValue(parent, out var set))
                {
                    set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    namesByParent[parent] = set;
                }
                else if (!set.Add(name))
                {
                    issues.Add(Issue("Lint.DuplicateSiblingName", "Duplicate asset name under " + parent + ": " + name));
                }
            }

            var scenes = EditorBuildSettings.scenes;
            if (scenes == null || scenes.Length == 0)
                issues.Add(Issue("Lint.NoBuildScenes", "No scenes in build settings.", "Add at least one enabled scene via ProjectSettings."));

            return CommandDispatcher.Ok("Project lint completed.", new Dictionary<string, object>
            {
                ["issueCount"] = issues.Count,
                ["issues"] = issues,
            });
        }

        public static string UpdateMaterial(Dictionary<string, object> args)
        {
            string path = CommandDispatcher.GetString(args, "fileName");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) return CommandDispatcher.Fail("Material.NotFound", "Material not found: " + path);
            var props = args.ContainsKey("properties") ? args["properties"] as Dictionary<string, object> : null;
            if (props == null) return CommandDispatcher.Fail("Material.InvalidProperties", "properties required.");

            if (props.ContainsKey("color") || props.ContainsKey("_Color"))
            {
                var c = props.ContainsKey("color") ? props["color"] : props["_Color"];
                if (c is Dictionary<string, object> cd)
                {
                    mat.color = new Color(
                        Convert.ToSingle(cd.GetValueOrDefault("r", 1f)),
                        Convert.ToSingle(cd.GetValueOrDefault("g", 1f)),
                        Convert.ToSingle(cd.GetValueOrDefault("b", 1f)),
                        Convert.ToSingle(cd.GetValueOrDefault("a", 1f)));
                }
            }
            if (props.ContainsKey("metallic")) mat.SetFloat("_Metallic", Convert.ToSingle(props["metallic"]));
            if (props.ContainsKey("smoothness")) mat.SetFloat("_Glossiness", Convert.ToSingle(props["smoothness"]));
            if (props.ContainsKey("texturePath") || props.ContainsKey("_MainTex"))
            {
                string texPath = Convert.ToString(props.GetValueOrDefault("texturePath", props.GetValueOrDefault("_MainTex", "")));
                if (!string.IsNullOrWhiteSpace(texPath))
                {
                    var tex = AssetDatabase.LoadAssetAtPath<Texture>(texPath);
                    if (tex != null) mat.SetTexture("_MainTex", tex);
                }
            }

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return CommandDispatcher.Ok("Material updated.", new Dictionary<string, object> { ["path"] = path });
        }

        public static string CreateScriptableObject(Dictionary<string, object> args)
        {
            string path = CommandDispatcher.GetString(args, "fileName");
            string typeName = CommandDispatcher.GetString(args, "typeName");
            Type type = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = asm.GetType(typeName) ?? asm.GetTypes().FirstOrDefault(t => t.Name == typeName);
                if (type != null) break;
            }
            if (type == null || !typeof(ScriptableObject).IsAssignableFrom(type))
                return CommandDispatcher.Fail("ScriptableObject.UnknownType", "Type not found: " + typeName);

            EnsureFolder(path);
            var asset = ScriptableObject.CreateInstance(type);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            return CommandDispatcher.Ok("ScriptableObject created.", new Dictionary<string, object> { ["path"] = path, ["type"] = type.Name });
        }

        public static string ReadSerialized(Dictionary<string, object> args)
        {
            string path = CommandDispatcher.GetString(args, "fileName");
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (asset == null) return CommandDispatcher.Fail("Asset.NotFound", "Asset not found: " + path);
            var so = new SerializedObject(asset);
            var props = new Dictionary<string, object>();
            var it = so.GetIterator();
            bool enter = true;
            while (it.NextVisible(enter))
            {
                enter = false;
                props[it.propertyPath] = it.propertyType.ToString();
            }
            return CommandDispatcher.Ok("Serialized asset read.", props);
        }

        public static string UpdateSerialized(Dictionary<string, object> args)
        {
            string path = CommandDispatcher.GetString(args, "fileName");
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (asset == null) return CommandDispatcher.Fail("Asset.NotFound", "Asset not found: " + path);
            var props = args.ContainsKey("properties") ? args["properties"] as Dictionary<string, object> : null;
            if (props == null) return CommandDispatcher.Fail("Asset.InvalidProperties", "properties required.");
            var so = new SerializedObject(asset);
            foreach (var kv in props)
            {
                var p = so.FindProperty(kv.Key);
                if (p == null) continue;
                if (p.propertyType == SerializedPropertyType.String) p.stringValue = Convert.ToString(kv.Value);
                else if (p.propertyType == SerializedPropertyType.Integer) p.intValue = Convert.ToInt32(kv.Value);
                else if (p.propertyType == SerializedPropertyType.Float) p.floatValue = Convert.ToSingle(kv.Value);
                else if (p.propertyType == SerializedPropertyType.Boolean) p.boolValue = Convert.ToBoolean(kv.Value);
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return CommandDispatcher.Ok("Serialized asset updated.", new Dictionary<string, object> { ["path"] = path });
        }

        private static Dictionary<string, object> Issue(string code, string message, string remediation = null) =>
            new()
            {
                ["category"] = "Validation",
                ["code"] = code,
                ["message"] = message,
                ["suggestedRemediation"] = remediation,
            };

        private static void EnsureFolder(string assetPath)
        {
            string dir = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(dir) || AssetDatabase.IsValidFolder(dir)) return;
            string[] parts = dir.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }

    internal static class DictExt
    {
        public static object GetValueOrDefault(this Dictionary<string, object> dict, string key, object defaultValue)
            => dict != null && dict.TryGetValue(key, out var v) ? v : defaultValue;
    }
}
