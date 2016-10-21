using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Experience.Protocol;
using UnityEngine.EventSystems;
using Tools.Unity;

namespace HBP.UI.Experience.Protocol
{
	public class BlocItem : Tools.Unity.Lists.ListItemWithActions<Bloc> , IBeginDragHandler , IDragHandler, IEndDragHandler
	{
		#region Attributs

		#region UI Elements
		[SerializeField]
		Image m_image;
		[SerializeField]
		Image m_illustration;
		[SerializeField]
		Text m_label;
		[SerializeField]
		Color m_headerColor;
		[SerializeField]
		Color m_bodyColor;
		#endregion

		#endregion

		#region Public Methods

		public void OpenBlocModifier()
		{
            ActionEvent.Invoke(Object, 0);
        }

        public void RemoveBloc()
		{
            ActionEvent.Invoke(Object, 1);
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
            ActionEvent.Invoke(Object, 2);
            GetComponent<LayoutElement>().ignoreLayout = false;
        }

        #endregion

        #region Private Methods
        protected override void SetObject(Bloc bloc)
        {
            gameObject.name = bloc.DisplayInformations.Name;
            m_label.text = bloc.DisplayInformations.Name;
            if (bloc.DisplayInformations.Column == 1)
            {
                m_image.color = m_headerColor;
                Texture2D l_texture = new Texture2D(0, 0);
                if (l_texture.LoadPNG(bloc.DisplayInformations.Image))
                {
                    m_illustration.sprite = Sprite.Create(l_texture, new Rect(0, 0, l_texture.width, l_texture.height), new Vector2(0.5f, 0.5f));
                    m_illustration.enabled = true;
                    m_label.enabled = false;
                }
                else
                {
                    m_illustration.enabled = false;
                    m_label.enabled = true;
                }
            }
            else
            {
                m_image.color = m_bodyColor;
                m_illustration.enabled = false;
            }
        }
        #endregion
    }
}

