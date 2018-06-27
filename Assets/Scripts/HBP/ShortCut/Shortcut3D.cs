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
                while ((!site.IsActive || site.State.IsBlackListed) && ++count <= selectedColumn.Sites.Count);
                site.IsSelected = true;
            }
        }
    }

    void ChangeSelectedSiteState()
    {
        Base3DScene scene = ApplicationState.Module3D.SelectedScene;
        if (scene)
        {
            Column3D column = scene.ColumnManager.SelectedColumn;
            if (column)
            {
                Site site = column.SelectedSite;
                if (site)
                {
                    if (Input.GetKeyDown(KeyCode.Alpha1))
                    {
                        if (site.State.IsExcluded)
                        {
                            scene.ChangeSiteState(HBP.Data.Enums.SiteAction.Include);
                        }
                        else
                        {
                            scene.ChangeSiteState(HBP.Data.Enums.SiteAction.Exclude);
                        }
                    }
                    else if (Input.GetKeyDown(KeyCode.Alpha2))
                    {
                        if (site.State.IsHighlighted)
                        {
                            scene.ChangeSiteState(HBP.Data.Enums.SiteAction.Unhighlight);
                        }
                        else
                        {
                            scene.ChangeSiteState(HBP.Data.Enums.SiteAction.Highlight);
                        }
                    }
                    else if (Input.GetKeyDown(KeyCode.Alpha3))
                    {
                        if (site.State.IsBlackListed)
                        {
                            scene.ChangeSiteState(HBP.Data.Enums.SiteAction.Unblacklist);
                        }
                        else
                        {
                            scene.ChangeSiteState(HBP.Data.Enums.SiteAction.Blacklist);
                        }
                    }
                    else if (Input.GetKeyDown(KeyCode.Alpha4))
                    {
                        if (site.State.IsMarked)
                        {
                            scene.ChangeSiteState(HBP.Data.Enums.SiteAction.Unmark);
                        }
                        else
                        {
                            scene.ChangeSiteState(HBP.Data.Enums.SiteAction.Mark);
                        }
                    }
                    else if (Input.GetKeyDown(KeyCode.Alpha5))
                    {
                        if (site.State.IsSuspicious)
                        {
                            scene.ChangeSiteState(HBP.Data.Enums.SiteAction.Unsuspect);
                        }
                        else
                        {
                            scene.ChangeSiteState(HBP.Data.Enums.SiteAction.Suspect);
                        }
                    }
                }
            }
        }
    }
}
