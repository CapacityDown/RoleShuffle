using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace REPOJP.StageRoles;

internal sealed class RoleResourceIcon : MaskableGraphic
{
    private AbilityMetric _metric = (AbilityMetric)(-1);
    private static readonly Dictionary<AbilityMetric, Texture2D> Textures = new();
    private Texture2D? _texture;
    public override Texture mainTexture => _texture != null ? _texture : Texture2D.whiteTexture;

    internal void SetMetric(AbilityMetric metric)
    {
        if (_metric == metric) return;
        _metric = metric;
        if (!Textures.TryGetValue(metric, out _texture))
        {
            const int size = RoleResourceRaster.Size;
            byte[] alpha = RoleResourceRaster.Alpha(metric);
            Color32[] pixels = new Color32[alpha.Length];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                pixels[(size - 1 - y) * size + x] = new Color32(255, 255, 255, alpha[y * size + x]);
            _texture = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                name = "RoleShuffle_HUD_" + metric, hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Trilinear, wrapMode = TextureWrapMode.Clamp
            };
            _texture.SetPixels32(pixels);
            _texture.Apply(true, true);
            Textures[metric] = _texture;
        }
        SetMaterialDirty();
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper helper)
    {
        helper.Clear();
        Rect rect = rectTransform.rect;
        helper.AddVert(new Vector3(rect.xMin, rect.yMin, 0), color, new Vector2(0, 0));
        helper.AddVert(new Vector3(rect.xMin, rect.yMax, 0), color, new Vector2(0, 1));
        helper.AddVert(new Vector3(rect.xMax, rect.yMax, 0), color, new Vector2(1, 1));
        helper.AddVert(new Vector3(rect.xMax, rect.yMin, 0), color, new Vector2(1, 0));
        helper.AddTriangle(0, 1, 2); helper.AddTriangle(2, 3, 0);
    }
}
