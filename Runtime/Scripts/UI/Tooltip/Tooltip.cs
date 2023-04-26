using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace StudioManette.Edna
{
    public abstract class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        protected abstract string GetText();

        public void OnPointerEnter(PointerEventData eventData)
        {
            TooltipManager.Activate(GetText());
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            TooltipManager.CancelToolTip();
        }
    }
}
