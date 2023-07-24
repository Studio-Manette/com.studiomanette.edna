using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;

using TriLibCore.SFB;

using MachinMachines.Common.Repository;
using MachinMachines.Utils;

using StudioManette.Bob;
using StudioManette.Bob.Helpers;
using StudioManette.Bob.MBX;
using StudioManette.ShaderProperties;

using Utils = StudioManette.Edna.RuntimeUtils;

namespace StudioManette.Edna
{
    public class MaterialManager : MonoBehaviour
    {
        public LightManager lightmanager;
        public Material defaultMaterial;

        [Header("Wireframe")]
        public Material wireframeMaterial;
        public string wireframeColorProperty = "_BackColor";
        public Image colorPreview;

        [Header("MBX Management")]
        public MBXPropertyManager mbxManager;
        //public CustomizableShader[] shaderProfiles;
        public ShaderProfilesConfig shaderConfig;
        // Those are here just so the dependencies can be processed for builds
        public FilesRepository TexturesRepository;
        public FilesRepository MaterialsRepository;

        public AccessiblityLevel accessModifyProperties;
        public TMPro.TMP_Dropdown accessibilityLevel;

        [Header("Export")]
        public CaptureManager captureManager;

        System.Diagnostics.Stopwatch _timer = new System.Diagnostics.Stopwatch();
        List<FileSystemWatcher> _fileWatchers = new List<FileSystemWatcher>();

        public float TextureLoadingCompletion
        {
            get
            {
                return 100.0f * (TextureRequestsInitialCount - TextureRequestsCount) / TextureRequestsInitialCount;
            }
        }

        [HideInInspector]
        public string newMbxPath;

        public GameObject WireFrameGo
        {
            get => wireFrameGo;
        }

        private GameObject wireFrameGo;

        private string filePath;
        private string mbxPathOverride;
        private string initMbxPath;
        private string currentMbxPath;

        private bool willCapture;
        private GameObject currentGo;
        private MBXOverrideCpnt mbxCpnt;

        private int TextureRequestsCount;
        private int TextureRequestsInitialCount;

        private string errorMessageLoadMBX;
        private List<string> errorTextures;

        public Action OnWireframeCreatedCallback;

        public void OnEnable()
        {
            accessibilityLevel.value = (accessModifyProperties == AccessiblityLevel.COMMON_USER ? 0 : 1);
            DisposeFileWatchers();

            //check shaderConfig Existence
            if (shaderConfig != null)
            {
                //Checker l'int�grit� des materials
                foreach (CustomizableShader cs in shaderConfig.shaderProfiles)
                {
                    if (cs == null)
                    {
                        string errorText = "ERROR : Material Manager : shaderProfiles is not complete or it contains missing customizableshaders.";
                        Debug.LogError(errorText);
                        Utils.Alert(errorText);
                    }
                }
            }
            else Debug.LogError("MaterialManager Error : Asset shaderConfig not found. Please create it (Create/StudioManette/LightConfigProfile) and assign it on current Material Manager.");



            // 1.0 Mettre en place les callbacks de chargement
            MBXOperations.loadMaterialDelegate = materialProfileName => MaterialManagerUtils.FindMaterialProfileByName(shaderConfig.shaderProfiles, materialProfileName);
            MBXOperations.loadTextureDelegate = texturePath => MBXOperations.PooledTextureLoading(texturePath);

            if (TexturesRepository == null || TexturesRepository.Usage != RepositoryUsage.Texture)
            {
                Debug.LogError("Empty textures repository - or not a textures repository!");
            }
            if (MaterialsRepository == null || MaterialsRepository.Usage != RepositoryUsage.Material)
            {
                Debug.LogError("Empty materials repository - or not a materials repository!");
            }
            // Only useful in editor mode but still
            FilesRepositoryManager.Instance.FindAllRepositories();

            // Overwrite Texture and Material Repository if configuration files exist
            string TexturesRepositoryFilePath = Application.streamingAssetsPath + "/Configurations/TextureFilesRepository.json";
            if (File.Exists(TexturesRepositoryFilePath))
            {
                JsonUtility.FromJsonOverwrite(File.ReadAllText(TexturesRepositoryFilePath), TexturesRepository);
            }

            string MaterialRepositoryFilePath = Application.streamingAssetsPath + "/Configurations/MaterialFilesRepository.json";
            if (File.Exists(TexturesRepositoryFilePath))
            {
                JsonUtility.FromJsonOverwrite(File.ReadAllText(MaterialRepositoryFilePath), MaterialsRepository);
            }
        }

