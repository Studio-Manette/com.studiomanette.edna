using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

using DG.Tweening;

using TriLibCore;
using TriLibCore.SFB;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

using StudioManette.Bob.Settings;

using Utils = StudioManette.Edna.RuntimeUtils;

namespace StudioManette.Edna
{
    public class ViewerManager : MonoBehaviour
    {
        private const string ArgModelFbx = "-model";
        private const string ArgMat = "-mat";
        private const string ArgTask = "-task";

        [Header("Assets loader")]
        public AssetLoaderOptions assetLoaderOptions;
        public AssetViewerManager assetViewerManager;

        [Header("Assets infos")]
        public TMPro.TextMeshProUGUI textAssetPath;
        public TMPro.TextMeshProUGUI textMaterialPath;
        public TMPro.TextMeshProUGUI textTask;
        public TMPro.TextMeshProUGUI textVertices;
        public TMPro.TextMeshProUGUI textTexturesInfos;

        [Header("Info Management")]
        public ErrorWindow errorWindow;
        public ConfirmWindow confirmWindow;
        public LoadingWindow loadingWindow;

        [Header("Material Management")]
        public MaterialManager materialManager;

        [Header("Hierarchy Management")]
        public RuntimeInspectorNamespace.RuntimeHierarchy hierarchyWindow;
        public Material materialHighLight;
        public string alphaPropertyName = "_Alpha";
        public Vector2 alphaPropertyMinMaxValue = new Vector2(0.0f, 0.5f);
        public float timeBlink = 0.5f;

        [Header("History Management")]
        public HistoryManager historyManager;

        [Header("BG option")]
        public string prefix_BG = "BG";
        public Button[] objectsToEnableOnBGOption;
        public Button[] objectsToDisableOnBGOption;
        public bool isBGoption = false;

        public GameObject rootGameObject;
        private MaterialManagerUtils.LoadingType currentLoadingType;

        public UnityEvent<GameObject> eventOnLoaded;

        private string initFileNameFBX;
        private string tmpFileNameFBX;

        private string fbxToLoadAtStartup = null;
        private string lastMBXFolder_;

        public void Awake()
        {
            RegisterArguments();

            errorWindow.gameObject.SetActive(false);
            confirmWindow.gameObject.SetActive(false);
            loadingWindow.gameObject.SetActive(false);

            Application.wantsToQuit += WantsToQuit;

            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

            // This might create settings and as such should be called here rather than in the constructor
            lastMBXFolder_ = Path.Combine(BobSettings.GetOrCreateSettings().RootDrive,
                BobSettings.GetOrCreateSettings().RootAssetsFolder,
                BobSettings.GetOrCreateSettings().AssetsFolder[Bob.ProdVariables.AssetType.SetElement],
                BobSettings.GetOrCreateSettings().AssetsExportFolder);
            if (!Directory.Exists(lastMBXFolder_))
            {
                lastMBXFolder_ = null;
            }
        }

        bool WantsToQuit()
        {
            Utils.Confirm("Do you really want to quit the application ?", ReallyQuit);
            return false;
        }

        private void ReallyQuit()
        {
            Application.wantsToQuit -= WantsToQuit;
            Application.Quit();
        }

        private void RegisterArguments()
        {
            string argFbx = CommonUtils.GetArg(ArgModelFbx);
            //toto += "\n ARG FBX : " + argFbx;

            if (!String.IsNullOrEmpty(argFbx)) fbxToLoadAtStartup = argFbx;

            //Utils.Alert("command : " + toto);

            if (assetLoaderOptions == null)
            {
                assetLoaderOptions = AssetLoader.CreateDefaultLoaderOptions();
            }
            assetViewerManager.SetAssetLoaderOptions(assetLoaderOptions);

            string argMbx = CommonUtils.GetArg(ArgMat);
            if (argMbx != null) textMaterialPath.text = CommonUtils.GetNameFromPath(argMbx);

            string argTsk = CommonUtils.GetArg(ArgTask);
            if (argTsk != null) textTask.text = argTsk;
        }

        public void Start()
        {
            if (fbxToLoadAtStartup != null)
            {
                DisplayModelName(fbxToLoadAtStartup);

                try
                {
                    Utils.LoadingInit("Loading Model...");
                    AssetLoader.LoadModelFromFile(fbxToLoadAtStartup, OnLoad, OnMaterialsLoad, OnProgress, OnError, assetViewerManager.gameObject, assetViewerManager.GetAssetLoaderOptions());
                }
                catch (Exception e)
                {
                    Utils.Alert(e.Message);
                }
            }
        }

