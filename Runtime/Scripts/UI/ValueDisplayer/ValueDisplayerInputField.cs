using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StudioManette.Edna
{
    public class ValueDisplayerInputField : MonoBehaviour
    {
        TMPro.TMP_InputField inputCpnt;
        [Range(0, 9)]
        public int decimals = 3;

        public void OnEnable()
        {
            inputCpnt = GetComponent<TMPro.TMP_InputField>();
        }

        public void DisplayValueFloat(float value)
        {
            string precision = "F" + decimals;
            inputCpnt.text = value.ToString(precision);
        }
    }
}
