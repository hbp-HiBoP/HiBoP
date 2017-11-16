using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Experience.Protocol;
using UnityEngine.EventSystems;
using Tools.Unity;

namespace HBP.UI.Experience.Protocol
{
	public class BlocItem : Tools.Unity.Lists.ActionnableItem<Bloc> , IBeginDragHandler , IDragHandler, IEndDragHandler
	{
        #region Properties
        [SerializeField]
        Image m_Image;
        [SerializeField]
        Image m_Illustration;
        [SerializeField]
        Text m_Label;

        public override Bloc Object
        {
            get
            {
                return base.Object;
            }

            set
            {
                base.Object = value;
                gameObject.name = value.Name;
                m_Label.text = value.Name;
                Theme.ThemeElement themeElement = GetComponent<Theme.ThemeElement>();
                if (value.Position.Column == 1)
                {
                    themeElement.Item = Theme.ThemeElement.ItemEnum.MainBloc;
                    Texture2D l_texture = new Texture2D(0, 0);
                    if (l_texture.LoadPNG(value.IllustrationPath))
                    {
                        m_Illustration.sprite = Sprite.Create(l_texture, new Rect(0, 0, l_texture.width, l_texture.height), new Vector2(0.5f, 0.5f));
                        m_Illustration.enabled = true;
                        m_Label.enabled = false;
                    }
                    else
                    {
                        m_Illustration.enabled = false;
                        m_Label.enabled = true;
                    }
                }
                else
                {
                    themeElement.Item = Theme.ThemeElement.ItemEnum.SecondaryBloc;
                    m_Illustration.enabled = false;
                    m_Label.enabled = true;
                }
                themeElement.Set(ApplicationState.GeneralSettings.Theme);
            }
        }
        #endregion

        #region Public Methods
        public void OpenBlocModifier()
		{
            OnAction.Invoke(Object, 0);
        }
        public void RemoveBloc()
		{
            OnAction.Invoke(Object, 1);
        }
        public void OnBeginDrag(PointerEventData eventData)
		{
            GetComponent<LayoutElement>().ignoreLayout = true;
            RectTransform l_rect = GetComponent<RectTransform>();
            Rect l_lastRect = l_rect.rect;
            l_rect.anchorMin = new Vector2(0.5f, 0.5f);
            l_rect.anchorMax = new Vector2(0.5f, 0.5f);
            l_rect.sizeDelta = l_lastRect.size;
            l_rect.SetParent(transform.parent.parent);
            l_rect.SetAsLastSibling();
        }
		public void OnDrag(PointerEventData eventData)
		{
            RectTransform l_rect = GetComponent<RectTransform>();
            l_rect.position = Input.mousePosition;
        }
        public void OnEndDrag(PointerEventData eventData)
		{
            OnAction.Invoke(Object, 2);
            GetComponent<LayoutElement>().ignoreLayout = false;
        }
        #endregion
    }
}

