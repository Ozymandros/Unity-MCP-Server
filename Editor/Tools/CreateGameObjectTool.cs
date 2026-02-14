using System;
using System.Text.Json;
using UnityEditor;
using UnityEngine;

namespace UnityMCP
{
    /// <summary>
    /// MCP tool to create game objects in the current scene.
    /// </summary>
    public class CreateGameObjectTool : BaseMcpTool
    {
        public override string GetName()
        {
            return "create_gameobject";
        }

        public override string GetDescription()
        {
            return "Creates a new GameObject in the active scene with optional primitive type, position, and components";
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
                        description = "Name of the GameObject"
                    },
                    type = new
                    {
                        type = "string",
                        description = "Type of object to create",
                        @enum = new[] { "empty", "cube", "sphere", "capsule", "cylinder", "plane", "quad" }
                    },
                    position = new
                    {
                        type = "object",
                        description = "Position in world space",
                        properties = new
                        {
                            x = new { type = "number" },
                            y = new { type = "number" },
                            z = new { type = "number" }
                        }
                    },
                    parent = new
                    {
                        type = "string",
                        description = "Optional parent GameObject name"
                    }
                },
                required = new[] { "name", "type" }
            };
        }

        public override object Execute(JsonElement parameters)
        {
            try
            {
                string objectName = GetStringParam(parameters, "name", "GameObject");
                string objectType = GetStringParam(parameters, "type", "empty");
                string parentName = GetStringParam(parameters, "parent");

                GameObject gameObject;

                // Create GameObject based on type
                switch (objectType.ToLower())
                {
                    case "cube":
                        gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        break;
                    case "sphere":
                        gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        break;
                    case "capsule":
                        gameObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                        break;
                    case "cylinder":
                        gameObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        break;
                    case "plane":
                        gameObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
                        break;
                    case "quad":
                        gameObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
                        break;
                    default:
                        gameObject = new GameObject();
                        break;
                }

                gameObject.name = objectName;

                // Set position if provided
                if (parameters.TryGetProperty("position", out JsonElement posElement))
                {
                    float x = GetFloatFromElement(posElement, "x", 0f);
                    float y = GetFloatFromElement(posElement, "y", 0f);
                    float z = GetFloatFromElement(posElement, "z", 0f);
                    gameObject.transform.position = new Vector3(x, y, z);
                }

                // Set parent if provided
                if (!string.IsNullOrEmpty(parentName))
                {
                    GameObject parent = GameObject.Find(parentName);
                    if (parent != null)
                    {
                        gameObject.transform.SetParent(parent.transform);
                    }
                    else
                    {
                        Debug.LogWarning($"[CreateGameObjectTool] Parent '{parentName}' not found");
                    }
                }

                // Register undo
                Undo.RegisterCreatedObjectUndo(gameObject, $"Create {objectName}");

                Debug.Log($"[CreateGameObjectTool] Created GameObject: {objectName}");

                return new
                {
                    success = true,
                    name = gameObject.name,
                    instanceId = gameObject.GetInstanceID(),
                    type = objectType,
                    position = new
                    {
                        x = gameObject.transform.position.x,
                        y = gameObject.transform.position.y,
                        z = gameObject.transform.position.z
                    },
                    message = $"GameObject '{objectName}' created successfully"
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CreateGameObjectTool] Error: {ex.Message}");
                return new
                {
                    success = false,
                    error = ex.Message
                };
            }
        }

        private float GetFloatFromElement(JsonElement element, string propertyName, float defaultValue)
        {
            if (element.TryGetProperty(propertyName, out JsonElement propElement))
            {
                return propElement.GetSingle();
            }
            return defaultValue;
        }
    }
}