        public void OnClickReloadFBXandMBX()
        {
            string mbxPath = initFileNameFBX.Replace(".fbx", ".mbx");
            Utils.Confirm("Do you really want to reload the files ?\n fbx file : " + initFileNameFBX + "\n mbx file : " + mbxPath, ReloadFBXandMBX);
        }

        private void ReloadFBXandMBX()
        {
            if (rootGameObject != null) GameObject.DestroyImmediate(rootGameObject);
            materialManager.DestroyWireframe();

            Utils.LoadingInit("Loading Model...");
            AssetLoader.LoadModelFromFile(initFileNameFBX, OnLoad, OnMaterialsLoadOnReloadFBXandMBX, OnProgress, OnError, assetViewerManager.gameObject, assetViewerManager.GetAssetLoaderOptions());
        }

        public void OnClickReloadFBX()
        {
            Utils.Confirm("Do you really want to reload the FBX file ?\n fbx file : " + initFileNameFBX, ReloadFBX);
        }

        private void ReloadFBX()
        {
            if (rootGameObject != null) GameObject.DestroyImmediate(rootGameObject);
            materialManager.DestroyWireframe();

            Utils.LoadingInit("Loading Model...");
            AssetLoader.LoadModelFromFile(initFileNameFBX, OnLoad, OnMaterialsLoadOnReloadFBX, OnProgress, OnError, assetViewerManager.gameObject, assetViewerManager.GetAssetLoaderOptions());
        }

        public void OnClickReloadMBX()
        {
            string mbxPath = materialManager.GetInitMatPath();
            Utils.Confirm("Do you really want to reload the MBX file ?\n mbx file : " + mbxPath, ReloadMBX);
        }

        private void ReloadMBX()
        {
            if (rootGameObject != null)
            {
                GameObject.DestroyImmediate(rootGameObject);
                materialManager.DestroyWireframe();
            }

            Utils.LoadingInit("Loading Model...");
            AssetLoader.LoadModelFromFile(tmpFileNameFBX, OnLoad, OnMaterialsLoadOnReloadMBX, OnProgress, OnError, assetViewerManager.gameObject, assetViewerManager.GetAssetLoaderOptions());
        }

        public void OnClickLoadMBX()
        {
            var title = "Select a mbx file to load ";
            var extensions = new ExtensionFilter[]
            {
                new ExtensionFilter("MBX files", "mbx")
            };

            try
            {
                StandaloneFileBrowser.OpenFilePanelAsync(title, lastMBXFolder_, extensions, false, OnLoadMBXSelected);
                //StandaloneFileBrowser.OpenFilePanelAsync(title, null, "mbx", false, OnLoadMBXSelected);
            }
            catch (Exception e)
            {
                Utils.Alert(e.Message);
            }
        }

        public void OnLoadSnapshot(string mbxFilePath, string loadingText)
        {
            Utils.LoadingInit(loadingText);
            StartCoroutine(nameof(ReloadMBXWithoutTextures), mbxFilePath);
        }

        public void OnLoadPreset(string mbxFilePath, string loadingText)
        {
            Utils.LoadingInit(loadingText);
            StartCoroutine(nameof(ReloadMBXWithoutTextures), mbxFilePath);
        }

        private IEnumerator ReloadMBXWithoutTextures(string mbxFilePath)
        {
            // we are waiting two frames that the loading screen is correctly dispayed
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            materialManager.ReloadMBXWithoutTextures(mbxFilePath);
        }

        private void OnLoadMBXSelected(IList<ItemWithStream> files)
        {
            if (files.Count < 1)
                return;
            ItemWithStream file = files[0];
            if (file != null && !String.IsNullOrEmpty(file.Name))
            {
                LoadNewMBX(file.Name);
            }
        }

        public void LoadNewMBX(string filePath)
        {
            lastMBXFolder_ = Path.GetDirectoryName(filePath);

            string filePathWithExt = filePath.EndsWith(".mbx") ? filePath : (filePath + ".mbx");

            materialManager.newMbxPath = filePathWithExt;
            if (rootGameObject != null) GameObject.DestroyImmediate(rootGameObject);
            materialManager.DestroyWireframe();

            Utils.LoadingInit("Loading Model...");
            AssetLoader.LoadModelFromFile(tmpFileNameFBX, OnLoad, OnMaterialsLoadOnLoadNewMBX, OnProgress, OnError, assetViewerManager.gameObject, assetViewerManager.GetAssetLoaderOptions());
        }

        public void OnObjectLoadFromAssetViewer(GameObject go)
        {
            OnObjectLoad(go, 0);
        }

