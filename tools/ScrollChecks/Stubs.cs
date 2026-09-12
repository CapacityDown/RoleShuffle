namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.Class)] public sealed class HarmonyPatch : Attribute
    {
        public HarmonyPatch(Type type, string method) { }
    }
    [AttributeUsage(AttributeTargets.Method)] public sealed class HarmonyPrefix : Attribute { }
    [AttributeUsage(AttributeTargets.Method)] public sealed class HarmonyFinalizer : Attribute { }
}
namespace UnityEngine
{
    public struct Vector2 { public float y; }
    public struct Rect { public float height; }
    public class RectTransform { public Vector2 sizeDelta; public Rect rect; }
    public static class Input { public static Vector2 mouseScrollDelta; }
    public static class Mathf
    {
        public static bool Approximately(float a, float b) => Math.Abs(a - b) < 0.00001f;
        public static float Clamp(float value, float min, float max) => Math.Clamp(value, min, max);
    }
}
namespace MenuLib.MonoBehaviors
{
    public class REPOScrollView { public float? scrollSpeed = 3; }
}
public class MenuScrollBox
{
    public UnityEngine.RectTransform scrollHandle = new() { sizeDelta = new() { y = 40 } };
    public UnityEngine.RectTransform scrollBarBackground = new() { rect = new() { height = 1000 } };
}
public static class SemiFunc
{
    public static float Scroll;
    public static float InputScrollY() => Scroll;
}
namespace REPOJP.StageRoles
{
    internal static class RoleMenu
    {
        internal static MenuScrollBox Box = new();
        internal static MenuLib.MonoBehaviors.REPOScrollView View = new();
        internal static float Multiplier = 30;
        internal static bool Open = true;
        internal static bool TryGetScrollSettings(MenuScrollBox box, out MenuLib.MonoBehaviors.REPOScrollView view, out float multiplier)
        {
            view = View; multiplier = Multiplier;
            return Open && ReferenceEquals(Box, box);
        }
    }
}
