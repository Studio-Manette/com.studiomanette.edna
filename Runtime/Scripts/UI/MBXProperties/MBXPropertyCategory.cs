using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;

using StudioManette.ShaderProperties;

namespace StudioManette.Edna
{
    public class MBXPropertyCategory : MBXProperty
    {
        public Toggle toggleFoldOut;
        public int childrenCategoryId = 0;

        public bool IsEnabled { get { return isEnabled; } }
        public bool IsCategory { get { return this.GetType() == typeof(MBXPropertyCategory); } }

        protected AccessiblityLevel userAccess;
        protected bool isEnabled;

        public void Setup(int _childCatIndex)
        {
            childrenCategoryId = _childCatIndex;

            if (toggleFoldOut != null)
            {
                toggleFoldOut.gameObject.SetActive(true);

                toggleFoldOut.isOn = true;
                toggleFoldOut.onValueChanged.RemoveListener(OnFold);
                toggleFoldOut.onValueChanged.AddListener(OnFold);
            }
        }

        public void OnFold(bool isOn)
        {
            isEnabled = isOn;
            UpdateVisibility();
        }

        public virtual void Show(bool _isVisible)
        {
            isEnabled = _isVisible;
            toggleFoldOut.SetIsOnWithoutNotify(_isVisible);
        }

        public void DisplayChildrenCount()
        {
            int count = 0;
            MBXPropertyItem item = propManager.propertiesHierarchicalTree.FindItem(this.gameObject);
            if (item.IsValid())
            {
                // Removing self
                count = item.GetPropertiesCount() - 1;
            }
            if (count == 0)
            {
                toggleFoldOut.gameObject.SetActive(false);
                TMProName.text = "";
            }
            else
            {
                TMProName.text += " (" + count + ")";
            }
        }

        protected void UpdateVisibility()
        {
            if (propManager != null)
            {
                propManager.UpdateVisibility(this.gameObject, isEnabled, userAccess);
            }
        }

        public override void Activate(bool _isInteractable)
        {
            throw new System.NotImplementedException();
        }
    }
}