        private void OnObjectLoad(GameObject go, MaterialManagerUtils.LoadingType loadingType)
        {
            currentLoadingType = loadingType;

            rootGameObject = go;

            //Recalculate Tangeants
            foreach (MeshFilter mf in go.GetComponentsInChildren<MeshFilter>())
            {
                //mf.mesh.RecalculateTangents();
                Bob.Common.MeshOperations.RecalculateTangentsUV1(mf);
            }
            foreach (SkinnedMeshRenderer smr in go.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                Bob.Common.MeshOperations.RecalculateTangentsUV1(smr);
            }
            ;

            eventOnLoaded.Invoke(rootGameObject);
            UpdateInfos();

            AddBlendshapeConstraints(rootGameObject.transform);

            isBGoption = go.name.StartsWith(prefix_BG);
            SetupBGOption();

            //Generate Hierarchy
            hierarchyWindow.DeleteAllPseudoScenes();

            string pseudoSceneName = go.transform.name;
            hierarchyWindow.CreatePseudoScene(pseudoSceneName);

            foreach (Transform trChild in go.transform)
            {
                hierarchyWindow.AddToPseudoScene(pseudoSceneName, trChild);
            }

            hierarchyWindow.OnItemDoubleClicked = OnHierarchyDoubleClick;
            hierarchyWindow.OnSelectionChanged = OnHierarchySelectionChanged;

            hierarchyWindow.gameObject.SetActive(true);

            Animation anim = rootGameObject.GetComponent<Animation>();
            if (anim && anim.clip)
            {
                QualitySettings.skinWeights = SkinWeights.Unlimited;
                anim.Play();
            }

            //Utils.Confirm("FBX loaded, press OK to load Materials", LoadMaterials);
            StartCoroutine(nameof(LoadMaterials));
        }

        private void SetupBGOption()
        {
            foreach (Button but in objectsToEnableOnBGOption)
            {
                but.interactable = isBGoption;
            }
            foreach (Button but in objectsToDisableOnBGOption)
            {
                but.interactable = !isBGoption;
            }
        }

        private IEnumerator LoadMaterials()
        {
            yield return new WaitForEndOfFrame();

            materialManager.SetMaterials(rootGameObject, currentLoadingType, isBGoption);
        }

        public void OnClickLoadModelFromFile()
        {
            Utils.LoadingInit("Load Model...");
            historyManager.Clean();
            assetViewerManager.LoadModelFromFile();
        }

        public void OnHierarchyDoubleClick(RuntimeInspectorNamespace.HierarchyData clickedItem)
        {
            if (clickedItem.BoundTransform == null)
                return;
            assetViewerManager.SetRootGameObject(clickedItem.BoundTransform.gameObject);
            assetViewerManager.ResetView();
        }

        public void OnHierarchySelectionChanged(ReadOnlyCollection<Transform> selection)
        {
            if (selection.Count != 0)
            {
                HashSet<String> listNames = new HashSet<string>();
                Transform lastChild = null;
                foreach (Transform selectedTransform in selection)
                {
                    listNames.Add(selectedTransform.name);
                    listNames.UnionWith(CommonUtils.ListChildrenNames(selectedTransform, true));
                    lastChild = selectedTransform;
                }

                /*
                Debug.Log("=== listNames : ===");
                foreach (string objName in listNames) Debug.Log(objName);
                Debug.Log("=== END listNames ===");
                */

                List<int> listIndexes = new List<int>();
                AssetUnloader au = lastChild.GetComponentInParent<AssetUnloader>();
                for (int i = 0; i < au.Allocations.Count; i++)
                {
                    if (listNames.Contains(au.Allocations[i].name))
                    {
                        listIndexes.Add(i);
                    }
                }

                /*
                Debug.Log("=== listIndexes : ===");
                foreach (int index in listIndexes) Debug.Log(index);
                Debug.Log("=== END listIndexes ===");
                */

                HashSet<string> filteredMatSlotNames = new HashSet<string>();
                Bob.FBXMetadata fbxm = lastChild.GetComponentInParent<Bob.FBXMetadata>();
                foreach (int index in listIndexes)
                {
                    filteredMatSlotNames.UnionWith(fbxm.GetMaterialSlotNameList(index));
                }

                /*
                Debug.Log("=== filteredMatSlotNames : ===");
                foreach (string matname in filteredMatSlotNames) Debug.Log(matname);
                Debug.Log("=== END filteredMatSlotNames ===");
                */

                materialManager.FilterCurrentMBXMaterials(filteredMatSlotNames.ToList());

                HighLightTransform(lastChild);
            }
            else
            {
                materialManager.FilterCurrentMBXMaterials(null);
            }
        }

