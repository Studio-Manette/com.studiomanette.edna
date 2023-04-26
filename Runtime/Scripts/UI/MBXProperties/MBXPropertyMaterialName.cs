using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEngine.UI;

using TriLibCore.SFB;

using StudioManette.ShaderProperties;
using StudioManette.Bob.Settings;

using Utils = StudioManette.Edna.RuntimeUtils;

namespace StudioManette.Edna
{
    public class MBXPropertyMaterialName : MBXProperty
    {
        public Toggle toggleFoldOut;

        public Button buttonSavePreset;
        public Button buttonLoadPreset;

        private AccessiblityLevel userAccess;
        private bool isActive;

        private string fileNamePresetToLoad;
        private string lastMbxpFolder_;

        public void Setup(bool _enablePresets = true)
        {
            if (toggleFoldOut != null)
            {
                toggleFoldOut.gameObject.SetActive(true);

                toggleFoldOut.onValueChanged.RemoveListener(OnFold); // remove it if it's already here
                toggleFoldOut.onValueChanged.AddListener(OnFold);
            }
            if (_enablePresets)
            {
                buttonSavePreset.onClick.AddListener(OnClickSavePreset);
                buttonLoadPreset.onClick.AddListener(OnClickLoadPreset);
            }
            else
            {
                buttonSavePreset.gameObject.SetActive(false);
                buttonLoadPreset.gameObject.SetActive(false);
            }

            // This might create settings instance
            lastMbxpFolder_ = Path.Combine(
                BobSettings.GetOrCreateSettings().RootDrive,
                BobSettings.GetOrCreateSettings().RootAssetsFolder,
                BobSettings.GetOrCreateSettings().MbxpFolder
            );
        }

        public void UpdateVisibility(AccessiblityLevel accessLevel)
        {
            userAccess = accessLevel;
            UpdateAccess();
        }

        public void ForceFold(bool isOn)
        {
            if (toggleFoldOut != null)
            {
                toggleFoldOut.isOn = isOn;
            }
        }

        public void OnFold(bool isOn)
        {
            isActive = isOn;
            UpdateAccess();
        }

        private void UpdateAccess()
        {
            if (propManager != null)
            {
                propManager.UpdateVisibility(this.gameObject, isActive, userAccess);
            }
        }

        public override void Activate(bool _isInteractable)
        {
            throw new System.NotImplementedException();
        }

        private void OnClickSavePreset()
        {
            var title = "Select a file to write";
            var extensions = new ExtensionFilter[]
            {
                new ExtensionFilter("MBX presets", "mbxp")
            };

            try
            {
                //GAB
                StandaloneFileBrowser.SaveFilePanelAsync(title, lastMbxpFolder_, propertyName + "_newPreset", extensions, OnSavePreset);
            }
            catch (Exception e)
            {
                Utils.Alert(e.Message);
            }
        }

        private void OnSavePreset(ItemWithStream file)
        {
            if (file != null && !String.IsNullOrEmpty(file.Name))
            {
                lastMbxpFolder_ = Path.GetDirectoryName(file.Name);

                //R�cup�rer le bloc correspond au propertyName
                string mbxPreset = propManager.GetMBXPresetBlock(propertyName);

                //Enregistrer au format .mbxp
                string mbxpFilepath = file.Name.EndsWith(".mbxp") ? file.Name : (file.Name + ".mbxp");
                File.WriteAllText(mbxpFilepath, mbxPreset);
            }
        }

        private void OnClickLoadPreset()
        {
            var title = "Select a mbxp file to load ";
            var extensions = new ExtensionFilter[]
            {
                new ExtensionFilter("MBX Preset", "mbxp")
            };

            try
            {
                StandaloneFileBrowser.OpenFilePanelAsync(title, lastMbxpFolder_, extensions, false, OnConfirmLoadPreset);
            }
            catch (Exception e)
            {
                Utils.Alert(e.Message);
            }
        }

        private void OnConfirmLoadPreset(IList<ItemWithStream> files)
        {
            if (files != null && files.Count > 0 && !String.IsNullOrEmpty(files[0].Name))
            {
                fileNamePresetToLoad = files[0].Name;

                lastMbxpFolder_ = Path.GetDirectoryName(fileNamePresetToLoad);


                string confirmQuestion = "Do you want to Apply Preset on all properties ?";

                confirmQuestion += "\n\nIf you keep persistent properties, it will keep specific properties (SplatMap, Stickers...)";

                Utils.Confirm(confirmQuestion, OnApplyAllDatas, OnApplyAllButPersistentDatas, "Apply All Properties", "Keep Persistent Properties");
            }
            else
            {
                fileNamePresetToLoad = null;
            }
        }

        private void OnApplyAllDatas()
        {
            if (!String.IsNullOrEmpty(fileNamePresetToLoad))
            {
                StreamReader reader = new StreamReader(fileNamePresetToLoad);
                string fileData = reader.ReadToEnd();
                reader.Close();

                propManager.ApplyPreset(propertyName, fileData, false);
            }
        }

        private void OnApplyAllButPersistentDatas()
        {
            if (!String.IsNullOrEmpty(fileNamePresetToLoad))
            {
                StreamReader reader = new StreamReader(fileNamePresetToLoad);
                string fileData = reader.ReadToEnd();
                reader.Close();

                try
                {
                    propManager.ApplyPreset(propertyName, fileData, true);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Exception! Level MBXPRopMatName");
                    Utils.Confirm("Error while checking for persistent properties.  " + e.Message, OnApplyAllDatas, null,
                        "Apply Preset anyway", "Cancel");
                }
            }
        }
    }
}
