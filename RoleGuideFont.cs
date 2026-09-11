using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace REPOJP.StageRoles;

internal static class RoleGuideFont
{
    private const string ResourceName =
        "REPOJP.StageRoles.Assets.Fonts.CheckpointRevenge";
    private const float JapaneseGlyphScale = 0.72f;

    private static bool _loadAttempted;
    private static TMP_FontAsset? _font;
    private static TMP_FontAsset? _scaledJapaneseFont;
    private static readonly Dictionary<int, TMP_FontAsset> MixedFonts = new();

    internal static TMP_FontAsset? Font
    {
        get
        {
            if (!_loadAttempted)
            {
                _loadAttempted = true;
                _font = Load();
            }
            return _font;
        }
    }

    internal static TMP_FontAsset? ForPrimary(TMP_FontAsset? primary)
    {
        if (primary == null)
        {
            return Font;
        }

        TMP_FontAsset? japanese = ScaledJapaneseFont();
        if (japanese == null)
        {
            return primary;
        }

        int key = primary.GetInstanceID();
        if (MixedFonts.TryGetValue(key, out TMP_FontAsset mixed) &&
            mixed != null)
        {
            return mixed;
        }

        mixed = UnityEngine.Object.Instantiate(primary);
        mixed.name = $"{primary.name} + Checkpoint Revenge";
        mixed.hideFlags = HideFlags.HideAndDontSave;
        List<TMP_FontAsset> fallbacks = primary.fallbackFontAssetTable != null
            ? new List<TMP_FontAsset>(primary.fallbackFontAssetTable)
            : new List<TMP_FontAsset>();
        fallbacks.Remove(japanese);
        fallbacks.Insert(0, japanese);
        mixed.fallbackFontAssetTable = fallbacks;
        MixedFonts[key] = mixed;
        return mixed;
    }

    private static TMP_FontAsset? ScaledJapaneseFont()
    {
        if (_scaledJapaneseFont != null)
        {
            return _scaledJapaneseFont;
        }

        TMP_FontAsset? source = Font;
        if (source == null)
        {
            return null;
        }

        _scaledJapaneseFont = UnityEngine.Object.Instantiate(source);
        _scaledJapaneseFont.name = $"{source.name} Role Guide Scaled";
        _scaledJapaneseFont.hideFlags = HideFlags.HideAndDontSave;
        var faceInfo = _scaledJapaneseFont.faceInfo;
        faceInfo.scale *= JapaneseGlyphScale;
        _scaledJapaneseFont.faceInfo = faceInfo;
        return _scaledJapaneseFont;
    }

    private static TMP_FontAsset? Load()
    {
        try
        {
            Assembly assembly = typeof(RoleGuideFont).Assembly;
            using Stream? stream =
                assembly.GetManifestResourceStream(ResourceName);
            if (stream == null)
            {
                StageRolesPlugin.ModLogger.LogWarning(
                    "The embedded Checkpoint Revenge font was not found.");
                return null;
            }

            byte[] bytes = ReadAllBytes(stream);
            AssetBundle? bundle = AssetBundle.LoadFromMemory(bytes);
            if (bundle == null)
            {
                StageRolesPlugin.ModLogger.LogWarning(
                    "The Checkpoint Revenge font bundle could not be loaded.");
                return null;
            }

            try
            {
                TMP_FontAsset[] fontAssets =
                    bundle.LoadAllAssets<TMP_FontAsset>();
                TMP_FontAsset? selected = Preferred(fontAssets);
                if (selected == null)
                {
                    Font[] sourceFonts = bundle.LoadAllAssets<Font>();
                    Font? source = Preferred(sourceFonts);
                    if (source != null)
                    {
                        selected = TMP_FontAsset.CreateFontAsset(source);
                    }
                }

                if (selected == null)
                {
                    StageRolesPlugin.ModLogger.LogWarning(
                        "The Checkpoint Revenge bundle contained no usable font asset.");
                    return null;
                }

                selected.hideFlags = HideFlags.HideAndDontSave;
                StageRolesPlugin.ModLogger.LogInfo(
                    $"Loaded Japanese Role Guide font: {selected.name}.");
                return selected;
            }
            finally
            {
                bundle.Unload(unloadAllLoadedObjects: false);
            }
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogWarning(
                $"The Checkpoint Revenge font could not be loaded: " +
                exception.Message);
            return null;
        }
    }

    private static byte[] ReadAllBytes(Stream stream)
    {
        using MemoryStream copy = new();
        stream.CopyTo(copy);
        return copy.ToArray();
    }

    private static T? Preferred<T>(T[] assets)
        where T : UnityEngine.Object
    {
        foreach (T asset in assets)
        {
            if (asset != null &&
                (asset.name.IndexOf(
                     "checkpoint",
                     StringComparison.OrdinalIgnoreCase) >= 0 ||
                 asset.name.IndexOf(
                     "revenge",
                     StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return asset;
            }
        }
        return assets.Length > 0 ? assets[0] : null;
    }
}
