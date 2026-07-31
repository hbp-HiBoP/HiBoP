using UnityEngine;
using data = HBP.Data.Informations.TrialMatrix;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine.UI;

namespace HBP.UI.Database
{
    public class TrialMatrixGrid : MonoBehaviour
    {
        #region Properties

        Color[] m_Colors;

        private Color[] Colors
        {
            get { return m_Colors; }
            set
            {
                m_Colors = value;
                foreach (var data in m_Data)
                {
                    data.Colors = value;
                }
            }
        }

        [SerializeField] Texture2D m_Colormap;

        public Texture2D Colormap
        {
            get { return m_Colormap; }
            set
            {
                m_Colormap = value;
                foreach (var data in m_Data)
                {
                    data.Colormap = value;
                }

                Colors = ExtractColormap(value);
            }
        }

        [SerializeField] RectTransform m_DataContainer;
        [SerializeField] GameObject m_DataPrefab;

        [SerializeField] RectTransform m_TitleHeaderContainer;
        [SerializeField] GameObject m_TitleHeaderPrefab;

        List<Informations.TrialMatrix.Data> m_Data = new();

        public ReadOnlyCollection<Informations.TrialMatrix.Data> Data
        {
            get { return new ReadOnlyCollection<Informations.TrialMatrix.Data>(m_Data); }
        }

        data.TrialMatrixGrid m_TrialMatrixGridData;

        #endregion

        #region Public Methods

        public void Display(data.TrialMatrixGrid trialMatrixGridData, string title, Texture2D colormap = null)
        {
            Clear();
            m_TrialMatrixGridData = trialMatrixGridData;
            DisplayTitle(title);
            if (colormap != null) Colormap = colormap;
            foreach (var data in trialMatrixGridData.Data) AddData(data);
        }

        public void DisplayTitle(string title)
        {
            GameObject header = Instantiate(m_TitleHeaderPrefab, m_TitleHeaderContainer);
            header.GetComponentInChildren<Text>().text = title;
        }

        #endregion

        #region Private Methods

        void AddData(data.Data d)
        {
            Informations.TrialMatrix.Data data = Instantiate(m_DataPrefab, m_DataContainer).GetComponent<Informations.TrialMatrix.Data>();
            data.Set(d, m_Colormap, m_Colors);
            m_Data.Add(data);
        }

        Color[] ExtractColormap(Texture2D colormap)
        {
            Color[] colors = new Color[colormap.width];
            for (int x = 0; x < colormap.width; x++)
            {
                colors[x] = colormap.GetPixel(x, 0);
            }

            return colors;
        }

        void Clear()
        {
            foreach (Transform child in m_DataContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (Transform child in m_TitleHeaderContainer)
            {
                Destroy(child.gameObject);
            }

            m_Data = new List<Informations.TrialMatrix.Data>();
        }

        #endregion
    }
}
