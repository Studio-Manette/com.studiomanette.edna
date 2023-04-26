using StudioManette.ShaderProperties;
using UnityEngine.UI;

namespace StudioManette.Edna
{
    public class MBXPropertyBoolean : MBXProperty
    {
        public Toggle _toggle;

        public void Setup()
        {
            float initValue = propManager.GetValueFloatFromMBX(materialID, propID);

            _toggle.SetIsOnWithoutNotify(initValue == 1.0f);
        }

        public void SetBool(bool bValue)
        {
            if (propManager != null)
            {
                propManager.SetValueFloatToMBX(materialID, propID, (bValue ? 1.0f : 0.0f), true);
            }
        }

        public override void Activate(bool _isInteractable)
        {
            _toggle.interactable = _isInteractable;
        }
    }
}
