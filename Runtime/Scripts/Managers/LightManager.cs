using System.Collections.Generic;
using TriLibCore.Samples;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif //UNITY_EDITOR

using Utils = StudioManette.Edna.RuntimeUtils;

namespace StudioManette.Edna
{
    public class LightManager : AbstractInputSystem
    {
        [System.Serializable]
        public struct LightParameters
        {
            public float Azimuth;
            public float Elevation;
        }

        public LightParameters DefaultLightParameters;

        public LightProfilesConfig lightConfig;
        public TMPro.TMP_Dropdown dropdownUI;

        private GameObject currentPrefab;
        private Slider azimuthUI;
        private Slider elevationUI;
        private LightParameters parameters = new LightParameters();

        private bool isFogEnabled = true;

        public void OnSelect(int index)
        {
            if (currentPrefab != null) Destroy(currentPrefab);

            GameObject go = lightConfig.lightProfiles[index];
            if (go != null)
            {
                currentPrefab = GameObject.Instantiate(go);
                currentPrefab.transform.parent = this.gameObject.transform;
                currentPrefab.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

                RefreshProbe();
                ToggleFog(isFogEnabled);
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Refresh Light Probes")]
#endif //UNITY_EDITOR
        public void RefreshProbe()
        {
            try
            {
                ReflectionProbe[] currentRefProbes = currentPrefab.GetComponentsInChildren<ReflectionProbe>();
                foreach (ReflectionProbe currentRefProbe in currentRefProbes)
                {
                    if (currentRefProbe && currentRefProbe.enabled)
                    {
                        HDAdditionalReflectionDataExtensions.RequestRenderNextUpdate(currentRefProbe);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }

        public void ToggleFog(bool toggle)
        {
            isFogEnabled = toggle;
            Volume currentVolume = currentPrefab.GetComponentInChildren<Volume>();
            Fog fog;
            if (currentVolume.profile.TryGet(out fog))
            {
                fog.enabled.overrideState = true;
                fog.enabled.value = isFogEnabled;
            }
        }

        public void SetAzimuth(float value)
        {
            parameters.Azimuth = value;
        }

        public void SetElevation(float value)
        {
            parameters.Elevation = value;
        }

        public void ResetLightParameters()
        {
            parameters = DefaultLightParameters;
            RefreshUI();
        }

        private void Awake()
        {
            dropdownUI.ClearOptions();

            List<string> options = new List<string>();

            //check LightConfig Existence
            if (lightConfig != null)
            {
                foreach (GameObject go in lightConfig.lightProfiles)
                {
                    options.Add(go.name);
                }
                dropdownUI.AddOptions(options);

                OnSelect(0);
            }
            else Debug.LogError("LightManager Error : Asset LightConfig not found. Please create it (Create/StudioManette/LightConfigProfile) and assign it on Light Manager.");

            }

        private void Start()
        {
            GameObject toolUI = Utils.GetManager().assetViewerManager.ToolParameters;
            if (toolUI != null)
            {
                GameObject azimuthGO = toolUI.transform.Find("Azimuth").gameObject;
                if (azimuthGO != null)
                {
                    azimuthUI = azimuthGO.GetComponentInChildren<Slider>();
                }
                GameObject elevationGO = toolUI.transform.Find("Elevation").gameObject;
                if (elevationGO != null)
                {
                    elevationUI = elevationGO.GetComponentInChildren<Slider>();
                }
            }

            ResetLightParameters();
        }

        private void Update()
        {
            if (currentPrefab != null && !currentPrefab.name.Contains("bg"))
            {
                if (!EventSystem.current.IsPointerOverGameObject())
                {
                    if (GetMouseButton(0))
                    {
                        if (GetKey(KeyCode.LeftAlt) || GetKey(KeyCode.RightAlt))
                        {
                            parameters.Azimuth = Mathf.Clamp(parameters.Azimuth + GetAxis("Mouse X"), -180.0f, 180.0f);
                            parameters.Elevation = Mathf.Clamp(parameters.Elevation + GetAxis("Mouse Y"), 0.0f, 90.0f);
                            RefreshUI();
                        }
                    }
                }
                Transform lightMaster = currentPrefab.transform.Find("Master_Lights");
                if (lightMaster != null)
                {
                    Quaternion rotation = Quaternion.AngleAxis(parameters.Azimuth, Vector3.up) * Quaternion.AngleAxis(parameters.Elevation, Vector3.right);
                    if (rotation != lightMaster.rotation)
                    {
                        lightMaster.SetPositionAndRotation(lightMaster.position, rotation);
                    }
                }
                HDRISky hdriSkyTemp;

                if (currentPrefab.GetComponentInChildren<Volume>().profile.TryGet<HDRISky>(out hdriSkyTemp))
                {
                    float RemapAzimuth = Remap(parameters.Azimuth, -180, 180, 0, 360);
                    if (RemapAzimuth != hdriSkyTemp.rotation.value)
                    {
                        hdriSkyTemp.rotation.value = RemapAzimuth;
                        RefreshProbe();
                    }
                }
            }
        }

        public static float Remap(float val, float in1, float in2, float out1, float out2)
        {
            return out1 + (val - in1) * (out2 - out1) / (in2 - in1);
        }

        private void RefreshUI()
        {
            if (azimuthUI != null)
            {
                azimuthUI.value = parameters.Azimuth;
            }
            if (elevationUI != null)
            {
                elevationUI.value = parameters.Elevation;
            }
        }
    }
}
