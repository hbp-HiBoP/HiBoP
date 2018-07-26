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
                Site selectedSite = column.SelectedSite;
                if (selectedSite)
                {
                    List<Site> sites = new List<Site>();
                    if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    {
                        foreach (Transform siteTransform in selectedSite.transform.parent)
                        {
                            sites.Add(siteTransform.GetComponent<Site>());
                        }
                    }
                    else if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                    {
                        foreach (Transform electrode in selectedSite.transform.parent.parent)
                        {
                            foreach (Transform siteTransform in electrode)
                            {
                                sites.Add(siteTransform.GetComponent<Site>());
                            }
                        }
                    }
                    else
                    {
                        sites.Add(selectedSite);
                    }
                    if (Input.GetKeyDown(KeyCode.Alpha1))
                    {
                        foreach (var site in sites) site.State.IsExcluded = !site.State.IsExcluded;
                    }
                    else if (Input.GetKeyDown(KeyCode.Alpha2))
                    {
                        foreach (var site in sites) site.State.IsHighlighted = !site.State.IsHighlighted;
                    }
                    else if (Input.GetKeyDown(KeyCode.Alpha3))
                    {
                        foreach (var site in sites) site.State.IsBlackListed = !site.State.IsBlackListed;
                    }
                    else if (Input.GetKeyDown(KeyCode.Alpha4))
                    {
                        foreach (var site in sites) site.State.IsMarked = !site.State.IsMarked;
                    }
                    else if (Input.GetKeyDown(KeyCode.Alpha5))
                    {
                        foreach (var site in sites) site.State.IsSuspicious = !site.State.IsSuspicious;
                    }
                }
            }
        }
    }
}
