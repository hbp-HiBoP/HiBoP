using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;


namespace Tools.Unity
{
	public class DoubleClickEventHandler : MonoBehaviour , IPointerClickHandler
	{
		public float delayBetweenClick = 0.1f;
		private bool isSecondClick = false;
		private bool WaitSecondClick = false;
		
		public FunctionToCall functionsToCallOnSimpleClick;
		public FunctionToCall functionsToCallOnDoubleClick;

		public void OnPointerClick(PointerEventData eventData)
		{
			OnClick();
		}

		private void OnClick()
		{
			if(!WaitSecondClick)
			{
				WaitSecondClick= true;
				StartCoroutine(WaitForClick(delayBetweenClick));
			}
			else
			{
				isSecondClick=true;
			}
		}
		
		private void simpleClick()
		{
			if(functionsToCallOnSimpleClick.Component != null)
			{
                functionsToCallOnSimpleClick.Send();
            }
		}
		
		private void doubleClick()
		{
			if(functionsToCallOnDoubleClick.Component != null)
			{
                functionsToCallOnDoubleClick.Send();
            }
		}
		
		IEnumerator WaitForClick(float waitTime) 
		{
			yield return new WaitForSeconds(waitTime);
			if(isSecondClick==true && WaitSecondClick==true)
			{
				doubleClick();
			}
			else
			{
				simpleClick();
			}
			WaitSecondClick=false;
			isSecondClick=false;
		}
	}
}


