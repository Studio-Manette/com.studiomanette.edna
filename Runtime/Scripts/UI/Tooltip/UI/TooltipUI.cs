using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace StudioManette.Edna
{
    public class TooltipUI : Tooltip
    {
        public TooltipObject tooltip;

        protected override string GetText()
        {
            return tooltip.messageText;
        }
    }
}
