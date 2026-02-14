using System;
using System.IO;
using System.Text;
using System.Text.Json;
using UnityEditor;
using UnityEngine;

namespace UnityMCP
{
    /// <summary>
    /// MCP tool to create C# scripts in the Unity project.
    /// </summary>
    public class CreateScriptTool : BaseMcpTool
    {
        private const string DEFAULT_SCRIPTS_PATH = "Assets/Scripts";

        public override string GetName()
        {
            return "create_script";
        }

        public override string GetDescription()
        {
            return "Creates a new C# script file in the Unity project with optional template (MonoBehaviour, ScriptableObject, or Plain)";
        }

        public override object GetInputSchema()
        {
            return new
            {
                type = "object",
                properties = new
                {
                    name = new
                    {
                        type = "string",
                        description = "Name of the script class"
                    },
                    type = new
                    {
                        type = "string",
                        description = "Script type template",
                        @enum = new[] { "monobehaviour", "scriptableobject", "plain", "interface" }
                    },
                    path = new
                    {
                        type = "string",
                        description = "Optional custom path (defaults to Assets/Scripts)"
                    },
                    @namespace = new
                    {
                        type = "string",
                        description = "Optional namespace for the script"
                    }
                },
                required = new[] { "name" }
            };
        }

        public override object Execute(JsonElement parameters)
        {
            try
            {
                string scriptName = GetStringParam(parameters, "name", "NewScript");
                string scriptType = GetStringParam(parameters, "type", "monobehaviour");
                string scriptsPath = GetStringParam(parameters, "path", DEFAULT_SCRIPTS_PATH);
                string scriptNamespace = GetStringParam(parameters, "namespace");

                // Validate script name
                if (string.IsNullOrWhiteSpace(scriptName))
                {
                    throw new ArgumentException("Script name cannot be empty");
                }

                // Sanitize script name
                scriptName = SanitizeClassName(scriptName);

                // Ensure Scripts directory exists
                if (!Directory.Exists(scriptsPath))
                {
                    Directory.CreateDirectory(scriptsPath);
                    AssetDatabase.Refresh();
                }

                // Generate script content
                string scriptContent = GenerateScriptContent(scriptName, scriptType, scriptNamespace);

                // Create script file
                string scriptPath = Path.Combine(scriptsPath, $"{scriptName}.cs");

                if (File.Exists(scriptPath))
                {
                    throw new Exception($"Script already exists at {scriptPath}");
                }

                File.WriteAllText(scriptPath, scriptContent, Encoding.UTF8);
                AssetDatabase.Refresh();

                Debug.Log($"[CreateScriptTool] Created script: {scriptPath}");

                return new
                {
                    success = true,
                    path = scriptPath,
                    name = scriptName,
                    type = scriptType,
                    message = $"Script '{scriptName}' created successfully at {scriptPath}"
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CreateScriptTool] Error: {ex.Message}");
                return new
                {
                    success = false,
                    error = ex.Message
                };
            }
        }

        private string GenerateScriptContent(string className, string scriptType, string namespaceName)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("using System;");
            sb.AppendLine("using UnityEngine;");

            if (scriptType == "monobehaviour" || scriptType == "scriptableobject")
            {
                sb.AppendLine();
            }

            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.AppendLine($"namespace {namespaceName}");
                sb.AppendLine("{");
            }

            string indent = !string.IsNullOrEmpty(namespaceName) ? "    " : "";

            switch (scriptType.ToLower())
            {
                case "monobehaviour":
                    sb.AppendLine($"{indent}/// <summary>");
                    sb.AppendLine($"{indent}/// {className} MonoBehaviour");
                    sb.AppendLine($"{indent}/// </summary>");
                    sb.AppendLine($"{indent}public class {className} : MonoBehaviour");
                    sb.AppendLine($"{indent}{{");
                    sb.AppendLine($"{indent}    void Start()");
                    sb.AppendLine($"{indent}    {{");
                    sb.AppendLine($"{indent}        // Initialization code here");
                    sb.AppendLine($"{indent}    }}");
                    sb.AppendLine();
                    sb.AppendLine($"{indent}    void Update()");
                    sb.AppendLine($"{indent}    {{");
                    sb.AppendLine($"{indent}        // Update code here");
                    sb.AppendLine($"{indent}    }}");
                    sb.AppendLine($"{indent}}}");
                    break;

                case "scriptableobject":
                    sb.AppendLine($"{indent}/// <summary>");
                    sb.AppendLine($"{indent}/// {className} ScriptableObject");
                    sb.AppendLine($"{indent}/// </summary>");
                    sb.AppendLine($"{indent}[CreateAssetMenu(fileName = \"{className}\", menuName = \"ScriptableObjects/{className}\")]");
                    sb.AppendLine($"{indent}public class {className} : ScriptableObject");
                    sb.AppendLine($"{indent}{{");
                    sb.AppendLine($"{indent}    // Add your data fields here");
                    sb.AppendLine($"{indent}}}");
                    break;

                case "interface":
                    sb.AppendLine($"{indent}/// <summary>");
                    sb.AppendLine($"{indent}/// {className} Interface");
                    sb.AppendLine($"{indent}/// </summary>");
                    sb.AppendLine($"{indent}public interface I{className}");
                    sb.AppendLine($"{indent}{{");
                    sb.AppendLine($"{indent}    // Define interface methods here");
                    sb.AppendLine($"{indent}}}");
                    break;

                default: // plain
                    sb.AppendLine($"{indent}/// <summary>");
                    sb.AppendLine($"{indent}/// {className} class");
                    sb.AppendLine($"{indent}/// </summary>");
                    sb.AppendLine($"{indent}public class {className}");
                    sb.AppendLine($"{indent}{{");
                    sb.AppendLine($"{indent}    // Add your code here");
                    sb.AppendLine($"{indent}}}");
                    break;
            }

            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.AppendLine("}");
            }

            return sb.ToString();
        }

        private string SanitizeClassName(string className)
        {
            // Remove spaces and invalid characters
            className = className.Replace(" ", "");
            
            // Ensure it starts with a letter or underscore
            if (!char.IsLetter(className[0]) && className[0] != '_')
            {
                className = "_" + className;
            }

            // Remove any characters that aren't letters, digits, or underscores
            StringBuilder sb = new StringBuilder();
            foreach (char c in className)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }
    }
}
