using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace StudioManette.Edna
{
    public class TooltipWindow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public void OnPointerEnter(PointerEventData eventData)
        {
            TooltipManager.OnPointerIsOnUI(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            TooltipManager.OnPointerIsOnUI(false);
            TooltipManager.CancelToolTip();
        }
    }
}
