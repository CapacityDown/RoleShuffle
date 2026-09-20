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

    internal static Sprite? Get(StageRole role, bool unrevealed = false, bool grayedOut = false)
    {
        int id = RoleCatalog.IsSecretRole(role) ? (int)role : (int)role + 1;
        string key = unrevealed ? "Unrevealed" : $"{id:D2}-{(role == StageRole.Jobless ? "Courier" : role.ToString())}";
        string cacheKey = grayedOut ? key + "_Disabled" : key;
        if (Cache.TryGetValue(cacheKey, out Sprite? cached)) return cached;
        Texture2D? texture = null;
        try
        {
            string resource = $"REPOJP.StageRoles.Emblems.{key}.png";
            using Stream? stream = typeof(RoleEmblems).Assembly.GetManifestResourceStream(resource);
            if (stream == null) throw new InvalidDataException($"Missing emblem: {key}");
            using MemoryStream bytes = new();
            stream.CopyTo(bytes);
            texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(texture, bytes.ToArray(), !grayedOut))
                throw new InvalidDataException($"Invalid emblem: {key}");
            if (texture.width != TextureSize || texture.height != TextureSize)
                throw new InvalidDataException($"Expected {TextureSize}x{TextureSize} emblem: {key}");
            if (grayedOut)
            {
                // Create each disabled variant once, preserving the PNG alpha.
                // The normal sprite stays unchanged for HUD and guide use.
                Color32[] pixels = texture.GetPixels32();
                for (int i = 0; i < pixels.Length; i++)
                {
                    Color32 pixel = pixels[i];
                    byte gray = (byte)((77 * pixel.r + 150 * pixel.g + 29 * pixel.b) * 7 / 2560);
                    pixels[i] = new Color32(gray, gray, gray, pixel.a);
                }
                texture.SetPixels32(pixels);
                texture.Apply(false, true);
            }
            // Runtime PNGs are already sized and alpha-resampled offline. Keep
            // their transparency directly, without a render-target round trip.
            texture.name = $"RoleShuffle_Emblem_{cacheKey}";
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, TextureSize, TextureSize),
                new Vector2(0.5f, 0.5f), 100f);
            sprite.name = texture.name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            Cache[cacheKey] = sprite;
            texture = null; // The cached sprite owns this texture until Shutdown.
            return sprite;
        }
        catch (Exception exception)
        {
            Cache[cacheKey] = null; // Log once and keep the original text-only UI usable.
            StageRolesPlugin.ModLogger.LogWarning($"Could not load {cacheKey} emblem: {exception.Message}");
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
