using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shortcut3D : MonoBehaviour
{
    enum Direction { Left, Right }
    const float DELAY = 0.2f;
    float m_Timer = 0.0f;

	void Update ()
    {
        m_Timer += Time.deltaTime;
		if((Input.GetKey(KeyCode.LeftArrow) && m_Timer >= DELAY) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            m_Timer = 0;
            ChangeSiteSelection(Direction.Left);
        }
        else if((Input.GetKey(KeyCode.RightArrow) && m_Timer >= DELAY) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            m_Timer = 0;
            ChangeSiteSelection(Direction.Right);
        }

        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            ChangeSelectedSiteState();
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

    void ChangeSelectedSiteState()
    {
        Base3DScene scene = ApplicationState.Module3D.SelectedScene;
        if (scene)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                scene.InvertSelectedSiteState(SiteAction.Include);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                scene.InvertSelectedSiteState(SiteAction.Highlight);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                scene.InvertSelectedSiteState(SiteAction.Blacklist);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                scene.InvertSelectedSiteState(SiteAction.Mark);
            }
        }
    }
}
