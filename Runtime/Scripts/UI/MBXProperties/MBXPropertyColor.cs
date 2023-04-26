using UnityEngine;
using UnityEngine.UI;
using TMPro;
using StudioManette.ShaderProperties;

namespace StudioManette.Edna
{
    public class MBXPropertyColor : MBXProperty
    {
        public Button colorButton;
        private Color propColor;
        private Image colorPreview;

        public void Awake()
        {
            colorButton.onClick.AddListener(OnClickColor);
        }

        public void Setup()
        {
            // Explicitly call lower level methods so we do not trigger a new MBX refresh
            propColor = propManager.GetValueColorFromMBX(materialID, propID);
            colorPreview.color = propColor;
        }

        void OnEnable()
        {
            colorPreview = colorButton.GetComponent<Image>();
            propColor = colorPreview.color;
            SetColorFinished(propColor);
        }

        public void OnClickColor()
        {
            ColorPicker.Create(propColor, "Choose color for " + propertyName, item => SetColor(item, false), SetColorFinished, true);
        }

        private void SetColor(Color currentColor, bool writeFile)
        {
            propColor = currentColor;
            colorPreview.color = propColor;

            if (propManager) propManager.SetValueColorToMBX(materialID, propID, propColor, writeFile);
        }

        private void SetColorFinished(Color currentColor)
        {
            SetColor(currentColor, true);
        }

        public override void Activate(bool _isInteractable)
        {
            throw new System.NotImplementedException();
        }
    }
}
