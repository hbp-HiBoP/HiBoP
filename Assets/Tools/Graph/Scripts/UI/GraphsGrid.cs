using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Tools.Unity.Graph
{
    public class GraphsGrid : MonoBehaviour
    {
        #region Properties
        [SerializeField]
        GameObject graphPrefab;
        RectTransform container;
        GridLayoutGroup gridLayout;
        #endregion

        #region Public Methods
        public void Plot(GraphData[] graphs)
        {
            StartCoroutine(c_Plot(graphs));
        }
        #endregion

        #region Private Methods
        IEnumerator c_Plot(GraphData[] graphs)
        {
            foreach (GraphData graph in graphs)
            {
                GameObject gameObject = Instantiate(graphPrefab, container);
                yield return true;
                gameObject.GetComponent<Graph>().Plot(graph);
            }
        }
        void Plot(GraphData graph)
        {
            GameObject gameObject = Instantiate(graphPrefab, container);
            gameObject.GetComponent<Graph>().Plot(graph);
        }
        void Clear()
        {
            while (container.transform.childCount > 0)
            {
                Destroy(container.transform.GetChild(0).gameObject);
            }
        }        
        void Awake()
        {
            container = GetComponent<RectTransform>();
            gridLayout = GetComponent<GridLayoutGroup>();
        }
        void OnRectTransformDimensionsChange()
        {
            if (container != null)
            {
                float size = container.rect.width / 2;
                gridLayout.cellSize = size * Vector2.one;
            }
        }
        #endregion
    }
}