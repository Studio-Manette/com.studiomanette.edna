using UnityEngine;
using UnityEngine.Profiling;

namespace StudioManette.Edna
{
    public class MBXGroupProperty : MBXSubPropertyCategory
    {
        private int childrenGroupId;

        public void Setup(int _childCatIndex, int _subChildCatIndex, int _groupIndex)
        {
            childrenCategoryId = _childCatIndex;
            childrenSubCategoryId = _subChildCatIndex;
            childrenGroupId = _groupIndex;

            if (toggleFoldOut != null)
            {
                toggleFoldOut.gameObject.SetActive(true);
                toggleFoldOut.isOn = false;

                toggleFoldOut.onValueChanged.RemoveListener(OnFold);
                toggleFoldOut.onValueChanged.AddListener(OnFold);
            }
        }
    }
}
