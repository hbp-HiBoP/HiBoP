using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

[RequireComponent(typeof(LayoutElement))]
public class TabsManager : MonoBehaviour
{
    public float Speed;
    Tab[] m_Tabs;
    List<Tab> m_OpenedTabs;
    Queue<Tab> m_TabsToClose;
    Queue<Tab> m_TabsToOpen;
    LayoutElement m_LayoutElement;
    VerticalLayoutGroup m_VerticalLayoutGroup;
    Fading m_Fading;
    bool m_Dangerous;

    private void Start()
    {
        m_Fading = GetComponent<Fading>();
        m_LayoutElement = GetComponent<LayoutElement>();
        m_VerticalLayoutGroup = GetComponent<VerticalLayoutGroup>();
        m_TabsToOpen = new Queue<Tab>();
        m_TabsToClose = new Queue<Tab>();
        m_OpenedTabs = new List<Tab>();
        m_Tabs = GetComponentsInChildren<Tab>(true);
        foreach (Tab tab in m_Tabs)
        {
            tab.OnClose.AddListener(() =>
            {
                    m_TabsToClose.Enqueue(tab);
            });
            tab.OnOpen.AddListener(() =>
            {
                    m_TabsToOpen.Enqueue(tab);
            }); 
        }
    }

    void Update()
    {
        if ((m_TabsToOpen.Count != 0 || m_TabsToClose.Count != 0) && !m_Dangerous)
        {
            StopAllCoroutines();
            StartCoroutine(Change(m_TabsToOpen, m_TabsToClose));
            m_TabsToOpen = new Queue<Tab>();
            m_TabsToClose = new Queue<Tab>();
        }
    }

    IEnumerator Change(IEnumerable<Tab> tabToOpen, IEnumerable<Tab> tabToClose)
    {
        m_Dangerous = true;
        m_VerticalLayoutGroup.enabled = false;
        m_LayoutElement.enabled = true;

        // CLOSE
        List<Graphic> graphicsToFade = new List<Graphic>();
        foreach (Tab tab in tabToClose)
        {
            m_OpenedTabs.Remove(tab);
            foreach (Graphic graphic in tab.Graphics)
            {
                graphicsToFade.Add(graphic);
            }
        }
        yield return m_Fading.Fade(graphicsToFade.ToArray(), 1, 0, 0.15f);
        foreach (var tab in tabToClose)
        {
            tab.gameObject.SetActive(false);
        }
        m_Dangerous = false;

        // CHANGE SIZE
        float height = transform.GetComponent<RectTransform>().rect.height;

        float finalHeight = m_OpenedTabs.Sum((tab) => tab.Height) + tabToOpen.Sum((tab) => tab.Height) + m_VerticalLayoutGroup.spacing * (tabToOpen.Count() + m_OpenedTabs.Count() - 1 );
        if(finalHeight > height)
        {
            while(height < finalHeight)
            {
                float movement = Speed * Time.deltaTime;
                if(height + movement > finalHeight)
                {
                    movement = finalHeight - height;
                }
                height += movement;
                foreach (var item in m_OpenedTabs)
                {
                    item.GetComponent<RectTransform>().position += new Vector3(0, -movement, 0);
                }
                m_LayoutElement.preferredHeight = height;
                yield return null;
            }
        }
        else
        {
            while (height > finalHeight)
            {
                float movement = - Speed * Time.deltaTime;
                if (height + movement < finalHeight)
                {
                    movement = finalHeight - height;
                }
                height += movement;
                foreach (var item in m_OpenedTabs.Where((tab) => tabToClose.Any((tab2) => tab2.transform.GetSiblingIndex() < tab.transform.GetSiblingIndex())))
                {
                    item.GetComponent<RectTransform>().position += new Vector3(0, -movement, 0);
                }
                m_LayoutElement.preferredHeight = height;
                yield return null;
            }
        }

        // OPEN
        graphicsToFade = new List<Graphic>();
        foreach (Tab tab in tabToOpen)
        {
            foreach (Graphic graphic in tab.Graphics)
            {
                graphicsToFade.Add(graphic);
            }
        }
        foreach (var tab in tabToOpen)
        {
            m_OpenedTabs.Add(tab);
            tab.gameObject.SetActive(true);
            tab.transform.SetAsFirstSibling();
        }
        yield return m_Fading.Fade(graphicsToFade.ToArray(), 0, 1, 0.15f);


        // FINISH
        m_LayoutElement.enabled = false;
        m_VerticalLayoutGroup.enabled = true;
    }
}
