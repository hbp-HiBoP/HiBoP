using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Tools.Unity.ResizableGrid;

public class SceneUIDBG : MonoBehaviour {
    public HBP.Module3D.Base3DScene Scene { get; set; }

    private void Awake()
    {
        ApplicationState.Module3D.ScenesManager.OnAddScene.AddListener((scene) =>
        {
            Scene = scene;
            for (int i = 0; i < Scene.Column3DViewManager.Columns.Count; i++)
            {
                AddColumn();
            }
            GetComponent<ResizableGrid>().AddViewLine();
            for (int i = 0; i < GetComponent<ResizableGrid>().Columns.Count; i++)
            {
                GetComponent<ResizableGrid>().Columns[i].Views.Last().GetComponent<ViewUIDBG>().AssociatedView = Scene.Column3DViewManager.Columns[i].Views.Last();
            }
        });
    }

    public void AddColumn()
    {
        GetComponent<ResizableGrid>().AddColumn();
    }
    
    public void AddView()
    {
        for (int i = 0; i < Scene.Column3DViewManager.Columns.Count; i++)
        {
            Scene.Column3DViewManager.Columns[i].AddView();
        }
        GetComponent<ResizableGrid>().AddViewLine();
        for (int i = 0; i < GetComponent<ResizableGrid>().Columns.Count; i++)
        {
            GetComponent<ResizableGrid>().Columns[i].Views.Last().GetComponent<ViewUIDBG>().AssociatedView = Scene.Column3DViewManager.Columns[i].Views.Last();
        }
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            AddView();
        }
    }
}
