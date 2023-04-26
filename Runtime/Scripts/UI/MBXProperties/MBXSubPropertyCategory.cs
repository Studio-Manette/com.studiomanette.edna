using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;

namespace StudioManette.Edna
{
    public class MBXSubPropertyCategory : MBXPropertyCategory
    {
        public Toggle toggleActive;
        protected int childrenSubCategoryId;
        protected bool isInteractable;

        public void Setup(int _childCatIndex, int _subChildCatIndex)
        {
            base.Setup(_childCatIndex);
            childrenSubCategoryId = _subChildCatIndex;

            toggleActive.isOn = true;
        }

        public void InitAfterLoading()
        {
            //if there's at least one texture in the subcat, active the toggle
            if (toggleActive != null)
            {
                toggleActive.gameObject.SetActive(HasTextureInSubCategory());
            }
        }

        private bool HasTextureInSubCategory()
        {
            if (propManager)
            {
                MBXPropertyItem thisItem = propManager.propertiesHierarchicalTree.FindItem(this.gameObject);
                return thisItem.IsValid() && thisItem.HasItemsOfType<MBXPropertyTexture>(false);
            }
            return false;
        }

        public override void Activate(bool _isInteractable)
        {
            isInteractable = _isInteractable;
        }

        public void OnClickActivate(bool isOn)
        {
            Activate(isOn);
            UpdateActivation();
        }

        protected virtual void UpdateActivation()
        {
            MBXPropertyItem item = propManager.propertiesHierarchicalTree.FindItem(this.gameObject);
            if (item.IsValid())
            {
                foreach (MBXPropertyItem child in item.BrowseChildren(false))
                {
                    MBXProperty mbxProp = child.gameObject.GetComponent<MBXProperty>();
                    mbxProp.Activate(isInteractable);
                }
            }
        }
    }
}
