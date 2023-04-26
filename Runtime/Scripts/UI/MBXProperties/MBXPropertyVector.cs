using UnityEngine;

namespace StudioManette.Edna
{
    public class MBXPropertyVector : MBXProperty
    {
        public TMPro.TMP_InputField InputX;
        public TMPro.TMP_InputField InputY;
        public Min_Max_Slider.MinMaxSlider minMaxSlider;

        public void Setup(Vector2 range)
        {
            Vector4 initValue = propManager.GetValueVectorFromMBX(materialID, propID);

            minMaxSlider.SetLimits(range.x, range.y);
            minMaxSlider.SetValues(initValue.x, initValue.y, false);

            if (InputX) InputX.text = initValue.x.ToString();
            if (InputY) InputY.text = initValue.y.ToString();
        }

        public void SetStringX(string strValue)
        {
            float val = CommonUtils.ParseStringToFloat(strValue);
            minMaxSlider.SetValues(val, minMaxSlider.Values.maxValue);
            SetX(val, true);
        }

        public void SetStringY(string strValue)
        {
            float val = CommonUtils.ParseStringToFloat(strValue);
            minMaxSlider.SetValues(minMaxSlider.Values.minValue, val);
            SetY(val, true);
        }

        private void SetX(float floatValue, bool writeFile)
        {
            Vector4 tmpValue = propManager.GetValueVectorFromMBX(materialID, propID);
            tmpValue.x = floatValue;
            propManager.SetValueVectorToMBX(materialID, propID, tmpValue, writeFile);
        }

        private void SetY(float floatValue, bool writeFile)
        {
            Vector4 tmpValue = propManager.GetValueVectorFromMBX(materialID, propID);
            tmpValue.y = floatValue;
            propManager.SetValueVectorToMBX(materialID, propID, tmpValue, writeFile);
        }

        public void SetXYFromMinMaxSlider()
        {
            SetX(minMaxSlider.Values.minValue, false);
            SetY(minMaxSlider.Values.maxValue, false);

            if (InputX) InputX.text = minMaxSlider.Values.minValue.ToString();
            if (InputY) InputY.text = minMaxSlider.Values.maxValue.ToString();
        }

        public override void Activate(bool _isInteractable)
        {
            InputX.interactable = _isInteractable;
            InputY.interactable = _isInteractable;
            minMaxSlider.interactable = _isInteractable;
        }
    }
}
