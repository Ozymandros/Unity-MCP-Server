using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityMcp.Bridge.Handlers
{
    public static class NativeSystemsHandler
    {
        public static string CreateAnimator(Dictionary<string, object> args)
        {
            string path = CommandDispatcher.GetString(args, "fileName");
            string name = CommandDispatcher.GetString(args, "name", Path.GetFileNameWithoutExtension(path));
            EnsureFolder(path);
            string controllerPath = Path.ChangeExtension(path, ".controller").Replace('\\', '/');
            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.name = name;
            if (args.ContainsKey("states") && args["states"] is List<object> states)
            {
                var sm = controller.layers[0].stateMachine;
                foreach (var stateObj in states)
                {
                    if (stateObj is not Dictionary<string, object> state) continue;
                    string stateName = Convert.ToString(state.GetValueOrDefault("name", "State"));
                    sm.AddState(stateName);
                }
            }
            AssetDatabase.SaveAssets();
            return CommandDispatcher.Ok("Native animator controller created.", new Dictionary<string, object>
            {
                ["path"] = controllerPath,
                ["native"] = true,
            });
        }

        public static string CreateTimeline(Dictionary<string, object> args)
        {
            string path = CommandDispatcher.GetString(args, "fileName");
            EnsureFolder(path);
            // Timeline package may be absent; create a PlayableAsset placeholder via ScriptableObject if unavailable.
            try
            {
                var timelineType = FindType("UnityEngine.Timeline.TimelineAsset");
                if (timelineType == null)
                    return CommandDispatcher.Fail("Timeline.PackageMissing", "Timeline package is not installed.", "Add com.unity.timeline via UPM.");
                var asset = ScriptableObject.CreateInstance(timelineType);
                AssetDatabase.CreateAsset(asset, Path.ChangeExtension(path, ".playable").Replace('\\', '/'));
                AssetDatabase.SaveAssets();
                return CommandDispatcher.Ok("Native timeline asset created.", new Dictionary<string, object>
                {
                    ["path"] = Path.ChangeExtension(path, ".playable").Replace('\\', '/'),
                    ["native"] = true,
                });
            }
            catch (Exception ex)
            {
                return CommandDispatcher.Fail("Timeline.CreateFailed", ex.Message);
            }
        }

        public static string CreateVfx(Dictionary<string, object> args)
        {
            string path = CommandDispatcher.GetString(args, "fileName");
            string name = CommandDispatcher.GetString(args, "name", "ParticleEffect");
            EnsureFolder(path);
            string prefabPath = Path.ChangeExtension(path, ".prefab").Replace('\\', '/');
            var go = new GameObject(name);
            go.AddComponent<ParticleSystem>();
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            UnityEngine.Object.DestroyImmediate(go);
            return CommandDispatcher.Ok("Native VFX prefab created.", new Dictionary<string, object>
            {
                ["path"] = prefabPath,
                ["native"] = true,
            });
        }

        public static string CreateNavMesh(Dictionary<string, object> args)
        {
            string path = CommandDispatcher.GetString(args, "fileName", "Assets/Settings/NavMeshSurface.prefab");
            EnsureFolder(path);
            var go = new GameObject("NavMeshSurface");
            var surfaceType = FindType("Unity.AI.Navigation.NavMeshSurface")
                              ?? FindType("UnityEngine.AI.NavMeshSurface");
            if (surfaceType == null)
            {
                UnityEngine.Object.DestroyImmediate(go);
                return CommandDispatcher.Fail("NavMesh.PackageMissing", "AI Navigation package is not installed.", "Add com.unity.ai.navigation via UPM.");
            }
            go.AddComponent(surfaceType);
            string prefabPath = Path.ChangeExtension(path, ".prefab").Replace('\\', '/');
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            UnityEngine.Object.DestroyImmediate(go);
            return CommandDispatcher.Ok("Native NavMesh surface created.", new Dictionary<string, object>
            {
                ["path"] = prefabPath,
                ["native"] = true,
            });
        }

        public static string CreateInputActions(Dictionary<string, object> args)
        {
            string path = CommandDispatcher.GetString(args, "fileName");
            EnsureFolder(path);
            // Write a minimal but importer-valid Input Actions JSON document.
            string assetPath = Path.ChangeExtension(path, ".inputactions").Replace('\\', '/');
            string json = @"{
    ""name"": ""PlayerInput"",
    ""maps"": [
        {
            ""name"": ""Player"",
            ""id"": """ + Guid.NewGuid() + @""",
            ""actions"": [
                {
                    ""name"": ""Move"",
                    ""type"": ""Value"",
                    ""id"": """ + Guid.NewGuid() + @""",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true
                }
            ],
            ""bindings"": []
        }
    ],
    ""controlSchemes"": []
}";
            if (args.ContainsKey("content"))
                json = Convert.ToString(args["content"]) ?? json;

            File.WriteAllText(Path.GetFullPath(assetPath), json);
            AssetDatabase.ImportAsset(assetPath);
            AssetDatabase.Refresh();
            return CommandDispatcher.Ok("Input actions asset written.", new Dictionary<string, object>
            {
                ["path"] = assetPath,
                ["native"] = true,
            });
        }

        public static string CreateUi(Dictionary<string, object> args)
        {
            string scenePath = CommandDispatcher.GetString(args, "fileName");
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            if (UnityEngine.Object.FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            if (args.ContainsKey("layout") && args["layout"] is Dictionary<string, object> layout)
                BuildLayout(canvasGo.transform, layout);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return CommandDispatcher.Ok("Native uGUI canvas created.", new Dictionary<string, object>
            {
                ["scenePath"] = scene.path,
                ["canvas"] = "Canvas",
                ["native"] = true,
            });
        }

        private static void BuildLayout(Transform parent, Dictionary<string, object> layout)
        {
            string type = Convert.ToString(layout.GetValueOrDefault("type", "Panel")) ?? "Panel";
            string name = Convert.ToString(layout.GetValueOrDefault("name", type)) ?? type;
            GameObject go;
            switch (type.ToLowerInvariant())
            {
                case "button":
                    go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                    break;
                case "text":
                    go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                    var text = go.GetComponent<Text>();
                    text.text = Convert.ToString(layout.GetValueOrDefault("text", "Text"));
                    text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    break;
                case "image":
                    go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    break;
                default:
                    go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    break;
            }
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            if (layout.ContainsKey("children") && layout["children"] is List<object> children)
            {
                foreach (var child in children)
                {
                    if (child is Dictionary<string, object> childLayout)
                        BuildLayout(go.transform, childLayout);
                }
            }
        }

        private static Type FindType(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(fullName);
                if (t != null) return t;
            }
            return null;
        }

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
}
