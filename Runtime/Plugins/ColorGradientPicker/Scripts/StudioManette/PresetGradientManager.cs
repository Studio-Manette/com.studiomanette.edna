using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace StudioManette.Gradients
{
    public class PresetGradientManager : MonoBehaviour
    {
        public GameObject presetGradientPrefab;
        public Transform transformPresetsParent;
        public Transform ContextMenu;

        // Fired in the case of a preset addition that fails
        // The error message is passed as an argument
        public static UnityEvent<string> OnAddPresetFailure = new UnityEvent<string>();

        private PresetGradient newPresetGradient;
        private PresetGradient lastSelectedPresetGradient;
        private Gradient currentGradient;

        private const string GRADIENTS_FILE_NAME = "gradients.js";

        // Start is called before the first frame update
        void Awake()
        {
            GameObject newPresetGradientGameObject = GameObject.Instantiate(presetGradientPrefab);
            newPresetGradient = newPresetGradientGameObject.GetComponentInChildren<PresetGradient>();
            newPresetGradient.onClick.AddListener(OnClickAddPreset);
            newPresetGradient.position = 999;
            newPresetGradientGameObject.transform.parent = transformPresetsParent;

            ContextMenu.gameObject.SetActive(false);

            LoadGradients();
        }

        public void LateUpdate()
        {
            if (Input.GetMouseButton(0))
            {
                GameObject currentGameObject = EventSystem.current.currentSelectedGameObject;
                if (currentGameObject == null ||
                    currentGameObject.transform.parent == null ||
                    currentGameObject.transform.parent.gameObject != ContextMenu.gameObject)
                {
                    ContextMenu.gameObject.SetActive(false);
                }
            }
        }

        public void UpdateCurrentGradient(Gradient _gradient, Texture2D _texture)
        {
            currentGradient = _gradient;
            newPresetGradient.SetTexture(_texture);
        }

        public void OnClickAddPreset(Gradient grad = null)
        {
            //Debug.Log("on click add preset PresetGradient Manager !");

            if (transformPresetsParent.childCount > 999)
            {
                Debug.LogError("ERROR : OnClickAddPreset : the number of presets you can create can not exceed 999.");
                OnAddPresetFailure.Invoke("ERROR : OnClickAddPreset : the number of presets you can create can not exceed 999.");
            }

            AddPreset(currentGradient, transformPresetsParent.childCount);

            SaveGradients();
        }

        public void AddPreset(Gradient gradient, int position)
        {
            GameObject presetPrefab = GameObject.Instantiate(presetGradientPrefab);
            presetPrefab.transform.SetParent(transformPresetsParent);
            presetPrefab.GetComponentInChildren<TMPro.TextMeshProUGUI>().gameObject.SetActive(false);

            // assigner grad
            presetPrefab.GetComponent<PresetGradient>().Init(this, gradient, position);
            presetPrefab.GetComponent<PresetGradient>().onClick.AddListener(OnClickPreset);

            OrganizePresets();
        }

        public void OnClickPreset(Gradient grad)
        {
            GradientPicker.UpdateGradient(grad);
        }

        public void ShowContextMenu(PresetGradient selectedPG)
        {
            lastSelectedPresetGradient = selectedPG;
            ContextMenu.transform.position = Input.mousePosition;
            ContextMenu.gameObject.SetActive(true);
        }

        public void OnClickReplace()
        {
            Debug.Log("on click replace");
            lastSelectedPresetGradient.Init(this, currentGradient, lastSelectedPresetGradient.position);

            SaveGradients();

            ContextMenu.gameObject.SetActive(false);
        }

        public void OnClickDelete()
        {
            Debug.Log("on click delete");

            DecrementPresetsOverPosition(lastSelectedPresetGradient.position);
            GameObject.DestroyImmediate(lastSelectedPresetGradient.gameObject);

            OrganizePresets();
            SaveGradients();

            ContextMenu.gameObject.SetActive(false);
        }

        public void OnClickMoveAtFirst()
        {
            Debug.Log("on click move at first");

            IncrementPresetsUnderPosition(lastSelectedPresetGradient.position);
            lastSelectedPresetGradient.position = 1;

            OrganizePresets();
            SaveGradients();

            ContextMenu.gameObject.SetActive(false);
        }

        private void LoadGradients()
        {
            string path = Application.persistentDataPath + "/" + GRADIENTS_FILE_NAME;
            string data = null;
            GradientList gradList = null;

            try
            {
                using (StreamReader sr = new StreamReader(path))
                {
                    data = sr.ReadToEnd();
                }
                if (data != null)
                {
                    gradList = JsonUtility.FromJson<GradientList>(data);
                }

            }
            catch (Exception e)
            {
                Debug.LogWarning("Error while reading file : " + path);
                Debug.LogWarning(e.Message);

                //Le fichier n'existe pas encore, c'est pas grave, ne rien faire.
                gradList = null;
            }

            if (gradList != null)
            {
                Init(gradList);
            }

        }

        private void Init(GradientList gradList)
        {
            foreach (JsonGradient gradjs in gradList.gradients)
            {
                Gradient newGrad = gradjs.ToUnityGradient();
                AddPreset(newGrad, gradjs.index);
            }
        }

        private void SaveGradients()
        {
            GradientList gradList = new GradientList();
            foreach (Transform trChild in transformPresetsParent)
            {
                PresetGradient tmpPreset = trChild.gameObject.GetComponent<PresetGradient>();
                if (tmpPreset == newPresetGradient) continue;
                JsonGradient jsg = new JsonGradient(tmpPreset.gradient, tmpPreset.position);
                gradList.gradients.Add(jsg);
            }

            string path = Application.persistentDataPath + "/" + GRADIENTS_FILE_NAME;

            StreamWriter writer = new StreamWriter(path, false);

            string data = gradList.ToJSonString();

            writer.Write(data);
            writer.Close();

            Debug.Log("file saved : " + path);
        }

        private void DecrementPresetsOverPosition(int currentPos)
        {
            foreach (Transform trChild in transformPresetsParent)
            {
                PresetGradient tmpPreset = trChild.gameObject.GetComponent<PresetGradient>();
                if (tmpPreset != null && tmpPreset.position > currentPos && tmpPreset != newPresetGradient)
                {
                    tmpPreset.position--;
                }
            }
        }

        private void IncrementPresetsUnderPosition(int currentPos)
        {
            foreach (Transform trChild in transformPresetsParent)
            {
                PresetGradient tmpPreset = trChild.gameObject.GetComponent<PresetGradient>();
                if (tmpPreset != null && tmpPreset.position < currentPos && tmpPreset != newPresetGradient)
                {
                    tmpPreset.position++;
                }
            }
        }

        private void OrganizePresets()
        {
            //1. créer liste
            List<GameObject> childList = new List<GameObject>();
            foreach (Transform trChild in transformPresetsParent)
            {
                if (trChild.gameObject.GetComponent<PresetGradient>() != null)
                {
                    childList.Add(trChild.gameObject);
                }
            }

            //1.5 retirer tous les objets de la liste de la hiérarchie
            foreach (GameObject go in childList)
            {
                go.transform.SetParent(null);
            }

            //2. trier liste
            childList.Sort(
                            delegate (GameObject obj1, GameObject obj2)
                            {
                                return obj1.GetComponent<PresetGradient>().position.CompareTo(obj2.GetComponent<PresetGradient>().position);
                            }
                        );

            //3. ajouter les objets dans l'ordre
            foreach (GameObject go in childList)
            {
                go.transform.SetParent(transformPresetsParent);
            }
        }
    }
}