        public void Update()
        {
            // in this case, the mbx has no texture to load
            if (TextureRequestsCount == -1)
            {
                TextureRequestsCount = 0;
                FinishLoadingTextures();
            }

            if ((TextureRequestsInitialCount > 0)
                && (TextureRequestsInitialCount == TextureRequestsCount))
            {
                InitLoadingTextures();
            }

            if (TexturePool.GetLoadingTexturesCount() > 0.0f)
            {
                TexturePool.Update();
                if (CheckError())
                {
                    FinishLoadingTextures();
                }
            }
        }

        private bool CheckError()
        {
            if (mbxCpnt.HasError())
            {
                Utils.Alert($"Error while parsing MBX {mbxCpnt.GetStatus()} - {mbxCpnt.GetStatusDetails()}");
                return true;
            }
            else return false;
        }

        public void SetFilePath(string fp)
        {
            filePath = fp;
        }

        public string GetInitMatPath()
        {
            return initMbxPath;
        }

        public void SetValueTextureToMBX(int matID, int propID, TextureParms texParams, bool writeFile)
        {
            mbxCpnt.SetPropertyTextureValue(matID, propID, texParams);
            if (writeFile)
            {
                mbxCpnt.SerialiseMBXData();
            }

            if (TexturePool.Schedule(texParams, true, OnTextureLoaded, OnTextureLoadingFailed, OnTextureLoadingEnded))
            {
                TextureRequestsCount += 1;
            }
            TextureRequestsInitialCount = TextureRequestsCount;
        }

        public void SetMaterials(GameObject go, MaterialManagerUtils.LoadingType typeLoading, bool isBG = false)
        {
            mbxCpnt = null;

            Utils.LoadingInit("Load Materials...");

            mbxManager.isEnablingPresets = !isBG;

            string mbxPath;

            switch (typeLoading)
            {
                case MaterialManagerUtils.LoadingType.NewMBX: mbxPath = newMbxPath; break;
                case MaterialManagerUtils.LoadingType.ReloadMBX: mbxPath = initMbxPath; break;
                case MaterialManagerUtils.LoadingType.ReloadFBX: mbxPath = mbxPathOverride; break;
                default: mbxPath = filePath.Replace(".fbx", ".mbx"); break;
            }

            if (File.Exists(mbxPath))
            {
                currentGo = go;
                //loadingWindow.SetActive(true);
                SetMBX(mbxPath, typeLoading);
            }
            else
            {
                Utils.Alert("error: no MBX file found : " + mbxPath);
                Utils.LoadingFinish();
                //ApplyMaterialToGameObject(defaultMaterial, go);
            }
        }

        private void OnTextureFileChanged(object sender, FileSystemEventArgs e)
        {
            LaunchLoadingTextures(true);
        }

        public MBXData GetCurrentMBXData()
        {
            if (mbxCpnt == null || String.IsNullOrEmpty(mbxCpnt.OverrideFilePath))
            {
                return null;
            }

            MBXData tmpData = Serialiser.GetDataFromFile<MBXData>(mbxCpnt.OverrideFilePath);
            return tmpData;
        }

        public void ReloadMBXWithoutTextures(string mbxpath)
        {
            mbxManager.Reset();

            mbxCpnt = currentGo.GetComponentInChildren<MBXOverrideCpnt>();

            mbxCpnt.SourceFilePath = mbxpath;
            mbxCpnt.OverrideFilePath = mbxpath;

            //2. TriggerNewRefresh
            mbxCpnt.TriggerNewRefresh(MBXRefreshType.ForceNoTextureRefresh);
            //GB : removed for testing purposes
            //mbxCpnt.SerialiseMBXData();

            //LaunchLoadingTextures(false);


            //4 Refresh l'interface � droite
            mbxManager.Init(mbxCpnt, this);

            Utils.LoadingFinish();
        }

        public void ReloadOnlyTextures()
        {
            Utils.Confirm("Do you want to reload current MBX ? (only textures)", ReloadTextures);
        }

