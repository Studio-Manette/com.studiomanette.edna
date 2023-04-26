using TMPro;
using UnityEngine;

namespace StudioManette.Edna
{
    public class MBXPropertyEnum : MBXPropertyFloat
    {
        public TMP_Dropdown floatDropDown;
        public TextMeshProUGUI textValue;

        public void Setup(string[] attributes)
        {
            base.Setup();
            float initValue = propManager.GetValueFloatFromMBX(materialID, propID);
            floatDropDown.ClearOptions();
            for (int i = 0; i < attributes.Length; i++)
            {
                TMP_Dropdown.OptionData opData = new TMP_Dropdown.OptionData(attributes[i]);
                floatDropDown.options.Add(opData);
            }
            floatDropDown.value = (int)initValue;
            textValue.text = initValue.ToString();
        }

        public void SetValueFromDropDown()
        {
            int val = floatDropDown.value;
            SetFloat(val, true);

            textValue.text = val.ToString();
        }
    }
}
