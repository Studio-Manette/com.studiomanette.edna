using System;

using UnityEngine;
using UnityEngine.UI;

using StudioManette.Bob.MBX;

using MachinMachines.Utils;

using Utils = StudioManette.Edna.RuntimeUtils;

namespace StudioManette.Edna
{
    public class MBXPropertyTexture : MBXProperty
    {
        public TMPro.TextMeshProUGUI texturePath;
        public RectTransform infos;
        public Button buttonChangeTexture;

        [HideInInspector]
        public TexturePool.TextureItem textureItem
        {
            get
            {
                string path = propManager.GetValueTextFromMBX(materialID, propID);
                return TexturePool.GetTexture(path);
            }
        }

        private string currentTexturePath = "";
        private string defaultTexturePath = "";

        public const string LINEAR_STR = "Linear";
        public const string SRGB_STR = "sRGB";

        private string colorSpace;
        private bool mustBeLinear;

        private bool isWaitingForImport;

        public void OnEnable()
        {
            buttonChangeTexture.onClick.AddListener(ChangeTexture);
        }

        public void Update()
        {
            if (isWaitingForImport)
            {
                if (!MBXWriter.Instance.HasPendingJobs)
                {
                    CallBackAfterLoadingFinish();
                }
            }
        }

        private void CallBackAfterLoadingFinish()
        {
            isWaitingForImport = false;
            Utils.LoadingFinish();
            Utils.GetManager().historyManager.CheckSanity();
        }

        public void Setup(bool _isLinear, bool _mustBeLinear)
        {
            mustBeLinear = _mustBeLinear;
            Refresh(_isLinear);
        }

        public void Refresh(bool _isLinear)
        {
            colorSpace = _isLinear ? LINEAR_STR : SRGB_STR;
            string path = propManager.GetValueTextFromMBX(materialID, propID);
            texturePath.text = CommonUtils.GetNameFromPath(path);

            if (!string.IsNullOrEmpty(defaultValue))
            {
                defaultTexturePath = defaultValue.Substring(2); // "on retire le T_
            }
        }

        public void UpdateInfosTexture()
        {
            string tooltip = "texture not found.";
            if (textureItem == null)
            {
                Utils.LogWarning("Texture not found : " + textureItem.TextureParms.Filepath);
            }
            else
            {
                int fileSize = (int)(new System.IO.FileInfo(textureItem.TextureParms.Filepath).Length);
                tooltip = "<b>path : </b> " + textureItem.TextureParms.Filepath +
                    "\n <b>colorSpace : </b> " + colorSpace +
                    "\n <b>size ROM : </b> " + MemorySizeUnit.DisplayByteSize(fileSize) +
                    "\n <b>size VRAM: </b> ~" + MemorySizeUnit.DisplayByteSize((long)(TextureUtils.GetTextureSizeInBytes(textureItem) * CommonUtils.RATIO_PIXEL_VRAM)) +
                    "\n <b>dimensions : </b> " + textureItem.TexturePixelSize.x + "x" + textureItem.TexturePixelSize.y;
                infos.GetComponentInChildren<TooltipMaterialProperty>().Init(tooltip);
                //Debug.Log("UpdateInfosTexture : " + tooltip);
            }
            infos.GetComponentInChildren<TooltipMaterialProperty>().Init(tooltip);
        }

        private void ChangeTexture()
        {
            if (propManager != null)
            {
                TextureLoaderWindow.Show(this,
                    materialID,
                    propID,
                    textureItem?.TextureParms.Filepath,
                    propManager.GetValueTextFromMBX(materialID, propID),
                    mustBeLinear,
                    colorSpace,
                    Bob.MBX.TextureFormat.BC7);
            }
        }

        private void AssignTexture(string filepath, Bob.MBX.TextureFormat format = Bob.MBX.TextureFormat.BC7, bool isLinear = true)
        {
            if (!String.IsNullOrEmpty(filepath))
            {
                TextureParms texParams = new TextureParms();
                texParams.ColorSpace = isLinear ? ColorSpace.Linear : ColorSpace.Gamma;
                texParams.TextureFormat = format;
                texParams.Filepath = filepath;

                Utils.GetManager().materialManager.SetValueTextureToMBX(materialID, propID, texParams, true);

                Refresh(isLinear);

                isWaitingForImport = true;
            }
            else
            {
                Utils.Alert("texture file path is null or empty");
                CallBackAfterLoadingFinish();
            }
        }

        public override void Activate(bool _isInteractable)
        {
            buttonChangeTexture.interactable = _isInteractable;

            Utils.LoadingInit("Refresh Textures...");

            //Replace StartCOroutine by an Invoke bc you can't launch coroutine in a disabled gameobject
            Invoke(nameof(CoroutineActivate), 0.05f);
        }

        private void CoroutineActivate()
        {
            //yield return new WaitForEndOfFrame();

            if (buttonChangeTexture.interactable)
            {
                AssignTexture(currentTexturePath);
            }
            else
            {
                currentTexturePath = TexturePool.ResolvePath(propManager.GetValueTextFromMBX(materialID, propID));
                AssignTexture(defaultTexturePath);
            }
        }
    }
}
