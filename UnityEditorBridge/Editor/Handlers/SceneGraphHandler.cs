using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityMcp.Bridge.Handlers
{
    public static class SceneGraphHandler
    {
        public static string List(Dictionary<string, object> args)
        {
            var scene = OpenScene(args);
            var objects = new List<object>();
            foreach (var root in scene.GetRootGameObjects())
                Collect(root, "", objects);
            return CommandDispatcher.Ok("Scene graph listed.", new Dictionary<string, object>
            {
                ["scenePath"] = scene.path,
                ["objects"] = objects,
            });
        }

        public static string Add(Dictionary<string, object> args)
        {
            var scene = OpenScene(args);
            string parentPath = CommandDispatcher.GetString(args, "parentPath", "");
            string objectName = CommandDispatcher.GetString(args, "objectName", "GameObject");
            Transform parent = string.IsNullOrWhiteSpace(parentPath) ? null : FindTransform(scene, parentPath);
            var go = new GameObject(objectName);
            if (parent != null)
                go.transform.SetParent(parent, false);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return CommandDispatcher.Ok("GameObject added.", new Dictionary<string, object>
            {
                ["path"] = GetHierarchyPath(go.transform),
                ["name"] = go.name,
            });
        }

        public static string Remove(Dictionary<string, object> args)
        {
            var scene = OpenScene(args);
            var t = FindTransform(scene, CommandDispatcher.GetString(args, "objectPath"));
            if (t == null) return CommandDispatcher.Fail("Scene.ObjectNotFound", "GameObject not found.");
            string path = GetHierarchyPath(t);
            UnityEngine.Object.DestroyImmediate(t.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return CommandDispatcher.Ok("GameObject removed.", new Dictionary<string, object> { ["path"] = path });
        }

        public static string Rename(Dictionary<string, object> args)
        {
            var scene = OpenScene(args);
            var t = FindTransform(scene, CommandDispatcher.GetString(args, "objectPath"));
            if (t == null) return CommandDispatcher.Fail("Scene.ObjectNotFound", "GameObject not found.");
            string newName = CommandDispatcher.GetString(args, "newName");
            t.gameObject.name = newName;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return CommandDispatcher.Ok("GameObject renamed.", new Dictionary<string, object>
            {
                ["path"] = GetHierarchyPath(t),
                ["newName"] = newName,
            });
        }

        public static string Reparent(Dictionary<string, object> args)
        {
            var scene = OpenScene(args);
            var t = FindTransform(scene, CommandDispatcher.GetString(args, "objectPath"));
            if (t == null) return CommandDispatcher.Fail("Scene.ObjectNotFound", "GameObject not found.");
            string newParentPath = CommandDispatcher.GetString(args, "newParentPath");
            Transform parent = string.IsNullOrWhiteSpace(newParentPath) ? null : FindTransform(scene, newParentPath);
            t.SetParent(parent, true);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return CommandDispatcher.Ok("GameObject reparented.", new Dictionary<string, object>
            {
                ["path"] = GetHierarchyPath(t),
            });
        }

        public static string GetProperties(Dictionary<string, object> args)
        {
            var scene = OpenScene(args);
            var t = FindTransform(scene, CommandDispatcher.GetString(args, "objectPath"));
            if (t == null) return CommandDispatcher.Fail("Scene.ObjectNotFound", "GameObject not found.");
            var props = new Dictionary<string, object>
            {
                ["name"] = t.gameObject.name,
                ["activeSelf"] = t.gameObject.activeSelf,
                ["tag"] = t.gameObject.tag,
                ["layer"] = t.gameObject.layer,
                ["position"] = Vec(t.localPosition),
                ["eulerAngles"] = Vec(t.localEulerAngles),
                ["scale"] = Vec(t.localScale),
            };
            return CommandDispatcher.Ok("Properties read.", props);
        }

        public static string SetProperties(Dictionary<string, object> args)
        {
            var scene = OpenScene(args);
            var t = FindTransform(scene, CommandDispatcher.GetString(args, "objectPath"));
            if (t == null) return CommandDispatcher.Fail("Scene.ObjectNotFound", "GameObject not found.");
            var props = args.ContainsKey("properties") ? args["properties"] as Dictionary<string, object> : null;
            if (props == null) return CommandDispatcher.Fail("Scene.InvalidProperties", "properties object is required.");

            if (props.ContainsKey("name")) t.gameObject.name = Convert.ToString(props["name"]);
            if (props.ContainsKey("tag")) t.gameObject.tag = Convert.ToString(props["tag"]);
            if (props.ContainsKey("layer")) t.gameObject.layer = Convert.ToInt32(props["layer"]);
            if (props.ContainsKey("activeSelf")) t.gameObject.SetActive(Convert.ToBoolean(props["activeSelf"]));
            if (props.ContainsKey("position")) t.localPosition = ReadVec(props["position"], t.localPosition);
            if (props.ContainsKey("eulerAngles")) t.localEulerAngles = ReadVec(props["eulerAngles"], t.localEulerAngles);
            if (props.ContainsKey("scale")) t.localScale = ReadVec(props["scale"], t.localScale);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return CommandDispatcher.Ok("Properties updated.", new Dictionary<string, object> { ["path"] = GetHierarchyPath(t) });
        }

        public static string SetActive(Dictionary<string, object> args)
        {
            var scene = OpenScene(args);
            var t = FindTransform(scene, CommandDispatcher.GetString(args, "objectPath"));
            if (t == null) return CommandDispatcher.Fail("Scene.ObjectNotFound", "GameObject not found.");
            bool active = CommandDispatcher.GetBool(args, "active", true);
            t.gameObject.SetActive(active);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return CommandDispatcher.Ok("Active state updated.", new Dictionary<string, object>
            {
                ["path"] = GetHierarchyPath(t),
                ["active"] = active,
            });
        }

        public static string ListComponents(Dictionary<string, object> args)
        {
            var scene = OpenScene(args);
            var t = FindTransform(scene, CommandDispatcher.GetString(args, "objectPath"));
            if (t == null) return CommandDispatcher.Fail("Scene.ObjectNotFound", "GameObject not found.");
            var list = t.GetComponents<Component>()
                .Select((c, i) => new Dictionary<string, object>
                {
                    ["index"] = i,
                    ["type"] = c.GetType().Name,
                    ["enabled"] = c is Behaviour b ? b.enabled : true,
                })
                .Cast<object>()
                .ToList();
            return CommandDispatcher.Ok("Components listed.", new Dictionary<string, object> { ["components"] = list });
        }

        public static string AddComponent(Dictionary<string, object> args)
        {
            var scene = OpenScene(args);
            var t = FindTransform(scene, CommandDispatcher.GetString(args, "objectPath"));
            if (t == null) return CommandDispatcher.Fail("Scene.ObjectNotFound", "GameObject not found.");
            string typeName = CommandDispatcher.GetString(args, "componentType");
            var type = FindType(typeName);
            if (type == null) return CommandDispatcher.Fail("Component.UnknownType", "Unknown component type: " + typeName);
            t.gameObject.AddComponent(type);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return CommandDispatcher.Ok("Component added.", new Dictionary<string, object> { ["type"] = type.Name });
        }

        public static string RemoveComponent(Dictionary<string, object> args)
        {
            var scene = OpenScene(args);
            var t = FindTransform(scene, CommandDispatcher.GetString(args, "objectPath"));
            if (t == null) return CommandDispatcher.Fail("Scene.ObjectNotFound", "GameObject not found.");
            var c = FindComponent(t, CommandDispatcher.GetString(args, "componentType"), CommandDispatcher.GetInt(args, "componentIndex"));
            if (c == null) return CommandDispatcher.Fail("Component.NotFound", "Component not found.");
            if (c is Transform) return CommandDispatcher.Fail("Component.Protected", "Cannot remove Transform.");
            UnityEngine.Object.DestroyImmediate(c);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return CommandDispatcher.Ok("Component removed.");
        }

        public static string GetComponent(Dictionary<string, object> args)
        {
            var scene = OpenScene(args);
            var t = FindTransform(scene, CommandDispatcher.GetString(args, "objectPath"));
            if (t == null) return CommandDispatcher.Fail("Scene.ObjectNotFound", "GameObject not found.");
            var c = FindComponent(t, CommandDispatcher.GetString(args, "componentType"), CommandDispatcher.GetInt(args, "componentIndex"));
            if (c == null) return CommandDispatcher.Fail("Component.NotFound", "Component not found.");
            var so = new SerializedObject(c);
            var props = new Dictionary<string, object>();
            var iterator = so.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.name == "m_Script") continue;
                props[iterator.name] = iterator.propertyType + ":" + iterator.displayName;
            }
            return CommandDispatcher.Ok("Component properties read.", props);
        }

        public static string SetComponent(Dictionary<string, object> args)
        {
            var scene = OpenScene(args);
            var t = FindTransform(scene, CommandDispatcher.GetString(args, "objectPath"));
            if (t == null) return CommandDispatcher.Fail("Scene.ObjectNotFound", "GameObject not found.");
            var c = FindComponent(t, CommandDispatcher.GetString(args, "componentType"), CommandDispatcher.GetInt(args, "componentIndex"));
            if (c == null) return CommandDispatcher.Fail("Component.NotFound", "Component not found.");
            var props = args.ContainsKey("properties") ? args["properties"] as Dictionary<string, object> : null;
            if (props == null) return CommandDispatcher.Fail("Component.InvalidProperties", "properties object is required.");

            var so = new SerializedObject(c);
            foreach (var kv in props)
            {
                var prop = so.FindProperty(kv.Key);
                if (prop == null) continue;
                ApplyProperty(prop, kv.Value);
            }
            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return CommandDispatcher.Ok("Component properties updated.");
        }

        public static string SetComponentEnabled(Dictionary<string, object> args)
        {
            var scene = OpenScene(args);
            var t = FindTransform(scene, CommandDispatcher.GetString(args, "objectPath"));
            if (t == null) return CommandDispatcher.Fail("Scene.ObjectNotFound", "GameObject not found.");
            var c = FindComponent(t, CommandDispatcher.GetString(args, "componentType"), CommandDispatcher.GetInt(args, "componentIndex"));
            if (c is not Behaviour behaviour)
                return CommandDispatcher.Fail("Component.NotBehaviour", "Component cannot be enabled/disabled.");
            behaviour.enabled = CommandDispatcher.GetBool(args, "enabled", true);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return CommandDispatcher.Ok("Component enabled state updated.");
        }

        public static string AttachScript(Dictionary<string, object> args)
        {
            var scene = OpenScene(args);
            var t = FindTransform(scene, CommandDispatcher.GetString(args, "objectPath"));
            if (t == null) return CommandDispatcher.Fail("Scene.ObjectNotFound", "GameObject not found.");
            string scriptPath = CommandDispatcher.GetString(args, "scriptPath");
            var mono = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
            if (mono == null) return CommandDispatcher.Fail("Script.NotFound", "Script asset not found: " + scriptPath);
            var type = mono.GetClass();
            if (type == null || !typeof(MonoBehaviour).IsAssignableFrom(type))
                return CommandDispatcher.Fail("Script.InvalidType", "Script does not define a MonoBehaviour.");
            t.gameObject.AddComponent(type);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return CommandDispatcher.Ok("Script attached.", new Dictionary<string, object> { ["type"] = type.Name });
        }

        public static string InstantiatePrefab(Dictionary<string, object> args)
        {
            var scene = OpenScene(args);
            string prefabPath = CommandDispatcher.GetString(args, "prefabPath");
            string instanceName = CommandDispatcher.GetString(args, "instanceName");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return CommandDispatcher.Fail("Prefab.NotFound", "Prefab not found: " + prefabPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            if (!string.IsNullOrWhiteSpace(instanceName))
                instance.name = instanceName;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return CommandDispatcher.Ok("Prefab instantiated.", new Dictionary<string, object>
            {
                ["path"] = GetHierarchyPath(instance.transform),
                ["prefabPath"] = prefabPath,
            });
        }

        public static string SaveAsPrefab(Dictionary<string, object> args)
        {
            var scene = OpenScene(args);
            var t = FindTransform(scene, CommandDispatcher.GetString(args, "objectPath"));
            if (t == null) return CommandDispatcher.Fail("Scene.ObjectNotFound", "GameObject not found.");
            string prefabPath = CommandDispatcher.GetString(args, "prefabPath");
            EnsureAssetFolder(prefabPath);
            var prefab = PrefabUtility.SaveAsPrefabAsset(t.gameObject, prefabPath);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return CommandDispatcher.Ok("Prefab saved.", new Dictionary<string, object>
            {
                ["prefabPath"] = AssetDatabase.GetAssetPath(prefab),
            });
        }

        public static string Diff(Dictionary<string, object> args)
        {
            string pathA = CommandDispatcher.GetString(args, "fileNameA");
            string pathB = CommandDispatcher.GetString(args, "fileNameB");
            var sceneA = EditorSceneManager.OpenScene(pathA, OpenSceneMode.Single);
            var mapA = BuildPathMap(sceneA);
            var sceneB = EditorSceneManager.OpenScene(pathB, OpenSceneMode.Single);
            var mapB = BuildPathMap(sceneB);

            var added = mapB.Keys.Except(mapA.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
            var removed = mapA.Keys.Except(mapB.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
            var modified = new List<string>();
            foreach (var key in mapA.Keys.Intersect(mapB.Keys, StringComparer.OrdinalIgnoreCase))
            {
                if (!string.Equals(mapA[key], mapB[key], StringComparison.Ordinal))
                    modified.Add(key);
            }

            return CommandDispatcher.Ok("Scene diff completed.", new Dictionary<string, object>
            {
                ["added"] = added,
                ["removed"] = removed,
                ["modified"] = modified,
            });
        }

        private static Dictionary<string, string> BuildPathMap(Scene scene)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var root in scene.GetRootGameObjects())
                CollectIdentity(root, "", map);
            return map;
        }

        private static void CollectIdentity(GameObject go, string parentPath, Dictionary<string, string> map)
        {
            string path = string.IsNullOrEmpty(parentPath) ? go.name : parentPath + "/" + go.name;
            string identity = string.Join("|", go.GetComponents<Component>().Select(c => c.GetType().FullName));
            map[path] = identity;
            foreach (Transform child in go.transform)
                CollectIdentity(child.gameObject, path, map);
        }

        private static void Collect(GameObject go, string parentPath, List<object> objects)
        {
            string path = string.IsNullOrEmpty(parentPath) ? go.name : parentPath + "/" + go.name;
            objects.Add(new Dictionary<string, object>
            {
                ["name"] = go.name,
                ["path"] = path,
                ["components"] = go.GetComponents<Component>().Select(c => c.GetType().Name).ToList(),
                ["activeSelf"] = go.activeSelf,
            });
            foreach (Transform child in go.transform)
                Collect(child.gameObject, path, objects);
        }

        private static Scene OpenScene(Dictionary<string, object> args)
        {
            string fileName = CommandDispatcher.GetString(args, "fileName");
            if (string.IsNullOrWhiteSpace(fileName))
                throw new InvalidOperationException("fileName is required.");
            return EditorSceneManager.OpenScene(fileName, OpenSceneMode.Single);
        }

        private static Transform FindTransform(Scene scene, string objectPath)
        {
            if (string.IsNullOrWhiteSpace(objectPath))
                return null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (string.Equals(root.name, objectPath, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(GetHierarchyPath(root.transform), objectPath, StringComparison.OrdinalIgnoreCase))
                    return root.transform;
                var child = root.transform.Find(objectPath);
                if (child != null) return child;
                var all = root.GetComponentsInChildren<Transform>(true);
                foreach (var t in all)
                {
                    if (string.Equals(t.name, objectPath, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(GetHierarchyPath(t), objectPath, StringComparison.OrdinalIgnoreCase))
                        return t;
                }
            }
            return null;
        }

        private static string GetHierarchyPath(Transform t)
        {
            var parts = new List<string>();
            while (t != null)
            {
                parts.Add(t.name);
                t = t.parent;
            }
            parts.Reverse();
            return string.Join("/", parts);
        }

        private static Component FindComponent(Transform t, string typeName, int index)
        {
            var matches = t.GetComponents<Component>()
                .Where(c => string.Equals(c.GetType().Name, typeName, StringComparison.OrdinalIgnoreCase)
                            || string.Equals(c.GetType().FullName, typeName, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (index < 0 || index >= matches.Count) return matches.FirstOrDefault();
            return matches[index];
        }

        private static Type FindType(string typeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(typeName)
                           ?? assembly.GetTypes().FirstOrDefault(t => t.Name == typeName);
                if (type != null && typeof(Component).IsAssignableFrom(type))
                    return type;
            }
            return null;
        }

        private static Dictionary<string, object> Vec(Vector3 v) =>
            new() { ["x"] = v.x, ["y"] = v.y, ["z"] = v.z };

        private static Vector3 ReadVec(object value, Vector3 fallback)
        {
            if (value is Dictionary<string, object> d)
            {
                float x = d.ContainsKey("x") ? Convert.ToSingle(d["x"]) : fallback.x;
                float y = d.ContainsKey("y") ? Convert.ToSingle(d["y"]) : fallback.y;
                float z = d.ContainsKey("z") ? Convert.ToSingle(d["z"]) : fallback.z;
                return new Vector3(x, y, z);
            }
            return fallback;
        }

        private static void ApplyProperty(SerializedProperty prop, object value)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:
                    prop.intValue = Convert.ToInt32(value);
                    break;
                case SerializedPropertyType.Boolean:
                    prop.boolValue = Convert.ToBoolean(value);
                    break;
                case SerializedPropertyType.Float:
                    prop.floatValue = Convert.ToSingle(value);
                    break;
                case SerializedPropertyType.String:
                    prop.stringValue = Convert.ToString(value);
                    break;
                case SerializedPropertyType.ObjectReference:
                    if (value is string path && !string.IsNullOrWhiteSpace(path))
                        prop.objectReferenceValue = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                    break;
            }
        }

        private static void EnsureAssetFolder(string assetPath)
        {
            string dir = System.IO.Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(dir) || AssetDatabase.IsValidFolder(dir))
                return;
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
