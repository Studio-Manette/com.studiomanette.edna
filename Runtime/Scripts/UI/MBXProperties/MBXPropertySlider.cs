using UnityEngine;
using UnityEngine.UI;

namespace StudioManette.Edna
{
    public class MBXPropertySlider : MBXPropertyFloat
    {
        public Slider floatSlider;

        public void Setup(Vector2 range)
        {
            base.Setup();
            float initValue = propManager.GetValueFloatFromMBX(materialID, propID);
            floatSlider.minValue = range.x;
            floatSlider.maxValue = range.y;
            floatSlider.SetValueWithoutNotify(initValue);
        }

        public override void Activate(bool _isInteractable)
        {
            base.Activate(_isInteractable);
            floatSlider.interactable = _isInteractable;
        }
    }
}
