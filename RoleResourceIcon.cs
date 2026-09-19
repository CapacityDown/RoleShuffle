using UnityEngine;
using UnityEngine.UI;

namespace REPOJP.StageRoles;

internal sealed class RoleResourceIcon : MaskableGraphic
{
    private AbilityMetric _metric = (AbilityMetric)(-1);
    internal void SetMetric(AbilityMetric metric)
    {
        if (_metric == metric) return;
        _metric = metric;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper helper)
    {
        helper.Clear();
        Rect rect = rectTransform.rect;
        var points = RoleResourceSymbols.Get(_metric);
        for (int i = 0; i < points.Count; i++)
        {
            var p = points[i];
            Color tint = color;
            tint.a *= p.Alpha;
            helper.AddVert(new Vector3(rect.xMin+p.X/25*rect.width,rect.yMax-p.Y/25*rect.height,0),tint,Vector2.zero);
            if (i % 3 == 2) helper.AddTriangle(i-2,i-1,i);
        }
    }
}
