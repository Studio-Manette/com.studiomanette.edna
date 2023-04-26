using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace StudioManette.Edna
{
    public class TooltipMaterialProperty : Tooltip
    {
        string materialToolTip;

        public void Init(string _tooltip)
        {
            materialToolTip = _tooltip;
        }

        protected override string GetText()
        {
            return materialToolTip;
        }
    }
}