        private void ReloadTextures()
        {
            mbxManager.Reset();

            mbxCpnt = currentGo.GetComponentInChildren<MBXOverrideCpnt>();

            //0. Dupliquer Override File Path
            string tmpOldMbx = mbxCpnt.OverrideFilePath;

            //1. Copier le source file � la place de l'override
            CopyMBX(mbxCpnt.SourceFilePath, tmpOldMbx);

            //2. TriggerNewRefresh
            mbxCpnt.TriggerNewRefresh(MBXRefreshType.ForceAllTextureRefreshOnly | MBXRefreshType.ForceReadFiles);
            mbxCpnt.SerialiseMBXData();

            //3. Recharger toutes les textures
            LaunchLoadingTextures(false);

            //4 Refresh l'interface � droite
            mbxManager.Init(mbxCpnt, this);
        }

        public void SetVisibilty()
        {
            switch (accessibilityLevel.value)
            {
                case 0:
                    accessModifyProperties = AccessiblityLevel.COMMON_USER;
                    break;
                case 1:
                    accessModifyProperties = AccessiblityLevel.SUPER_USER;
                    break;
            }
            mbxManager.FilterByAccessibility(accessModifyProperties);
        }

        private void SetMBX(string mbxPath, MaterialManagerUtils.LoadingType typeLoading)
        {
            if (typeLoading == MaterialManagerUtils.LoadingType.NewFiles)
            {
                initMbxPath = mbxPath;
            }

            //Debug.Log("SET MBX : " + initMbxPath);
            //string mbxPath = filePath.Replace(".fbx", ".mbx");
            string dateString = DateTime.Now.ToString("yyMMddHHmmss");
            string truncatedMbxPath = CommonUtils.GetNameFromPath(mbxPath, false);
            string tmpNewMbxPath = truncatedMbxPath;

            if (typeLoading == MaterialManagerUtils.LoadingType.ReloadFBX)
            {
                //on retire le nombre de caract�res qui correspond au format de date +1 pour le caract�re _
                tmpNewMbxPath = truncatedMbxPath.Substring(0, truncatedMbxPath.Length - (dateString.Length + 1));
            }

            mbxPathOverride = Application.temporaryCachePath + "/" + tmpNewMbxPath + "_" + dateString + ".mbx";

            Utils.LoadingProgress(25f, mbxPath);

            if (!File.Exists(mbxPathOverride))
            {
                FileStream fs = File.Create(mbxPathOverride);
                fs.Close();
            }

            currentMbxPath = mbxPath;
            Utils.LoadingProgress(50f, mbxPath);

            Invoke(nameof(LaunchLoadingMaterials), 0.1f);
        }

        private void LaunchLoadingMaterials()
        {
            //System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            //watch.Start();

            //1.1 instancier le FBXMetadata
            FBXMetadata.InsertMaterialsMetadataInto(currentGo);

            //1.2. instancier l'overrride MBX
            mbxCpnt = currentGo.AddComponent<MBXOverrideCpnt>();
            mbxCpnt.SourceFilePath = currentMbxPath;
            mbxCpnt.OverrideFilePath = mbxPathOverride;

            mbxCpnt.ResetMBXFromSource();
            if (CheckError()) { FinishLoadingTextures(); return; }

            mbxCpnt.TriggerNewRefresh(MBXRefreshType.ForceReadFiles);
            if (CheckError()) { FinishLoadingTextures(); return; }

            errorMessageLoadMBX = mbxManager.Init(mbxCpnt, this);
            if (CheckError()) { FinishLoadingTextures(); return; }

            //bugfix #270 : we disactive ShadowCasting on some meshes
            foreach (MeshRenderer mr in currentGo.GetComponentsInChildren<MeshRenderer>())
            {
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                foreach (Material mat in mr.sharedMaterials)
                {
                    if (mat != null)
                    {
                        CustomizableShader sp = MaterialManagerUtils.FindShaderProfileByMaterialName(shaderConfig.shaderProfiles, mat.name);
                        if (sp && sp.isCastingShadows)
                        {
                            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                            break;
                        }
                    }
                }
            }

            //1.3 Gestion des textures
            //LaunchLoadingTextures();
            LaunchLoadingTextures(false);

            //watch.Stop();
            //Debug.Log($"LaunchLoadingMaterials() in {watch.ElapsedMilliseconds}ms");
        }

