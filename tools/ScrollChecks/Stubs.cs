namespace UnityEngine
{
    public struct Vector2 { public float y; }
    public struct Rect { public float height; }
    public class RectTransform { public Vector2 sizeDelta; public Rect rect; }
    public static class Input { public static Vector2 mouseScrollDelta; }
    public static class Mathf
    {
        public static float Abs(float value) => Math.Abs(value);
        public static bool Approximately(float a, float b) => Math.Abs(a - b) < 0.00001f;
        public static float Clamp(float value, float min, float max) => Math.Clamp(value, min, max);
    }
}
public class MenuScrollBox
{
    public float scrollHeight = 1000;
    public float scrollerStartPosition = 1000;
    public float scrollerEndPosition = 0;
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
        internal static float LineHeight = 32.5f;
        internal static bool Open = true;
        internal static bool TryGetScrollSettings(MenuScrollBox box, out float lineHeight)
        {
            lineHeight = LineHeight;
            return Open && ReferenceEquals(Box, box);
        }
    }
}
