namespace StudioManette.Edna
{
    public class MBXPropertyFloat : MBXProperty
    {
        public TMPro.TMP_InputField floatLabel;

        public void Setup()
        {
            float initValue = propManager.GetValueFloatFromMBX(materialID, propID);

            if (floatLabel) floatLabel.SetTextWithoutNotify(initValue.ToString());
        }

        public void SetFloat(float floatValue)
        {
            SetFloat(floatValue, false);
        }

        protected void SetFloat(float floatValue, bool writeFile)
        {
            propManager.SetValueFloatToMBX(materialID, propID, floatValue, writeFile);
        }

        public void SetStringToFloat(string strValue)
        {
            float val = float.Parse(strValue, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture);
            SetFloat(val, true);
        }

        public override void Activate(bool _isInteractable)
        {
            floatLabel.interactable = _isInteractable;
        }
    }
}
