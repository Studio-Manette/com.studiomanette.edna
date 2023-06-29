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
                    ChangeMaterial(rootGameObject, captureParentMaterial.ParentMaterial);
                    return;
                }
            }
            ChangeMaterial(rootGameObject, LoadedMaterial);
        }

        public void EnableParentMaterialToggles()
        {
            StartCoroutine(UpdateTogglesState());
        }

        IEnumerator UpdateTogglesState()
        {
            yield return new WaitForSeconds(1.0f);

            LoadedMaterial = GetMaterial(rootGameObject);
            foreach (var captureParentMaterial in CaptureParentMaterials)
            {
                captureParentMaterial.Toggle.isOn = false;
                captureParentMaterial.Toggle.enabled = true;
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Change the parent material of the gameobject and its children
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="newParentMaterial"></param>
        public static void ChangeParentMaterial(GameObject gameObject, Material newParentMaterial)
        {
            Renderer renderer;
            if (gameObject.TryGetComponent<Renderer>(out renderer))
            {
                renderer.material.parent = newParentMaterial;
            }

            if (gameObject.transform.childCount > 0)
            {
                foreach (Transform child in gameObject.transform)
                {
                    ChangeParentMaterial(child.gameObject, newParentMaterial);
                }
            }
        }

        /// <summary>
        /// Retrieve the parent material of the gameobject
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        public static Material GetParentMaterial(GameObject gameObject)
        {
            Renderer renderer;
            if (gameObject.TryGetComponent<Renderer>(out renderer))
            {
                return renderer.material.parent;
            }
            else if (gameObject.transform.childCount > 0)
            {
                foreach (Transform child in gameObject.transform)
                {
                    Material res = GetParentMaterial(child.gameObject);
                    if (res != null)
                    {
                        return res;
                    }
                }
            }
            return null;
        }
#endif // UNITY EDITOR

        /// <summary>
        /// Retrieve the material of the gameobject
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        public static Material GetMaterial(GameObject gameObject)
        {
            Renderer renderer;
            if (gameObject.TryGetComponent<Renderer>(out renderer))
            {
                return renderer.material;
            }
            else if (gameObject.transform.childCount > 0)
            {
                foreach (Transform child in gameObject.transform)
                {
                    Material res = GetMaterial(child.gameObject);
                    if (res != null)
                    {
                        return res;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Change the material of the gameobject and its children
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="newMaterial"></param>
        public static void ChangeMaterial(GameObject gameObject, Material newMaterial)
        {
            Renderer renderer;
            if (gameObject.TryGetComponent<Renderer>(out renderer))
            {
                renderer.material = newMaterial;
            }

            if (gameObject.transform.childCount > 0)
            {
                foreach (Transform child in gameObject.transform)
                {
                    ChangeMaterial(child.gameObject, newMaterial);
                }
            }
        }
    }
}
