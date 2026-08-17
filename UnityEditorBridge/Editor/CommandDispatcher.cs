using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityMcp.Bridge
{
    public static class CommandDispatcher
    {
        public static string Dispatch(string requestJson, bool requireToken, string expectedToken = null)
        {
            var request = MiniJson.Deserialize(requestJson) as Dictionary<string, object>;
            if (request == null)
                return Fail("Dispatch.InvalidJson", "Request JSON could not be parsed.");

            string op = GetString(request, "op");
            string token = GetString(request, "token");
            var args = request.ContainsKey("args") ? request["args"] as Dictionary<string, object> : null;
            args ??= new Dictionary<string, object>();

            if (requireToken)
            {
                if (string.IsNullOrEmpty(expectedToken) || !string.Equals(token, expectedToken, StringComparison.Ordinal))
                    return Fail("Dispatch.Unauthorized", "Live bridge token mismatch.");
            }

            if (string.IsNullOrWhiteSpace(op))
                return Fail("Dispatch.MissingOp", "op is required.");

            try
            {
                return op switch
                {
                    "validate_import" => Handlers.ValidateImportHandler.Execute(args),
                    "scene.list" => Handlers.SceneGraphHandler.List(args),
                    "scene.add" => Handlers.SceneGraphHandler.Add(args),
                    "scene.remove" => Handlers.SceneGraphHandler.Remove(args),
                    "scene.rename" => Handlers.SceneGraphHandler.Rename(args),
                    "scene.reparent" => Handlers.SceneGraphHandler.Reparent(args),
                    "scene.get_properties" => Handlers.SceneGraphHandler.GetProperties(args),
                    "scene.set_properties" => Handlers.SceneGraphHandler.SetProperties(args),
                    "scene.set_active" => Handlers.SceneGraphHandler.SetActive(args),
                    "component.list" => Handlers.SceneGraphHandler.ListComponents(args),
                    "component.add" => Handlers.SceneGraphHandler.AddComponent(args),
                    "component.remove" => Handlers.SceneGraphHandler.RemoveComponent(args),
                    "component.get" => Handlers.SceneGraphHandler.GetComponent(args),
                    "component.set" => Handlers.SceneGraphHandler.SetComponent(args),
                    "component.set_enabled" => Handlers.SceneGraphHandler.SetComponentEnabled(args),
                    "scene.attach_script" => Handlers.SceneGraphHandler.AttachScript(args),
                    "scene.instantiate_prefab" => Handlers.SceneGraphHandler.InstantiatePrefab(args),
                    "scene.save_as_prefab" => Handlers.SceneGraphHandler.SaveAsPrefab(args),
                    "scene.diff" => Handlers.SceneGraphHandler.Diff(args),
                    "asset.metadata" => Handlers.AssetHandler.GetMetadata(args),
                    "asset.move" => Handlers.AssetHandler.Move(args),
                    "asset.lint" => Handlers.AssetHandler.Lint(args),
                    "asset.update_material" => Handlers.AssetHandler.UpdateMaterial(args),
                    "asset.create_scriptable_object" => Handlers.AssetHandler.CreateScriptableObject(args),
                    "asset.read_serialized" => Handlers.AssetHandler.ReadSerialized(args),
                    "asset.update_serialized" => Handlers.AssetHandler.UpdateSerialized(args),
                    "native.create_animator" => Handlers.NativeSystemsHandler.CreateAnimator(args),
                    "native.create_timeline" => Handlers.NativeSystemsHandler.CreateTimeline(args),
                    "native.create_vfx" => Handlers.NativeSystemsHandler.CreateVfx(args),
                    "native.create_navmesh" => Handlers.NativeSystemsHandler.CreateNavMesh(args),
                    "native.create_input_actions" => Handlers.NativeSystemsHandler.CreateInputActions(args),
                    "native.create_ui" => Handlers.NativeSystemsHandler.CreateUi(args),
                    "domain.create_camera" => Handlers.DomainHandler.CreateCamera(args),
                    "domain.update_camera" => Handlers.DomainHandler.UpdateCamera(args),
                    "domain.validate_camera" => Handlers.DomainHandler.ValidateCameras(args),
                    "domain.create_light" => Handlers.DomainHandler.CreateLight(args),
                    "domain.update_light" => Handlers.DomainHandler.UpdateLight(args),
                    "domain.validate_light" => Handlers.DomainHandler.ValidateLights(args),
                    "domain.create_physics" => Handlers.DomainHandler.CreatePhysics(args),
                    "domain.update_physics" => Handlers.DomainHandler.UpdatePhysics(args),
                    "domain.validate_physics" => Handlers.DomainHandler.ValidatePhysics(args),
                    "ecosystem.resolve_packages" => Handlers.EcosystemHandler.ResolvePackages(args),
                    "ecosystem.search_packages" => Handlers.EcosystemHandler.SearchPackages(args),
                    "ecosystem.configure_project_settings" => Handlers.EcosystemHandler.ConfigureProjectSettings(args),
                    _ => Fail("Dispatch.UnknownOp", "Unknown operation: " + op),
                };
            }
            catch (Exception ex)
            {
                return Fail("Dispatch.Exception", ex.Message);
            }
        }

        public static string Ok(string message, object data = null)
            => MiniJson.Serialize(new Dictionary<string, object>
            {
                ["success"] = true,
                ["message"] = message,
                ["data"] = data,
                ["errors"] = new List<object>(),
                ["warnings"] = new List<object>(),
                ["suggestedRemediation"] = null,
            });

        public static string Fail(string code, string message, string remediation = null)
            => MiniJson.Serialize(new Dictionary<string, object>
            {
                ["success"] = false,
                ["message"] = message,
                ["data"] = null,
                ["errors"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["category"] = "ExternalTool",
                        ["code"] = code,
                        ["message"] = message,
                    }
                },
                ["warnings"] = new List<object>(),
                ["suggestedRemediation"] = remediation,
            });

        public static string GetString(Dictionary<string, object> args, string key, string defaultValue = "")
        {
            if (args == null || !args.ContainsKey(key) || args[key] == null)
                return defaultValue;
            return Convert.ToString(args[key]) ?? defaultValue;
        }

        public static bool GetBool(Dictionary<string, object> args, string key, bool defaultValue = false)
        {
            if (args == null || !args.ContainsKey(key) || args[key] == null)
                return defaultValue;
            return Convert.ToBoolean(args[key]);
        }

        public static int GetInt(Dictionary<string, object> args, string key, int defaultValue = 0)
        {
            if (args == null || !args.ContainsKey(key) || args[key] == null)
                return defaultValue;
            return Convert.ToInt32(args[key]);
        }
    }
}
