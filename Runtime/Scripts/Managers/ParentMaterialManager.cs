using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace StudioManette.Edna
{
    [System.Serializable]
    internal class CaptureParentMaterial
    {
        public string DisplayName;
        public string Name;
        public Material ParentMaterial;
        public Toggle Toggle;
    }

    public class ParentMaterialManager : MonoBehaviour
    {
        [SerializeField]
        private GameObject rootGameObject;

        [SerializeField]
        private List<CaptureParentMaterial> CaptureParentMaterials;

        private Material LoadedMaterial;

        private void Awake()
        {
            foreach (var captureParentMaterial in CaptureParentMaterials)
            {
                captureParentMaterial.Toggle.enabled = false;
            }
        }

        public bool TryGetParentMaterialByName(string name, out Material returnedMaterial)
        {
            foreach (CaptureParentMaterial parentMaterial in CaptureParentMaterials)
            {
                if (parentMaterial.Name == name)
                {
                    returnedMaterial = parentMaterial.ParentMaterial;
                    return true;
                }
            }
            returnedMaterial = null;
            return false;
        }


        public void UpdateParentMaterialByToggle()
        {
            foreach(var captureParentMaterial in CaptureParentMaterials) {
                if (captureParentMaterial.Toggle.isOn)
                {
                    RuntimeUtils.ChangeMaterial(rootGameObject, captureParentMaterial.ParentMaterial);
                    return;
                }
            }
            RuntimeUtils.ChangeMaterial(rootGameObject, LoadedMaterial);
        }

        public void EnableParentMaterialToggles()
        {
            StartCoroutine(UpdateTogglesState());
        }

        IEnumerator UpdateTogglesState()
        {
            yield return new WaitForSeconds(1.0f);

            LoadedMaterial = RuntimeUtils.GetMaterial(rootGameObject);
            foreach (var captureParentMaterial in CaptureParentMaterials)
            {
                captureParentMaterial.Toggle.isOn = false;
                captureParentMaterial.Toggle.enabled = true;
            }
        }
    }
}