        private void LaunchLoadingTextures(bool onlySpecificTextures)
        {
            TextureRequestsCount = 0;
            MBXMaterialProperty.PropertyOption[] propertyOptions = onlySpecificTextures ?
                new MBXMaterialProperty.PropertyOption[] { MBXMaterialProperty.PropertyOption.SM, MBXMaterialProperty.PropertyOption.STICKER}
            : MBXMaterialProperty.kAllPropertyOptions;
            HashSet<TextureParms> texturesParms = mbxCpnt.GetTextureDependenciesParms(propertyOptions);
            foreach (TextureParms textureParms in texturesParms)
            {
                if (TexturePool.Schedule(textureParms, true, OnTextureLoaded, OnTextureLoadingFailed, OnTextureLoadingEnded))
                {
                    TextureRequestsCount += 1;
                }
            }

            if (TextureRequestsCount != 0)
            {
                TextureRequestsInitialCount = TextureRequestsCount;
            }
            else
            {
                //setup this value to -1 so we can handle the exception in the Update function
                TextureRequestsCount = -1;
            }
        }

        void OnTextureLoadingFailed(string textureFullPath, string errorMessage)
        {
            TextureRequestsCount -= 1;
            errorTextures.Add("(" + errorTextures.Count + ") " + errorMessage + "\n");
        }

        void OnTextureLoadingEnded()
        {
            FinishLoadingTextures();
            mbxCpnt.TriggerNewRefresh(MBXRefreshType.ForceReadFiles);

            if (!string.IsNullOrEmpty(errorMessageLoadMBX))
            {
                errorMessageLoadMBX += "\n \n";
            }
            if (errorTextures.Count > 0)
            {
                errorMessageLoadMBX += "Textures loading ended with " + errorTextures.Count + " errors : \n \n";
                for (int i = 0; i < errorTextures.Count; i++)
                {
                    errorMessageLoadMBX += errorTextures[i];
                }
            }
            if (!string.IsNullOrEmpty(errorMessageLoadMBX))
            {
                Utils.Alert(errorMessageLoadMBX);
            }
        }

        void InitLoadingTextures()
        {
            _timer.Start();
            errorTextures = new List<string>();

            Utils.LoadingInit("Loading Textures...");
            Utils.LoadingProgress(TextureLoadingCompletion);
        }

        void DisposeFileWatchers()
        {
            // Clean up existing watchers to prevent zombie handles
            foreach (FileSystemWatcher watcher in _fileWatchers)
            {
                watcher.Dispose();
            }
            _fileWatchers.Clear();
        }

        void FinishLoadingTextures()
        {
            Profiler.BeginSample("Material manager - FinishLoadingTextures");

            _timer.Stop();
            _timer.Reset();

            Utils.LoadingProgress(1.0f, "Textures loaded.");
            // The following magic seems required to prevent bug #206
            currentGo.SetActive(false);
            currentGo.SetActive(true);
            currentGo.GetComponent<MBXOverrideCpnt>().TriggerNewRefresh(MBXRefreshType.ForceReadFiles);

            string texturesInfos = mbxManager.RefreshTextures();
            Utils.GetManager().textTexturesInfos.text = texturesInfos;

            // Setup directory watcher for specific textures
            // First list all specific textures directories
            HashSet<string> textureDirectories = new HashSet<string>();
            MBXMaterialProperty.PropertyOption[] propertyOptions = { MBXMaterialProperty.PropertyOption.SM, MBXMaterialProperty.PropertyOption.STICKER };
            HashSet<TextureParms> texturesParms = mbxCpnt.GetTextureDependenciesParms(propertyOptions);
            foreach (TextureParms textureParms in texturesParms)
            {
                textureDirectories.Add(Path.GetDirectoryName(textureParms.Filepath));
            }

            // Now set up watchers
            DisposeFileWatchers();
            int watcherIndex = 0;
            foreach (string directoryToWatch in textureDirectories)
            {
                if (!string.IsNullOrEmpty(directoryToWatch))
                {
                    string fullPath = TexturePool.ResolvePath(directoryToWatch);
                    // Support local textures as well
                    if (Paths.PathStartsWith(fullPath, "Assets"))
                    {
                        fullPath = Path.Combine(Application.dataPath, "..", fullPath);
                    }
                    if (Directory.Exists(fullPath))
                    {
                        fullPath = Path.GetDirectoryName(fullPath);
                    }
                    else if (fullPath.EndsWith(".png"))
                    {
                        fullPath = fullPath.Remove(fullPath.Length - 4);
                    }
                    if (!string.IsNullOrEmpty(fullPath))
                    {
                        FileSystemWatcher watcher = new FileSystemWatcher(fullPath, "*.png");

                        watcher.NotifyFilter = NotifyFilters.Attributes
                            | NotifyFilters.Size
                            | NotifyFilters.LastWrite;
                        watcher.Created += OnTextureFileChanged;
                        watcher.Changed += OnTextureFileChanged;
                        watcher.Error += (object sender, ErrorEventArgs e) => Utils.LogError(e.GetException().Message);
                        watcher.IncludeSubdirectories = true;
                        watcher.EnableRaisingEvents = true;
                        _fileWatchers.Add(watcher);
                        watcherIndex += 1;
                    }
                }
            }

            Profiler.EndSample();
        }

