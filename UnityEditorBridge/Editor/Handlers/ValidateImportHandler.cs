using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace UnityMcp.Bridge.Handlers
{
    public static class ValidateImportHandler
    {
        public static string Execute(Dictionary<string, object> args)
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            var errors = new List<object>();
            var warnings = new List<object>();

            // Preferred path: reflect CompilerMessage APIs when present.
            TryCollectCompilerMessages(errors, warnings);

            // Always include the authoritative compile-failed flag.
            if (EditorUtility.scriptCompilationFailed && errors.Count == 0)
            {
                errors.Add(new Dictionary<string, object>
                {
                    ["category"] = "Validation",
                    ["code"] = "UnityCompiler.Failed",
                    ["message"] = "Unity script compilation failed. See the Editor log for details.",
                });
            }

            bool success = errors.Count == 0 && !EditorUtility.scriptCompilationFailed;
            return MiniJson.Serialize(new Dictionary<string, object>
            {
                ["success"] = success,
                ["message"] = success
                    ? "Unity import validation completed."
                    : "Unity import validation found compiler errors.",
                ["data"] = new Dictionary<string, object>
                {
                    ["error_count"] = errors.Count,
                    ["warning_count"] = warnings.Count,
                },
                ["error_count"] = errors.Count,
                ["warning_count"] = warnings.Count,
                ["errors"] = errors,
                ["warnings"] = warnings,
                ["suggestedRemediation"] = success
                    ? null
                    : "Fix the listed compiler errors, then re-run unity_validate_import.",
            });
        }

        private static void TryCollectCompilerMessages(List<object> errors, List<object> warnings)
        {
            try
            {
                // Unity versions differ; probe for GetCompilerMessages overloads via reflection.
                var methods = typeof(CompilationPipeline).GetMethods(BindingFlags.Public | BindingFlags.Static);
                foreach (var method in methods)
                {
                    if (method.Name != "GetCompilerMessages")
                        continue;

                    object result;
                    var parameters = method.GetParameters();
                    if (parameters.Length == 0)
                    {
                        result = method.Invoke(null, null);
                    }
                    else if (parameters.Length == 1)
                    {
                        var assemblies = CompilationPipeline.GetAssemblies();
                        var list = new List<object>();
                        foreach (var assembly in assemblies)
                        {
                            var messages = method.Invoke(null, new object[] { assembly }) as System.Array;
                            if (messages == null) continue;
                            foreach (var message in messages)
                                list.Add(message);
                        }
                        result = list.ToArray();
                    }
                    else continue;

                    if (result is not System.Collections.IEnumerable enumerable)
                        continue;

                    foreach (var message in enumerable)
                    {
                        if (message == null) continue;
                        var typeProp = message.GetType().GetField("type") ?? (MemberInfo)message.GetType().GetProperty("type");
                        var messageProp = message.GetType().GetField("message") ?? (MemberInfo)message.GetType().GetProperty("message");
                        var fileProp = message.GetType().GetField("file") ?? (MemberInfo)message.GetType().GetProperty("file");
                        var lineProp = message.GetType().GetField("line") ?? (MemberInfo)message.GetType().GetProperty("line");

                        object typeVal = GetMember(typeProp, message);
                        string msg = System.Convert.ToString(GetMember(messageProp, message)) ?? "";
                        string file = System.Convert.ToString(GetMember(fileProp, message));
                        object line = GetMember(lineProp, message);

                        bool isError = typeVal != null && typeVal.ToString().IndexOf("Error", System.StringComparison.OrdinalIgnoreCase) >= 0;
                        var payload = new Dictionary<string, object>
                        {
                            ["category"] = "Validation",
                            ["code"] = isError ? "UnityCompiler.Error" : "UnityCompiler.Warning",
                            ["message"] = msg,
                            ["details"] = new Dictionary<string, object>
                            {
                                ["file"] = file,
                                ["line"] = line,
                            }
                        };
                        if (isError) errors.Add(payload);
                        else warnings.Add(payload);
                    }

                    return;
                }
            }
            catch
            {
                // Fall back to scriptCompilationFailed only.
            }
        }

        private static object GetMember(MemberInfo member, object target)
        {
            if (member is FieldInfo field) return field.GetValue(target);
            if (member is PropertyInfo prop) return prop.GetValue(target);
            return null;
        }
    }
}
