using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StudioManette.Edna
{
    public class ValueDisplayer : MonoBehaviour
    {
        TMPro.TextMeshProUGUI txtCpnt;
        [Range(0, 9)]
        public int decimals = 3;

        public void OnEnable()
        {
            txtCpnt = GetComponent<TMPro.TextMeshProUGUI>();
        }

        public void DisplayValueFloat(float value)
        {
            string precision = "F" + decimals;
            txtCpnt.text = value.ToString(precision);
        }
    }
}