        void OnTextureLoaded(string textureFullPath)
        {
            TextureRequestsCount -= 1;

            //Debug.Log($"Texture pool - Loaded {textureFullPath} - completion {TextureLoadingCompletion}%");
            //Debug.Log("textures to load : " + TextureRequestsCount + "left" );

            Utils.LoadingProgress(TextureLoadingCompletion, textureFullPath);

            /*
            Slider slider = loadingWindow.GetComponentInChildren<Slider>();
            if (slider != null)
            {
                slider.value = TextureLoadingCompletion;
            }
            Transform subTextTransform = loadingWindow.transform.Find("LoadingFrame/SubText");

            if(subTextTransform != null)
            {
                TMPro.TextMeshProUGUI text = subTextTransform.gameObject.GetComponent<TMPro.TextMeshProUGUI>();
                text.text = textureFullPath;
            }
            */
        }

        public void CreateWireFrame(GameObject go)
        {
            if (wireFrameGo != null) GameObject.DestroyImmediate(wireFrameGo);

            wireFrameGo = GameObject.Instantiate(go);
            wireFrameGo.name += "_WIREFRAME";
            wireFrameGo.transform.SetPositionAndRotation(go.transform.position, go.transform.rotation);

            // FOR SKINMESHES
            foreach (Renderer smr in wireFrameGo.GetComponentsInChildren<Renderer>())
            {
                MaterialManagerUtils.ApplyMaterialToGameObject(wireframeMaterial, smr.gameObject);
            }

            wireFrameGo.SetActive(false);

            OnWireframeCreatedCallback?.Invoke();
        }

        #region Wireframe Management

        public void ToggleWireframe(bool toggle)
        {
            if (wireFrameGo)
            {
                if (!toggle)  mbxManager.ApplyProperties();
                currentGo.SetActive(!toggle);
                if (toggle) mbxCpnt.TriggerNewRefresh(MBXRefreshType.ForceReadFiles);

                wireFrameGo.SetActive(toggle);
            }

            lightmanager.ToggleFog(!toggle);
        }

        public void DestroyWireframe()
        {
            if (!wireFrameGo)
                return;
            DestroyImmediate(wireFrameGo);
            ToggleWireframe(false);
        }

        public void ChangeWireFrameColorClick()
        {
            ColorPicker.Create(GetWFColor(), "Choose Wireframe color", SetWFColor, ColorWFFinished, true);
        }

        private Color GetWFColor()
        {
            return wireframeMaterial.GetColor(wireframeColorProperty);
        }

        private void SetWFColor(Color currentColor)
        {
            colorPreview.color = currentColor;
            wireFrameGo.GetComponentInChildren<Renderer>().sharedMaterial.SetColor(wireframeColorProperty, currentColor);
        }

        private void ColorWFFinished(Color finishedColor)
        {
            Debug.Log("You chose the color " + ColorUtility.ToHtmlStringRGBA(finishedColor));
        }

        #endregion

        public void Export(bool capturePNG = false)
        {
            willCapture = capturePNG;

            if (willCapture)
            {
                captureManager.PrepareCapture();
            }

            Invoke(nameof(LaunchFilePanel), 0.1f);
        }

        public void OnClickSaveMBXAs()
        {
            var title = "Select a file to write";
            var extensions = new ExtensionFilter[]
            {
                new ExtensionFilter("MBX files", "mbx")
            };

            string filename = CommonUtils.GetNameFromPath(filePath);
            string fileNameWithoutExtension = filename.EndsWith(".fbx") ? filename.Substring(0, filename.Length - 4) : filename;

            try
            {
                StandaloneFileBrowser.SaveFilePanelAsync(title, null, fileNameWithoutExtension, extensions, OnSaveOnlyMBX);
            }
            catch (Exception e)
            {
                Utils.Alert(e.Message);
            }
        }

