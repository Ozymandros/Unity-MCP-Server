using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityMCP
{
    /// <summary>
    /// MCP tool to get information about the current Unity scene.
    /// </summary>
    public class GetSceneInfoTool : BaseMcpTool
    {
        public override string GetName()
        {
            return "get_scene_info";
        }

        public override string GetDescription()
        {
            return "Gets detailed information about the currently active Unity scene including objects, hierarchy, and properties";
        }

        public override object GetInputSchema()
        {
            return new
            {
                type = "object",
                properties = new
                {
                    includeHierarchy = new
                    {
                        type = "boolean",
                        description = "Include full GameObject hierarchy (default: true)"
                    },
                    includeComponents = new
                    {
                        type = "boolean",
                        description = "Include component information for each GameObject (default: false)"
                    }
                },
                required = new string[] { }
            };
        }

        public override object Execute(JsonElement parameters)
        {
            try
            {
                bool includeHierarchy = GetBoolParam(parameters, "includeHierarchy", true);
                bool includeComponents = GetBoolParam(parameters, "includeComponents", false);

                Scene activeScene = SceneManager.GetActiveScene();

                if (!activeScene.IsValid())
                {
                    return new
                    {
                        success = false,
                        error = "No active scene"
                    };
                }

                // Get root GameObjects
                GameObject[] rootObjects = activeScene.GetRootGameObjects();

                var sceneInfo = new
                {
                    success = true,
                    name = activeScene.name,
                    path = activeScene.path,
                    isLoaded = activeScene.isLoaded,
                    isDirty = activeScene.isDirty,
                    buildIndex = activeScene.buildIndex,
                    rootCount = activeScene.rootCount,
                    rootObjects = includeHierarchy 
                        ? rootObjects.Select(go => GetGameObjectInfo(go, includeComponents)).ToArray()
                        : rootObjects.Select(go => go.name).ToArray(),
                    totalObjectCount = GetTotalObjectCount(rootObjects)
                };

                Debug.Log($"[GetSceneInfoTool] Retrieved info for scene: {activeScene.name}");

                return sceneInfo;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GetSceneInfoTool] Error: {ex.Message}");
                return new
                {
                    success = false,
                    error = ex.Message
                };
            }
        }

        private object GetGameObjectInfo(GameObject go, bool includeComponents)
        {
            var info = new Dictionary<string, object>
            {
                { "name", go.name },
                { "tag", go.tag },
                { "layer", LayerMask.LayerToName(go.layer) },
                { "active", go.activeSelf },
                { "static", go.isStatic },
                { "instanceId", go.GetInstanceID() },
                { "position", new { 
                    x = go.transform.position.x, 
                    y = go.transform.position.y, 
                    z = go.transform.position.z 
                }}
            };

            if (includeComponents)
            {
                Component[] components = go.GetComponents<Component>();
                info["components"] = components.Select(c => c != null ? c.GetType().Name : "Missing").ToArray();
            }

            // Get children
            if (go.transform.childCount > 0)
            {
                var children = new List<object>();
                for (int i = 0; i < go.transform.childCount; i++)
                {
                    children.Add(GetGameObjectInfo(go.transform.GetChild(i).gameObject, includeComponents));
                }
                info["children"] = children.ToArray();
            }

            return info;
        }

        private int GetTotalObjectCount(GameObject[] rootObjects)
        {
            int count = rootObjects.Length;
            foreach (var go in rootObjects)
            {
                count += CountChildren(go.transform);
            }
            return count;
        }

        private int CountChildren(Transform parent)
        {
            int count = parent.childCount;
            for (int i = 0; i < parent.childCount; i++)
            {
                count += CountChildren(parent.GetChild(i));
            }
            return count;
        }
    }
}
