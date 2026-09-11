using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace REPOJP.StageRoles;

// Client-local presentation only; no network objects or extra client requirements.
internal static class RoleEmblems
{
    private const int TextureSize = 256;
    private static readonly Dictionary<string, Sprite?> Cache = new(StringComparer.Ordinal);

    internal static Sprite? Get(StageRole role, bool unrevealed = false)
    {
        int id = RoleCatalog.IsSecretRole(role) ? (int)role : (int)role + 1;
        string key = unrevealed ? "Unrevealed" : $"{id:D2}-{role}";
        if (Cache.TryGetValue(key, out Sprite? cached)) return cached;
        Texture2D? texture = null;
        try
        {
            string resource = $"REPOJP.StageRoles.Emblems.{key}.png";
            using Stream? stream = typeof(RoleEmblems).Assembly.GetManifestResourceStream(resource);
            if (stream == null) throw new InvalidDataException($"Missing emblem: {key}");
            using MemoryStream bytes = new();
            stream.CopyTo(bytes);
            texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(texture, bytes.ToArray(), true))
                throw new InvalidDataException($"Invalid emblem: {key}");
            if (texture.width != TextureSize || texture.height != TextureSize)
                throw new InvalidDataException($"Expected {TextureSize}x{TextureSize} emblem: {key}");
            // Runtime PNGs are already sized and alpha-resampled offline. Keep
            // their transparency directly, without a render-target round trip.
            texture.name = $"RoleShuffle_Emblem_{key}";
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, TextureSize, TextureSize),
                new Vector2(0.5f, 0.5f), 100f);
            sprite.name = texture.name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            Cache[key] = sprite;
            texture = null; // The cached sprite owns this texture until Shutdown.
            return sprite;
        }
        catch (Exception exception)
        {
            Cache[key] = null; // Log once and keep the original text-only UI usable.
            StageRolesPlugin.ModLogger.LogWarning($"Could not load {key} emblem: {exception.Message}");
            return null;
        }
        finally
        {
            if (texture != null) UnityEngine.Object.Destroy(texture);
        }
    }

    internal static void Shutdown()
    {
        foreach (Sprite? sprite in Cache.Values)
        {
            if (sprite == null) continue;
            UnityEngine.Object.Destroy(sprite.texture);
            UnityEngine.Object.Destroy(sprite);
        }
        Cache.Clear();
    }
}
