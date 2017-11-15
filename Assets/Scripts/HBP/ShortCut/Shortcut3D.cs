using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shortcut3D : MonoBehaviour
{
    enum Direction { Left, Right}
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update ()
    {
		if(Input.GetKeyDown(KeyCode.LeftArrow))
        {
            ChangeSiteSelection(Direction.Left);
        }
        else if(Input.GetKeyDown(KeyCode.RightArrow))
        {
            ChangeSiteSelection(Direction.Right);
        }
	}

    void ChangeSiteSelection(Direction direction)
    {
        Base3DScene scene = ApplicationState.Module3D.SelectedScene;
        if (scene != null)
        {
            Column3D selectedColumn = scene.ColumnManager.SelectedColumn;
            int selectedId = selectedColumn.SelectedSiteID;
            if (selectedId != -1)
            {
                Site site;
                int id = selectedId;
                int count = 0;
                do
                {
                    switch (direction)
                    {
                        case Direction.Left:
                            id--;
                            if (id < 0) id = selectedColumn.Sites.Count - 1;
                            break;
                        case Direction.Right:
                            id++;
                            if (id > selectedColumn.Sites.Count - 1) id = 0;
                            break;
                    }
                    site = selectedColumn.Sites[id];
                }
                while (!site.State.IsDisplayed && ++count <= selectedColumn.Sites.Count);
                selectedColumn.SelectedSiteID = id;
            }
        }
    }
}
