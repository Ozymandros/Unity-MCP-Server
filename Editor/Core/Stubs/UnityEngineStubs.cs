#if !UNITY_EDITOR
using System;

namespace UnityEngine
{
    public static class Debug
    {
        public static void Log(object message) => Console.WriteLine($"[Log] {message}");
        public static void LogWarning(object message) => Console.WriteLine($"[Warning] {message}");
        public static void LogError(object message) => Console.Error.WriteLine($"[Error] {message}");
    }

    public class MonoBehaviour { }

    [AttributeUsage(AttributeTargets.Field)]
    public class SerializeField : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public class RuntimeInitializeOnLoadMethodAttribute : Attribute
    {
        public RuntimeInitializeLoadType LoadType { get; set; }
        public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType type) { LoadType = type; }
        public RuntimeInitializeOnLoadMethodAttribute() { }
    }

    public enum RuntimeInitializeLoadType { AfterSceneLoad, BeforeSceneLoad }
}

namespace UnityEditor
{
    public class EditorWindow : UnityEngine.MonoBehaviour
    {
        public static T GetWindow<T>(string title) where T : EditorWindow => null;
        public void Show() { }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class InitializeOnLoadAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public class InitializeOnLoadMethodAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public class MenuItemAttribute : Attribute
    {
        public MenuItemAttribute(string path) { }
    }
}
#endif
