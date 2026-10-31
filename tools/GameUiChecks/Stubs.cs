namespace UnityEngine
{
    public enum HideFlags { HideAndDontSave }
    public class Texture2D { public int width = 2048, height = 1024; }
    public class Shader
    {
        public static Shader? Available = new();
        public static Shader? Find(string name) => Available;
    }
    public class Material
    {
        public static int Created;
        public Material(Shader shader) { this.shader = shader; Created++; }
        public string name = ""; public HideFlags hideFlags;
        public Shader? shader; public Texture2D? mainTexture;
        public Dictionary<string,float> Values = new();
        public void SetFloat(string key, float value) => Values[key] = value;
    }
}
namespace TMPro
{
    public class TMP_FontAsset
    {
        public string name = "Old bundled Japanese font";
        public UnityEngine.Texture2D? atlasTexture;
        public UnityEngine.Material? material;
        public int atlasPadding = 5;
        public float normalStyle = 0, boldStyle = .75f;
        public int Definitions;
        public void ReadFontAssetDefinition() { if (material?.mainTexture == null) throw new InvalidOperationException("Missing material"); Definitions++; }
    }
    public static class TMP_Settings { public static TMP_FontAsset? defaultFontAsset; }
}
public class LocaleAsset { public bool RightToLeft; }
public class LocalizationManager
{
    public static LocalizationManager? instance;
    public LocaleAsset? ActiveLocale;
}
public static class SemiFunc
{
    public static string Locale = "en-US";
    public static string LocaleGetCode() => Locale;
}
public class LocalizedAsset
{
    public Dictionary<string,string> Values = new();
    public bool Throw; public int Calls;
    public string? GetLocalizedString() { Calls++; if (Throw) throw new InvalidOperationException("Tables not ready"); return Values.GetValueOrDefault(SemiFunc.Locale); }
}
public class StatsManager
{
    public static StatsManager? instance;
    public Dictionary<string,UpgradeInfo> upgradesInfo = new();
    public class UpgradeInfo { public LocalizedAsset? displayNameLocalized; }
}