        private void HighLightTransform(Transform _transform)
        {
            int oldLayer = _transform.gameObject.layer;
            _transform.gameObject.layer = LayerMask.NameToLayer("Highlight");
            //GAB
            materialHighLight.SetFloat(alphaPropertyName, alphaPropertyMinMaxValue.x);
            materialHighLight.DOFloat(alphaPropertyMinMaxValue.y, alphaPropertyName, timeBlink / 2.0f).OnComplete(() => DecreaseHighLight(_transform, oldLayer)).SetEase(Ease.Linear);
        }

        private void DecreaseHighLight(Transform _tr, int layerToRestore)
        {
            materialHighLight.DOFloat(alphaPropertyMinMaxValue.x, alphaPropertyName, timeBlink / 2.0f).OnComplete(() => UnHighLight(_tr, layerToRestore)).SetEase(Ease.Linear);
        }

        private void UnHighLight(Transform _tr, int layerToRestore)
        {
            _tr.gameObject.layer = layerToRestore;
        }

        //only for one purpose : being called by AssetViewer Trilib
        public void OnObjectLoadNameFromAssetViewer(string goName)
        {
            OnObjectLoadName(goName, true);
        }

        public void OnObjectLoadName(string goName, bool isFirstTime = false)
        {
            if (isFirstTime)
            {
                initFileNameFBX = goName;
                tmpFileNameFBX = Application.temporaryCachePath + "/" + CommonUtils.GetNameFromPath(initFileNameFBX, false) + ".fbx";
                //Copy FBX
                if (File.Exists(tmpFileNameFBX)) File.Delete(tmpFileNameFBX);
                File.Copy(initFileNameFBX, tmpFileNameFBX);
            }

            materialManager.SetFilePath(goName);
            DisplayModelName(goName);
        }

        private void UpdateInfos()
        {
            int vertices = 0;
            int triangles = 0;

            foreach (Mesh mesh in CommonUtils.GetMeshes(rootGameObject))
            {
                vertices += mesh.vertexCount;
                triangles += (mesh.triangles.Length / 3);
            }

            textVertices.text = "vertices : " + vertices + "\ntriangles : " + triangles;
        }

        private void AddBlendshapeConstraints(Transform tr)
        {
            foreach (Transform trChild in tr.transform)
            {
                if (trChild.name.IndexOf(BlendshapeConstraint.BLENDSHAPE_PREFIX) == 0)
                {
                    BlendshapeConstraint blsConstraint = trChild.gameObject.AddComponent<BlendshapeConstraint>();
                    blsConstraint.Init();
                }
                AddBlendshapeConstraints(trChild);
            }
        }

        public void DisplayModelName(string name)
        {
            textAssetPath.text = CommonUtils.GetNameFromPath(name);
        }

        private void OnError(IContextualizedError obj)
        {
            Debug.LogError($"An error occurred while loading your Model: {obj.GetInnerException()}");
            Utils.Alert("An error occurred while loading your Model: " + obj.GetInnerException());
        }

        private void OnProgress(AssetLoaderContext assetLoaderContext, float progress)
        {
            Utils.LoadingProgress(progress);
        }

        private void OnLoad(AssetLoaderContext assetLoaderContext)
        {
        }

        private void OnMaterialsLoad(AssetLoaderContext assetLoaderContext)
        {
            //Utils.LoadingFinish();

            OnObjectLoadName(assetLoaderContext.Filename, true);
            OnObjectLoad(assetLoaderContext.RootGameObject, MaterialManagerUtils.LoadingType.NewFiles);
        }

        private void OnMaterialsLoadOnReloadFBX(AssetLoaderContext assetLoaderContext)
        {
            OnObjectLoadName(assetLoaderContext.Filename);
            OnObjectLoad(assetLoaderContext.RootGameObject, MaterialManagerUtils.LoadingType.ReloadFBX);
        }

        private void OnMaterialsLoadOnReloadMBX(AssetLoaderContext assetLoaderContext)
        {
            OnObjectLoadName(assetLoaderContext.Filename);
            OnObjectLoad(assetLoaderContext.RootGameObject, MaterialManagerUtils.LoadingType.ReloadMBX);
        }

        private void OnMaterialsLoadOnReloadFBXandMBX(AssetLoaderContext assetLoaderContext)
        {
            OnObjectLoadName(assetLoaderContext.Filename);
            OnObjectLoad(assetLoaderContext.RootGameObject, MaterialManagerUtils.LoadingType.ReloadFiles);
        }

        private void OnMaterialsLoadOnLoadNewMBX(AssetLoaderContext assetLoaderContext)
        {
            OnObjectLoadName(assetLoaderContext.Filename);
            OnObjectLoad(assetLoaderContext.RootGameObject, MaterialManagerUtils.LoadingType.NewMBX);
        }

        public void OnApplicationQuit()
        {
            if (File.Exists(tmpFileNameFBX))
            {
                File.Delete(tmpFileNameFBX);
            }
        }
    }
}
