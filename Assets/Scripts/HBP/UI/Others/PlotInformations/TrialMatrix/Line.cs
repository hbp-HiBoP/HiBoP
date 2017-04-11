using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;
using d = HBP.Data.TrialMatrix;

namespace HBP.UI.TrialMatrix
{
    public class Line : MonoBehaviour
    {
        #region Properties
        [SerializeField]
        GameObject pref_Bloc;

        Image illustration;
        Text label;
        RectTransform rect;

        List<Bloc> blocs = new List<Bloc>();
        public Bloc[] Blocs { get { return blocs.ToArray(); } }
        #endregion

        #region Public Methods
        public void Set(d.Bloc[] blocs,int max,Texture2D colorMap,Vector2 limits)
        {
            //Set illustration/label
            if (blocs.Length > 0)
            {
                SetIllustration(blocs[0]);
            }

            //Add blocs
            foreach (d.Bloc bloc in blocs)
            {
                AddBloc(bloc, colorMap,limits);
            }

            if(blocs.Length < max)
            {
                int blocEmptyToAdd = max - blocs.Length;
                for (int i = 0; i < blocEmptyToAdd; i++)
                {
                    AddEmpty();
                }
            }
        }

        public void SelectLines(int[] lines, Data.Experience.Protocol.Bloc bloc,bool additive)
        {
            foreach(Bloc l_bloc in blocs)
            {
                if(l_bloc.Data.PBloc == bloc)
                {
                    l_bloc.SelectLines(lines,additive);
                }
            }
        }
        #endregion

        #region Private Methods
        void Awake()
        {
            rect = transform.FindChild("Blocs").GetComponent<RectTransform>();
            label = transform.FindChild("Title").FindChild("Label").GetComponent<Text>();
            illustration = transform.FindChild("Title").FindChild("Image").GetComponent<Image>();
        }

        void SetIllustration(d.Bloc bloc)
        {
            string l_illustrationPath = bloc.PBloc.DisplayInformations.IllustrationPath;
            if(l_illustrationPath != string.Empty)
            {
                FileInfo l_file = new FileInfo(l_illustrationPath);
                if (l_file.Exists && (l_file.Extension == ".png" || l_file.Extension == ".jpg"))
                {
                    byte[] l_bytes = File.ReadAllBytes(l_illustrationPath);
                    Texture2D l_texture = new Texture2D(0, 0);
                    l_texture.LoadImage(l_bytes);
                    illustration.sprite = Sprite.Create(l_texture, new Rect(0, 0, l_texture.width, l_texture.height), new Vector2(0.5f, 0.5f));
                    illustration.gameObject.SetActive(true);
                }
                else
                {
                    if (label != null) //FIXME : temporary solution
                    {
                        label.gameObject.SetActive(true);
                        illustration.gameObject.SetActive(false);
                        label.text = bloc.PBloc.DisplayInformations.Name;
                    }
                }
            }
            else
            {
                if (label != null) //FIXME : temporary solution
                {
                    label.gameObject.SetActive(true);
                    illustration.gameObject.SetActive(false);
                    label.text = bloc.PBloc.DisplayInformations.Name;
                }
            }
        }

        void AddBloc(d.Bloc bloc,Texture2D colorMap,Vector2 limits)
        {
            Bloc instantiateBloc = (Instantiate(pref_Bloc) as GameObject).GetComponent<Bloc>();
            instantiateBloc.Set(bloc, colorMap, limits);
            instantiateBloc.transform.SetParent(rect);
            blocs.Add(instantiateBloc);
        }

        void AddEmpty()
        {
            GameObject l_empty = new GameObject();
            RectTransform l_rect = l_empty.AddComponent<RectTransform>();
            Image l_image = l_empty.AddComponent<Image>();
            l_image.color = Color.black;
            l_empty.name = "Empty bloc";
            l_empty.transform.SetParent(rect);
        }
        #endregion
    }
}
