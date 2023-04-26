using System.Collections.Generic;
using System.IO;

using UnityEngine;

using TMPro;
using TriLibCore.SFB;

using MachinMachines.Common.Repository;

using StudioManette.Bob.MBX;
using StudioManette.Bob.Settings;

using Utils = StudioManette.Edna.RuntimeUtils;

namespace StudioManette.Edna
{
    public class TextureLoaderWindow : MonoBehaviour
    {
        public MaterialManager matManager;

        public TMP_InputField textGlobalPath;
        public TextMeshProUGUI textRelativePath;
        private bool mustBeLinear;
        public TextMeshProUGUI currentColorSpace;
        public TMP_Dropdown dropdownCompression;
        public UnityEngine.UI.Button buttonApply;

        public TooltipObject tooltipGoodColorSpace;
        public TooltipObject tooltipBadColorSpace;

        private MBXPropertyTexture propTextureMBXRef;

        private int matID;
        private int propID;

        private bool isWaitingForImport;

        private string lastTextureFolder_;

        public static TextureLoaderWindow _instance;
        public static TextureLoaderWindow Instance
        {
            get
            {
                return _instance;
            }
        }

        public void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else if (_instance != this)
            {
                Debug.LogError("error : you can't have multiple instance of a TextureLoaderWindow ");
            }
            this.gameObject.SetActive(false);

            buttonApply.onClick.AddListener(OnApplyTexture);

            // This might create settings and as such should be called here rather than in the constructor
            lastTextureFolder_ = Path.Combine(BobSettings.GetOrCreateSettings().RootDrive,
                BobSettings.GetOrCreateSettings().RootAssetsFolder,
                BobSettings.GetOrCreateSettings().AssetsFolder[Bob.ProdVariables.AssetType.SetElement],
                BobSettings.GetOrCreateSettings().AssetsExportFolder);
            if (!Directory.Exists(lastTextureFolder_))
            {
                lastTextureFolder_ = null;
            }
        }

        public void Update()
        {
            if (isWaitingForImport)
            {
                if (!MBXWriter.Instance.HasPendingJobs)
                {
                    isWaitingForImport = false;
                    Hide();
                    Utils.GetManager().historyManager.CheckSanity();
                }
            }
        }

        public static void Show(MBXPropertyTexture _propTextureRef, int matIndex, int propIndex, string _globalPath, string _relativePath, bool _mustBeLinear, string _colorSpace, Bob.MBX.TextureFormat _texFormat)
        {
            _instance.gameObject.SetActive(true);

            _instance.propTextureMBXRef = _propTextureRef;

            _instance.matID = matIndex;
            _instance.propID = propIndex;
            _instance.textGlobalPath.text = _globalPath;
            _instance.textRelativePath.text = _relativePath;
            _instance.mustBeLinear = _mustBeLinear;
            bool isGoodColorSpace = (_mustBeLinear && _colorSpace == MBXPropertyTexture.LINEAR_STR) || (!_mustBeLinear && _colorSpace == MBXPropertyTexture.SRGB_STR);

            _instance.currentColorSpace.color = isGoodColorSpace ? Color.white : Color.red;
            _instance.currentColorSpace.GetComponentInChildren<TooltipUI>().tooltip = isGoodColorSpace ? _instance.tooltipGoodColorSpace : _instance.tooltipBadColorSpace;
            _instance.currentColorSpace.text = _colorSpace;
            _instance.dropdownCompression.value = _texFormat == Bob.MBX.TextureFormat.BC7 ? 0 : 1;

            _instance.SetApplyButtonActive(false);
        }

        public void OnApplyTexture()
        {
            //bool isLinear = (_instance.currentColorSpace.text == MBXPropertyTexture.LINEAR_STR);

            TextureParms texParams = new TextureParms();
            texParams.ColorSpace = mustBeLinear ? ColorSpace.Linear : ColorSpace.Gamma;
            texParams.TextureFormat = dropdownCompression.value == 0 ? Bob.MBX.TextureFormat.BC7 : Bob.MBX.TextureFormat.DXT5;
            texParams.Filepath = textRelativePath.text;

            matManager.SetValueTextureToMBX(matID, propID, texParams, true);

            //propTextureMBXRef.Init();
            propTextureMBXRef.Refresh(mustBeLinear);

            isWaitingForImport = true;
        }

        public void Hide()
        {
            _instance.gameObject.SetActive(false);
        }

        public void RefreshPath()
        {
            string trimmedPath = textGlobalPath.text.Trim();
            trimmedPath = trimmedPath.Replace("\\", "/");

            if (!string.IsNullOrEmpty(trimmedPath))
            {
                lastTextureFolder_ = Path.GetDirectoryName(trimmedPath);
            }

            string relativePath = FilesRepositoryManager.Instance.GetRelativePathFrom(RepositoryUsage.Texture, trimmedPath);

            if (string.IsNullOrEmpty(relativePath))
            {
                SetApplyButtonActive(false);
                textRelativePath.text = "unvalid texture";
                string texturePaths = string.Join("\n", FilesRepositoryManager.Instance.GetAllPaths(RepositoryUsage.Texture));
                Utils.Alert("Error : Unvalid Texture : Your texture file must be located in following folders : \n" + texturePaths);
            }
            else
            {
                SetApplyButtonActive(true);
                textGlobalPath.text = trimmedPath;
                textRelativePath.text = relativePath;
            }
        }

        public void OnBrowseTexture()
        {
            var title = "Select a new texture";
            var extensions = new ExtensionFilter[]
            {
                new ExtensionFilter("PNG file", "png")
            };
            StandaloneFileBrowser.OpenFilePanelAsync(title, lastTextureFolder_, extensions, true, OnTextureSelected);
        }

        private void OnTextureSelected(IList<ItemWithStream> files)
        {
            if (files != null && files.Count > 0 && files[0].HasData)
            {
                //System.IO.Stream stream = files[0].OpenStream();
                textGlobalPath.text = files[0].Name;
                RefreshPath();
            }
            else
            {
                //Utils.Dispatcher.InvokeAsync(ClearSkybox);
            }
        }

        public void SetApplyButtonActive(bool isActive)
        {
            buttonApply.interactable = isActive;
            buttonApply.GetComponentInChildren<TextMeshProUGUI>().color = isActive ? Color.white : Color.grey;
        }
    }
}
