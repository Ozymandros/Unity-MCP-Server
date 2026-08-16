using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityMcp.Bridge.Handlers
{
    public static class DomainHandler
    {
        public static string CreateCamera(Dictionary<string, object> args)
        {
            var scene = OpenScene(args);
            string parentPath = CommandDispatcher.GetString(args, "parentPath");
            string name = CommandDispatcher.GetString(args, "name", "Main Camera");
            var go = new GameObject(name);
            var cam = go.AddComponent<Camera>();
            cam.tag = "MainCamera";
            if (!string.IsNullOrWhiteSpace(parentPath))
            {
                var parent = Find(scene, parentPath);
                if (parent != null) go.transform.SetParent(parent, false);
            }
            ApplyCamera(cam, args);
            Save(scene);
            return CommandDispatcher.Ok("Camera created.", new Dictionary<string, object> { ["name"] = name });
        }

        public static string UpdateCamera(Dictionary<string, object> args)
        {
            var scene = OpenScene(args);
            var t = Find(scene, CommandDispatcher.GetString(args, "objectPath"));
            if (t == null) return CommandDispatcher.Fail("Camera.NotFound", "Camera GameObject not found.");
            var cam = t.GetComponent<Camera>();
            if (cam == null) return CommandDispatcher.Fail("Camera.MissingComponent", "No Camera component.");
            ApplyCamera(cam, args);
            Save(scene);
            return CommandDispatcher.Ok("Camera updated.");
        }

        public static string ValidateCameras(Dictionary<string, object> args)
        {
            var issues = new List<object>();
            foreach (var cam in Resources.FindObjectsOfTypeAll<Camera>().Where(IsSceneObject))
            {
                if (cam.tag == "MainCamera" && !cam.enabled)
                    issues.Add(Issue("Camera.MainDisabled", cam.name + " is MainCamera but disabled."));
                if (cam.fieldOfView <= 0)
                    issues.Add(Issue("Camera.InvalidFov", cam.name + " has invalid FOV."));
                if (cam.nearClipPlane <= 0 || cam.farClipPlane <= cam.nearClipPlane)
                    issues.Add(Issue("Camera.InvalidClipPlanes", cam.name + " has invalid clip planes."));
            }
            if (!Resources.FindObjectsOfTypeAll<Camera>().Any(c => IsSceneObject(c) && c.tag == "MainCamera"))
                issues.Add(Issue("Camera.MissingMain", "No MainCamera tagged camera found.", "Tag one Camera as MainCamera."));
            return CommandDispatcher.Ok("Camera validation completed.", new Dictionary<string, object> { ["issues"] = issues });
        }

        public static string CreateLight(Dictionary<string, object> args)
        {
            var scene = OpenScene(args);
            string name = CommandDispatcher.GetString(args, "name", "Directional Light");
            var go = new GameObject(name);
            var light = go.AddComponent<Light>();
            ApplyLight(light, args);
            string parentPath = CommandDispatcher.GetString(args, "parentPath");
            if (!string.IsNullOrWhiteSpace(parentPath))
            {
                var parent = Find(scene, parentPath);
                if (parent != null) go.transform.SetParent(parent, false);
            }
            Save(scene);
            return CommandDispatcher.Ok("Light created.", new Dictionary<string, object> { ["name"] = name });
        }

        public static string UpdateLight(Dictionary<string, object> args)
        {
            var scene = OpenScene(args);
            var t = Find(scene, CommandDispatcher.GetString(args, "objectPath"));
            if (t == null) return CommandDispatcher.Fail("Light.NotFound", "Light GameObject not found.");
            var light = t.GetComponent<Light>();
            if (light == null) return CommandDispatcher.Fail("Light.MissingComponent", "No Light component.");
            ApplyLight(light, args);
            Save(scene);
            return CommandDispatcher.Ok("Light updated.");
        }

        public static string ValidateLights(Dictionary<string, object> args)
        {
            var issues = new List<object>();
            var lights = Resources.FindObjectsOfTypeAll<Light>().Where(IsSceneObject).ToList();
            if (lights.Count == 0)
                issues.Add(Issue("Light.NoneFound", "No lights found in open scenes."));
            foreach (var light in lights)
            {
                if (light.intensity <= 0)
                    issues.Add(Issue("Light.ZeroIntensity", light.name + " has zero or negative intensity."));
                if (light.type == LightType.Point && light.range <= 0)
                    issues.Add(Issue("Light.InvalidRange", light.name + " point light has invalid range."));
            }
            return CommandDispatcher.Ok("Light validation completed.", new Dictionary<string, object> { ["issues"] = issues });
        }

        public static string CreatePhysics(Dictionary<string, object> args)
        {
            var scene = OpenScene(args);
            string name = CommandDispatcher.GetString(args, "name", "PhysicsBody");
            var go = new GameObject(name);
            var rb = go.AddComponent<Rigidbody>();
            if (CommandDispatcher.GetBool(args, "boxCollider", true))
                go.AddComponent<BoxCollider>();
            ApplyRigidbody(rb, args);
            string parentPath = CommandDispatcher.GetString(args, "parentPath");
            if (!string.IsNullOrWhiteSpace(parentPath))
            {
                var parent = Find(scene, parentPath);
                if (parent != null) go.transform.SetParent(parent, false);
            }
            Save(scene);
            return CommandDispatcher.Ok("Physics body created.", new Dictionary<string, object> { ["name"] = name });
        }

        public static string UpdatePhysics(Dictionary<string, object> args)
        {
            var scene = OpenScene(args);
            var t = Find(scene, CommandDispatcher.GetString(args, "objectPath"));
            if (t == null) return CommandDispatcher.Fail("Physics.NotFound", "Physics GameObject not found.");
            var rb = t.GetComponent<Rigidbody>();
            if (rb == null) return CommandDispatcher.Fail("Physics.MissingRigidbody", "No Rigidbody component.");
            ApplyRigidbody(rb, args);
            Save(scene);
            return CommandDispatcher.Ok("Physics body updated.");
        }

        public static string ValidatePhysics(Dictionary<string, object> args)
        {
            var issues = new List<object>();
            foreach (var rb in Resources.FindObjectsOfTypeAll<Rigidbody>().Where(IsSceneObject))
            {
                var colliders = rb.GetComponents<Collider>();
                if (colliders.Length == 0)
                    issues.Add(Issue("Physics.RigidbodyWithoutCollider", rb.name + " has Rigidbody but no Collider."));
                foreach (var col in colliders.Where(c => c.isTrigger))
                {
                    // Triggers without any MonoBehaviour are often incomplete setups.
                    if (rb.GetComponents<MonoBehaviour>().Length == 0)
                        issues.Add(Issue("Physics.TriggerWithoutHandler", rb.name + " has trigger collider but no MonoBehaviour handler."));
                }
                if (rb.mass <= 0)
                    issues.Add(Issue("Physics.InvalidMass", rb.name + " has invalid mass."));
            }
            return CommandDispatcher.Ok("Physics validation completed.", new Dictionary<string, object> { ["issues"] = issues });
        }

        private static void ApplyCamera(Camera cam, Dictionary<string, object> args)
        {
            if (args.ContainsKey("fov")) cam.fieldOfView = Convert.ToSingle(args["fov"]);
            if (args.ContainsKey("nearClip")) cam.nearClipPlane = Convert.ToSingle(args["nearClip"]);
            if (args.ContainsKey("farClip")) cam.farClipPlane = Convert.ToSingle(args["farClip"]);
            if (args.ContainsKey("depth")) cam.depth = Convert.ToSingle(args["depth"]);
        }

        private static void ApplyLight(Light light, Dictionary<string, object> args)
        {
            if (args.ContainsKey("type"))
            {
                string type = Convert.ToString(args["type"]);
                light.type = type?.ToLowerInvariant() switch
                {
                    "point" => LightType.Point,
                    "spot" => LightType.Spot,
                    "rectangle" or "area" => LightType.Rectangle,
                    _ => LightType.Directional,
                };
            }
            if (args.ContainsKey("intensity")) light.intensity = Convert.ToSingle(args["intensity"]);
            if (args.ContainsKey("range")) light.range = Convert.ToSingle(args["range"]);
        }

        private static void ApplyRigidbody(Rigidbody rb, Dictionary<string, object> args)
        {
            if (args.ContainsKey("mass")) rb.mass = Convert.ToSingle(args["mass"]);
            if (args.ContainsKey("useGravity")) rb.useGravity = Convert.ToBoolean(args["useGravity"]);
            if (args.ContainsKey("isKinematic")) rb.isKinematic = Convert.ToBoolean(args["isKinematic"]);
        }

        private static Scene OpenScene(Dictionary<string, object> args)
        {
            string fileName = CommandDispatcher.GetString(args, "fileName");
            return EditorSceneManager.OpenScene(fileName, OpenSceneMode.Single);
        }

        private static Transform Find(Scene scene, string objectPath)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (string.Equals(root.name, objectPath, StringComparison.OrdinalIgnoreCase))
                    return root.transform;
                var t = root.transform.Find(objectPath);
                if (t != null) return t;
                foreach (var child in root.GetComponentsInChildren<Transform>(true))
                {
                    if (string.Equals(child.name, objectPath, StringComparison.OrdinalIgnoreCase))
                        return child;
                }
            }
            return null;
        }

        private static void Save(Scene scene)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static bool IsSceneObject(Component c) =>
            c != null && c.gameObject != null && !EditorUtility.IsPersistent(c.gameObject);

        private static Dictionary<string, object> Issue(string code, string message, string remediation = null) =>
            new()
            {
                ["category"] = "Validation",
                ["code"] = code,
                ["message"] = message,
                ["suggestedRemediation"] = remediation,
            };
    }
}