        public void OnApplicationQuit()
        {
            if (mbxPathOverride != null && mbxPathOverride.Contains(Application.temporaryCachePath))
            {
                string tmpPathFbx = mbxPathOverride.Replace(".mbx", ".fbx");
                if (File.Exists(tmpPathFbx)) File.Delete(tmpPathFbx);

                // pour l'instant on garde tous les MBX temporaires
                //if (File.Exists(mbxPathOverride)) File.Delete(mbxPathOverride);
            }
        }

        private void LaunchFilePanel()
        {
            var title = "Select a file to write";
            var extensions = new ExtensionFilter[]
            {
                new ExtensionFilter("FBX and MBX files", "fbx")
            };

            string filename = CommonUtils.GetNameFromPath(filePath);
            string fileNameWithoutExtension = filename.EndsWith(".fbx") ? filename.Substring(0, filename.Length - 4) : filename;

            try
            {
                StandaloneFileBrowser.SaveFilePanelAsync(title, null, fileNameWithoutExtension, extensions, OnSaveFileSelected);
            }
            catch (Exception e)
            {
                Utils.Alert(e.Message);
            }
        }

        private void OnSaveOnlyMBX(ItemWithStream file)
        {
            if (file != null && file.Name != null)
            {
                Debug.Log("file.name : " + file.Name);
                Debug.Log("file.tostring : " + file.ToString());

                string mbxfilepath = file.Name.EndsWith(".mbx") ? file.Name : (file.Name + ".mbx");

                //Copy MBX
                CopyMBX(mbxPathOverride, mbxfilepath);

                Invoke(nameof(AfterSave), 0.1f);
            }
            else AfterSave();
        }

        private void OnSaveFileSelected(ItemWithStream file)
        {
            if (file != null && file.Name != null)
            {
                Debug.Log("file.name : " + file.Name);
                Debug.Log("file.tostring : " + file.ToString());

                string filePathWithExt = file.Name.EndsWith(".fbx") ? file.Name : (file.Name + ".fbx");
                string mbxfilepath = filePathWithExt.Replace(".fbx", ".mbx");
                string capturePath = filePathWithExt.Replace(".fbx", ".png");

                string tmpPathFbx = mbxPathOverride.Replace(".mbx", ".fbx");


                //Copy FBX
                if (File.Exists(tmpPathFbx)) File.Delete(tmpPathFbx);
                File.Copy(filePath, tmpPathFbx);
                if (File.Exists(filePathWithExt)) File.Delete(filePathWithExt);
                File.Copy(tmpPathFbx, filePathWithExt);
                Debug.Log("File Copied :  " + filePathWithExt);

                //Copy MBX
                CopyMBX(mbxPathOverride, mbxfilepath);

                //Capture if neeeded
                if (willCapture)
                {
                    if (File.Exists(capturePath))
                    {
                        File.Delete(capturePath);
                    }
                    captureManager.Capture(capturePath);
                }

                Invoke(nameof(AfterSave), 0.1f);
            }
            else AfterSave();
        }

        public void OnClickSaveDirectMBX()
        {
            Utils.Confirm("Do you really want to save this file ?\n mbx file : " + initMbxPath, SaveDirectMBX);
        }

        public void OnExportMBXForEveryBE()
        {
            string message = "";
            try
            {
                string[] result = MBXOperations.ExportSingleMBXsToBaseAssets("", mbxCpnt.OverrideFilePath);

                if (result != null)
                {
                    message = "Following files are successfully saved : ";
                    foreach (string str in result) message += str + "\n";
                }
                else
                {
                    message = "No file to export";
                }
            }
            catch (Exception e)
            {
                message = e.Message;
            }
            Utils.Alert(message);
        }

        private void SaveDirectMBX()
        {
            string message;
            try
            {
                CopyMBX(mbxPathOverride, initMbxPath);
                message = "File Saved : " + initMbxPath;
            }
            catch (Exception e)
            {
                message = e.Message;
            }
            Utils.Alert(message);
        }

        private void CopyMBX(string pathSrc, string pathDest)
        {
            if (File.Exists(pathDest)) File.Delete(pathDest);
            File.Copy(pathSrc, pathDest);
            Debug.Log("File Copied :  " + pathDest);
        }

        private void AfterSave()
        {
            //if (willCapture) captureManager.RestoreAfterCapture();
        }

        public void FilterCurrentMBXMaterials(List<string> matSlotNames)
        {
            mbxManager.FilterByMatNames(matSlotNames);
        }
    }
}
