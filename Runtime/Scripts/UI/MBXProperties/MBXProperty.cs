using UnityEngine;
using UnityEngine.Profiling;

using TMPro;

using StudioManette.ShaderProperties;

namespace StudioManette.Edna
{
    public abstract class MBXProperty : MonoBehaviour
    {
        public static readonly int OFFSET_GROUP = 60;

        public TextMeshProUGUI TMProName;
        public AccessiblityLevel accessibility;

        public int materialID;
        public int categoryIndex;
        public int subCategoryIndex;
        public int groupIndex;

        public string propertyName = "";

        protected int propID;
        protected MBXPropertyManager propManager;

        protected string defaultValue;

        abstract public void Activate(bool _isInteractable);

        virtual public void Init(MBXPropertyManager _mbxpropManager,
            int _matID,
            int _prID,
            string _prName,
            AccessiblityLevel _accessLevel,
            int _category,
            int _subCategory,
            int _group,
            string _defaultValue = null)
        {
            Profiler.BeginSample("MBXProperty - Init");
            propManager = _mbxpropManager;
            materialID = _matID;
            propID = _prID;
            accessibility = _accessLevel;
            categoryIndex = _category;
            subCategoryIndex = _subCategory;
            groupIndex = _group;
            defaultValue = _defaultValue;
            SetName(_prName);
            Profiler.EndSample();
        }

        public void SetName(string propName)
        {
            propertyName = propName;
            string propertyToDisplay = CommonUtils.RemoveSuffixNumber(propertyName);
            TMProName.text = propertyToDisplay;

            // if property is in a group, offset the name
            if (groupIndex != MBXPropertyManager.DEFAULT_GROUP)
            {
                TMProName.GetComponent<RectTransform>().sizeDelta = new Vector2(-OFFSET_GROUP, TMProName.GetComponent<RectTransform>().sizeDelta.y);
            }
        }

        public void EndEdit()
        {
            propManager.ApplyProperties();
        }
    }
}
