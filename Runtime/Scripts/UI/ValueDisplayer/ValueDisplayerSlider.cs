using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StudioManette.Edna
{
    public class ValueDisplayerSlider : MonoBehaviour
    {
        Slider slider;

        public void OnEnable()
        {
            slider = GetComponent<Slider>();
        }

        public void SetValueInFloat(string strval)
        {
            float val = CommonUtils.ParseStringToFloat(strval);
            slider.value = val;
            slider.onValueChanged.Invoke(val);
            this.GetComponent<EventTrigger>().OnPointerUp(null);
        }
    }
}
