using HBP.Module3D;
using Tools.Unity.ResizableGrid;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViewUIDBG : MonoBehaviour {
    public View3D AssociatedView { get; set; }

    private void Update()
    {
        Rect viewport = RectTransformToScreenSpace(GetComponent<RectTransform>());
        AssociatedView.SetViewport(viewport.x, viewport.y, viewport.width, viewport.height);
    }

    private Rect RectTransformToScreenSpace(RectTransform transform)
    {
        Vector2 size = Vector2.Scale(transform.rect.size, transform.lossyScale);
        return new Rect((Vector2)transform.position - (size * 0.5f), size);
    }
}
