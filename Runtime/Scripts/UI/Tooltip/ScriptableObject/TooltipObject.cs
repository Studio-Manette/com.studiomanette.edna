using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StudioManette.Edna
{
    [CreateAssetMenu(fileName = "Tooltip", menuName = "StudioManette/Tooltip Object", order = 1)]
    public class TooltipObject : ScriptableObject
    {
        [TextArea]
        public string messageText;
    }
}
