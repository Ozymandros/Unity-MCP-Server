using System;
using System.IO;
using System.Text.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityMCP
{
    /// <summary>
    /// MCP tool to create a new Unity scene.
    /// </summary>
    public class CreateSceneTool : BaseMcpTool
    {
        private const string DEFAULT_SCENES_PATH = "Assets/Scenes";

        public override string GetName()
        {
            return "create_scene";
        }

        public override string GetDescription()
        {
            return "Creates a new Unity scene with the specified name and optional setup type";
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
                        description = "Name of the scene to create"
                    },
                    path = new
                    {
                        type = "string",
                        description = "Optional custom path (defaults to Assets/Scenes)"
                    },
                    setup = new
                    {
                        type = "string",
                        description = "Scene setup type: 'default' (Main Camera + Directional Light) or 'empty' (no objects)",
                        @enum = new[] { "default", "empty" }
                    }
                },
                required = new[] { "name" }
            };
        }

        public override object Execute(JsonElement parameters)
        {
            try
            {
                // Parse scene name from parameters
                string sceneName = GetStringParam(parameters, "name", "NewScene");

                // Validate scene name
                if (string.IsNullOrWhiteSpace(sceneName))
                {
                    throw new ArgumentException("Scene name cannot be empty");
                }

                // Sanitize scene name (remove invalid characters)
                sceneName = SanitizeFileName(sceneName);

                // Get optional path parameter
                string scenesPath = GetStringParam(parameters, "path", DEFAULT_SCENES_PATH);

                // Get setup type
                string setupType = GetStringParam(parameters, "setup", "default");
                NewSceneSetup sceneSetup = setupType.ToLower() == "empty" 
                    ? NewSceneSetup.EmptyScene 
                    : NewSceneSetup.DefaultGameObjects;

                // Ensure Scenes directory exists
                if (!Directory.Exists(scenesPath))
                {
                    Directory.CreateDirectory(scenesPath);
                    AssetDatabase.Refresh();
                }

                // Create new scene
                Scene newScene = EditorSceneManager.NewScene(sceneSetup, NewSceneMode.Single);

                // Save scene
                string scenePath = $"{scenesPath}/{sceneName}.unity";
                
                // Check if scene already exists
                if (File.Exists(scenePath))
                {
                    Debug.LogWarning($"[CreateSceneTool] Scene already exists, overwriting: {scenePath}");
                }

                bool saved = EditorSceneManager.SaveScene(newScene, scenePath);

                if (!saved)
                {
                    throw new Exception($"Failed to save scene at {scenePath}");
                }

                Debug.Log($"[CreateSceneTool] Created scene: {scenePath}");

                // Return scene information
                return new
                {
                    success = true,
                    path = scenePath,
                    name = sceneName,
                    setup = setupType,
                    objectCount = newScene.rootCount,
                    message = $"Scene '{sceneName}' created successfully at {scenePath}"
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CreateSceneTool] Error: {ex.Message}");
                return new
                {
                    success = false,
                    error = ex.Message
                };
            }
        }

        private string SanitizeFileName(string fileName)
        {
            // Remove invalid characters from file name
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                fileName = fileName.Replace(c.ToString(), "");
            }
            return fileName;
        }
    }
}
