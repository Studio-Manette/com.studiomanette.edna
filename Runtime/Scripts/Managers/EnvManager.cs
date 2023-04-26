using System.Collections.Generic;

using UnityEngine;

namespace StudioManette.Edna
{
    public class EnvManager : MonoBehaviour
    {
        public GameObject[] presets;
        public TMPro.TMP_Dropdown dropdownUI;

        private GameObject currentPrefab;

        public void OnSelect(int index)
        {
            if (currentPrefab != null) Destroy(currentPrefab);

            GameObject go = presets[index];
            if (go != null)
            {
                currentPrefab = GameObject.Instantiate(go);
                currentPrefab.transform.parent = this.gameObject.transform;
                currentPrefab.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

                ReflectionProbe currentRefProbe = currentPrefab.GetComponentInChildren<ReflectionProbe>();
                if (currentRefProbe)
                {
                    currentRefProbe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.ViaScripting;
                    currentRefProbe.RenderProbe();
                }
            }
        }

        private void Awake()
        {
            dropdownUI.ClearOptions();

            List<string> options = new List<string>();

            foreach (GameObject go in presets)
            {
                if (go != null)
                {
                    options.Add(go.name);
                }
                else
                {
                    options.Add("None");
                }
            }
            dropdownUI.AddOptions(options);

            OnSelect(0);
        }

        private GameObject FindPresetByName(string _name)
        {
            foreach (GameObject go in presets)
            {
                if (go.name == _name) return go;
            }
            return null;
        }
    }
}
