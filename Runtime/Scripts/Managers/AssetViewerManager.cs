using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

using DG.Tweening;

using TriLibCore;
using TriLibCore.Extensions;
using TriLibCore.Samples;
using TriLibCore.Utils;

using StudioManette.Bob.Settings;

using Utils = StudioManette.Edna.RuntimeUtils;

namespace StudioManette.Edna
{
    /// <summary>Represents a TriLib sample which allows the user to load models and HDR skyboxes from the local file-system.</summary>
    public class AssetViewerManager : TriLibCore.Samples.AbstractInputSystem
    {
        /***  ASSETS FROM BASE */
        public float sensitivityX = 1;
        public float sensitivityY = 1;

        /// <summary>
        /// Options used in this sample.
        /// </summary>
        protected AssetLoaderOptions AssetLoaderOptions;

        /// <summary>
        /// Current camera pitch and yaw angles.
        /// </summary>
        protected Vector2 cameraAngle;

        /// <summary>
        /// Current camera pitch and yaw angles property.
        /// </summary>
        public Vector2 CameraAngle
        {
            get => cameraAngle;
        }

        /// <summary>
        /// Anchor game object for all view related parameters
        /// </summary>
        [SerializeField]
        private GameObject ViewParameters;

        /// <summary>
        /// Anchor game object for current tool UI
        /// </summary>
        [SerializeField]
        public GameObject ToolParameters;

        /// <summary>
        /// Loaded game object.
        /// </summary>
        public GameObject RootGameObject { get; protected set; }

        /// <summary>
        /// Mouse input multiplier.
        /// Higher values will make the mouse movement more sensible.
        /// </summary>
        private const float InputMultiplierRatio = 0.1f;

        /// <summary>
        /// Maximum camera pitch and light pitch (rotation around local X-axis).
        /// </summary>
        private const float MaxPitch = 80f;

        /*** ASSETS FROM BASE */

        /// <summary>
        /// Maximum camera distance ratio based on model bounds.
        /// </summary>
        private const float MaxCameraDistanceRatio = 3f;

        /// <summary>
        /// Camera distance ratio based on model bounds.
        /// </summary>
        protected const float CameraDistanceRatio = 2f;

        /// <summary>
        /// minimum camera distance.
        /// </summary>
        protected const float MinCameraDistance = 0.01f;

        /// <summary>
        /// Main scene Camera.
        /// </summary>
        [SerializeField]
        private Camera _mainCamera;

        /// <summary>
        /// Current camera distance.
        /// </summary>
        protected float cameraDistance = 1f;

        /// <summary>
        /// Current camera distance property.
        /// </summary>
        public float CameraDistance
        {
            get => cameraDistance;
        }

        /// <summary>
        /// Current camera pivot position.
        /// </summary>
        protected Vector3 cameraPivot;

        /// <summary>
        /// Current camera pivot position property.
        /// </summary>
        public Vector3 CameraPivot
        {
            get => cameraPivot;
        }

        /// <summary>
        /// Input multiplier based on loaded model bounds.
        /// </summary>
        protected float InputMultiplier = 1f;

        /// <summary>
        /// Skybox instantiated material.
        /// </summary>
        private Material _skyboxMaterial;

        /// <summary>
        /// Texture loaded for skybox.
        /// </summary>
        private Texture2D _skyboxTexture;

        /// <summary>
        /// Loaded model cameras.
        /// </summary>
        private IList<Camera> _cameras;

        /// <summary>
        /// Stop Watch used to track the model loading time.
        /// </summary>
        private Stopwatch _stopwatch;

        /*
        public float defaultFov = 60;
        public float minFov = 45;
        public float maxFov = 75;
        */
        private float currentFov = 60;

        // Not used as is, see ActualCameraZoom()
        private float cameraZoomMultiplier = 20.0f / 3.0f;

        private bool isChangingFov = false;
        public bool isFocusOnViewer = false;

        private bool isMaterialsLoaded = false;

        [SerializeField]
        public UnityEvent<GameObject> eventOnLoaded;
        [SerializeField]
        public UnityEvent<string> eventOnLoadedName;

        public bool IsCameraAnimated;

        private string lastFBXFolder_;

        [SerializeField]
        private MaterialManager materialManager;

        private void OnProgress(AssetLoaderContext assetLoaderContext, float value)
        {
            if (!isMaterialsLoaded)
            {
                Utils.LoadingProgress(value * 100.0f, assetLoaderContext.Filename);
            }
            else
            {
                //Utils.LoadingProgress(50.0f, "load materials... :");
            }
        }

        public void SetFov(float value)
        {
            currentFov = value;
            isChangingFov = true;
        }

        public void SetCameraZoom(float value)
        {
            cameraZoomMultiplier = value;
            Transform transform = ViewParameters.transform.Find("Camera_Zoom");
            if (transform != null)
            {
                Slider slider = transform.gameObject.GetComponentInChildren<Slider>();
                if (slider != null)
                {
                    slider.value = cameraZoomMultiplier;
                }
            }
        }

        public void TweenAngle(Vector2 newAngle, float time)
        {
            IsCameraAnimated = true;
            DOTween.To(() => cameraAngle, x => cameraAngle = x, newAngle, time).OnComplete(() => IsCameraAnimated = false);;
        }

        private float ActualCameraZoom()
        {
            // As we have no log slider we map the zoom from its regular linear 0 to 10 scale
            // to a log-ish thing from 10e-2 to 10
            float outMax = Mathf.Log(10f);
            float outMin = Mathf.Log(1e-2f);

            float result = Mathf.Exp(outMin + (outMax - outMin) * (cameraZoomMultiplier / 10.0f));
            return result;
        }

        /// <summary>
        /// Enables/disables the loading flag.
        /// </summary>
        /// <param name="value">The new loading flag.</param>
        protected void SetLoading(bool value)
        {
            var selectables = FindObjectsOfType<Selectable>();
            for (var i = 0; i < selectables.Length; i++)
            {
                var button = selectables[i];
                button.interactable = !value;
            }
        }

        /// <summary>
        /// Shows the file picker for loading a model from the local file-system.
        /// </summary>
        public void LoadModelFromFile(GameObject wrapperGameObject = null, Action<AssetLoaderContext> onMaterialsLoad = null)
        {
            isMaterialsLoaded = false;
            var filePickerAssetLoader = AssetLoaderFilePicker.Create();
            filePickerAssetLoader.LoadModelFromFilePickerAsync("Select a File", OnLoad, onMaterialsLoad ?? OnMaterialsLoad, OnProgress, OnBeginLoadModel, OnError, wrapperGameObject ?? gameObject, AssetLoaderOptions, lastFBXFolder_);
        }

        public void SetRootGameObject(GameObject go)
        {
            RootGameObject = go;
        }

        public void ResetModelScale()
        {
            if (RootGameObject != null)
            {
                RootGameObject.transform.localScale = Vector3.one;
            }
        }

        /// <summary>Switches to the camera selected on the Dropdown.</summary>
        /// <param name="index">The selected Camera index.</param>
        public void CameraChanged(int index)
        {
            for (var i = 0; i < _cameras.Count; i++)
            {
                var camera = _cameras[i];
                camera.enabled = false;
            }
            if (index == 0)
            {
                _mainCamera.enabled = true;
            }
            else
            {
                _cameras[index - 1].enabled = true;
            }
        }

        /// <summary>Loads the skybox from the given Stream.</summary>
        /// <param name="stream">The Stream containing the HDR Image data.</param>
        /// <returns>Coroutine IEnumerator.</returns>
        private IEnumerator DoLoadSkybox(Stream stream)
        {
            //Double frame waiting hack
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            if (_skyboxTexture != null)
            {
                Destroy(_skyboxTexture);
            }
            _skyboxTexture = HDRLoader.HDRLoader.Load(stream, out var gamma, out var exposure);
            _skyboxMaterial.mainTexture = _skyboxTexture;
            stream.Close();
            SetLoading(false);
        }

        /// <summary>Starts the Coroutine to load the skybox from the given Sstream.</summary>
        /// <param name="stream">The Stream containing the HDR Image data.</param>
        private void LoadSkybox(Stream stream)
        {
            SetLoading(true);
            StartCoroutine(DoLoadSkybox(stream));
        }

        /// <summary>Initializes the base-class and clears the skybox Texture.</summary>
        protected void Start()
        {
            Dispatcher.CheckInstance();
            PasteManager.CheckInstance();

            if (AssetLoaderOptions == null)
            {
                AssetLoaderOptions = AssetLoader.CreateDefaultLoaderOptions();
            }
            AssetLoaderOptions.ShowLoadingWarnings = true;

            // This might create settings and as such should be called here rather than in the constructor
            lastFBXFolder_ = Path.Combine(BobSettings.GetOrCreateSettings().RootDrive,
                BobSettings.GetOrCreateSettings().RootAssetsFolder,
                BobSettings.GetOrCreateSettings().AssetsFolder[Bob.ProdVariables.AssetType.SetElement],
                BobSettings.GetOrCreateSettings().AssetsExportFolder);
            if (!Directory.Exists(lastFBXFolder_))
            {
                lastFBXFolder_ = null;
            }
        }

        /// <summary>Handles the input.</summary>
        private void Update()
        {
            ProcessInput(IsCameraAnimated);

            if (!EventSystem.current.IsPointerOverGameObject())
            {
                if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(2))
                    isFocusOnViewer = true;
            }
            if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(2))
            {
                isFocusOnViewer = false;
            }
        }

        public void ResetView()
        {
            ModelTransformChanged();
            // Duee to the normalisation in ActualCameraZoom()
            SetCameraZoom(20.0f / 3.0f);
        }

        /// <summary>Handles the input and moves the Camera accordingly.</summary>
        private void ProcessInput(bool forceUpdate = false)
        {
            if (!_mainCamera.enabled)
            {
                return;
            }
            ProcessInputInternal(_mainCamera.transform, forceUpdate);
        }

        /// <summary>Updates the Camera based on mouse Input.</summary>
        private void UpdateCamera()
        {
            cameraAngle.x = Mathf.Repeat(cameraAngle.x + GetAxis("Mouse X") * sensitivityX, 360f);
            //CameraAngle.y = Mathf.Repeat(CameraAngle.y + GetAxis("Mouse Y") * sensitivityY, 360f);
            cameraAngle.y = Mathf.Clamp(cameraAngle.y + GetAxis("Mouse Y") * sensitivityY, -MaxPitch, MaxPitch);
        }

        /// <summary>
        /// Handles the input using the given Camera.
        /// </summary>
        /// <param name="cameraTransform">The Camera to process input movements.</param>
        private void ProcessInputInternal(Transform cameraTransform, bool forceUpdate = false)
        {
            if (isChangingFov)
            {
                cameraTransform.GetComponent<Camera>().fieldOfView = currentFov;
                isChangingFov = false;
            }

            if (forceUpdate || !EventSystem.current.IsPointerOverGameObject())
            {
                if (isFocusOnViewer)
                {
                    if (GetMouseButton(0))
                    {
                        if (!GetKey(KeyCode.LeftAlt) && !GetKey(KeyCode.RightAlt))
                        {
                            UpdateCamera();
                        }
                    }
                    if (GetMouseButton(2))
                    {
                        cameraPivot -= cameraTransform.up * GetAxis("Mouse Y") * InputMultiplier + cameraTransform.right * GetAxis("Mouse X") * InputMultiplier;
                    }
                }

                // Handle camera zoom multiplier
                bool cameraZoomGotChanged = false;
                if (GetKey(KeyCode.LeftControl) || GetKey(KeyCode.RightControl))
                {
                    float newValue = Mathf.Max(0.0f, Mathf.Min(10.0f, cameraZoomMultiplier + GetMouseScrollDelta().y));
                    SetCameraZoom(newValue);
                    cameraZoomGotChanged = true;
                }

                if (!cameraZoomGotChanged)
                {
                    float actualZoomMultiplier = ActualCameraZoom();
                    cameraDistance = Mathf.Min(cameraDistance - GetMouseScrollDelta().y * actualZoomMultiplier * InputMultiplier,
                        InputMultiplier * (1f / InputMultiplierRatio) * MaxCameraDistanceRatio);
                    if (cameraDistance < 0f)
                    {
                        cameraPivot += cameraTransform.forward * -cameraDistance;
                        cameraDistance = 0f;
                    }
                }

                cameraTransform.position = cameraPivot + SphericalToCartesian(cameraDistance, cameraAngle.x * Mathf.Deg2Rad, cameraAngle.y * Mathf.Deg2Rad);
                cameraTransform.LookAt(cameraPivot);
            }
        }

        /// <summary>Event triggered when the user selects a file or cancels the Model selection dialog.</summary>
        /// <param name="hasFiles">If any file has been selected, this value is <c>true</c>, otherwise it is <c>false</c>.</param>
        protected void OnBeginLoadModel(bool hasFiles)
        {
            if (hasFiles)
            {
                if (RootGameObject != null)
                {
                    Destroy(RootGameObject);
                    materialManager.DestroyWireframe();
                }
                SetLoading(true);

                // _animations = null;
                _stopwatch = new Stopwatch();
                _stopwatch.Start();
            }
            else
            {
                Utils.LoadingFinish();
            }
        }

        /// <summary>Event triggered when the Model Meshes and hierarchy are loaded.</summary>
        /// <param name="assetLoaderContext">The Asset Loader Context reference. Asset Loader Context contains the Model loading data.</param>
        protected void OnLoad(AssetLoaderContext assetLoaderContext)
        {
            ResetModelScale();
            _cameras = null;
            // _animation = null;
            _mainCamera.enabled = true;
            if (assetLoaderContext.RootGameObject != null)
            {
                if (assetLoaderContext.Options.ImportCameras)
                {
                    _cameras = assetLoaderContext.RootGameObject.GetComponentsInChildren<Camera>();
                    if (_cameras.Count > 0)
                    {
                    }
                    else
                    {
                        _cameras = null;
                    }
                }
                // _animation = assetLoaderContext.RootGameObject.GetComponent<Animation>();

                RootGameObject = assetLoaderContext.RootGameObject;
            }
            ModelTransformChanged();
        }

        /// <summary>
        /// Changes the camera placement when the Model has changed.
        /// </summary>
        protected virtual void ModelTransformChanged()
        {
            if (RootGameObject != null && _mainCamera.enabled)
            {
                cameraPivot = GetModelBoundCenter();
                cameraDistance = (_mainCamera.transform.position - cameraPivot).magnitude;
                cameraAngle = new Vector2(0f, 0f);
                ProcessInput(true);
            }
        }

        /// <summary>
        /// Get the bound center of the loaded model
        /// </summary>
        /// <returns>Bound center of the loaded model</returns>
        public Vector3 GetModelBoundCenter()
        {
            Bounds bounds = RootGameObject.CalculateBounds();
            if (bounds.size != Vector3.zero)
            {
                _mainCamera.FitToBounds(bounds, CameraDistanceRatio);
                InputMultiplier = bounds.size.magnitude * InputMultiplierRatio;
            }
            return bounds.center;
        }

        /// <summary>
        /// Event is triggered when any error occurs.
        /// </summary>
        /// <param name="contextualizedError">The Contextualized Error that has occurred.</param>
        protected void OnError(IContextualizedError contextualizedError)
        {
            Debug.LogError(contextualizedError);
            RootGameObject = null;
            SetLoading(false);

            _stopwatch?.Stop();
        }

        /// <summary>Event is triggered when the Model (including Textures and Materials) has been fully loaded.</summary>
        /// <param name="assetLoaderContext">The Asset Loader Context reference. Asset Loader Context contains the Model loading data.</param>
        protected void OnMaterialsLoad(AssetLoaderContext assetLoaderContext)
        {
            SetLoading(false);

            lastFBXFolder_ = Path.GetDirectoryName(assetLoaderContext.Filename);

            isMaterialsLoaded = true;

            _stopwatch.Stop();
            //var loadedText = $"Loaded in: {_stopwatch.Elapsed.Minutes:00}:{_stopwatch.Elapsed.Seconds:00}";

            ModelTransformChanged();

            eventOnLoadedName.Invoke(assetLoaderContext.Filename);

            eventOnLoaded.Invoke(RootGameObject);
        }

        public void SetAssetLoaderOptions(AssetLoaderOptions value)
        {
            AssetLoaderOptions = value;
        }

        public AssetLoaderOptions GetAssetLoaderOptions()
        {
            return AssetLoaderOptions;
        }


        /// <summary>
        /// Converts a point from Spherical coordinates to Cartesian (using positive * Y as up). All angles are in radians.
        /// </summary>
        /// <param name="radius">Radius (similar to Camera distance)</param>
        /// <param name="polar">Polar (similar to pitch angle)</param>
        /// <param name="elevation"Elevation (similar to yaw angle)></param>
        /// <returns>Cartesian coordinates</returns>
        public static Vector3 SphericalToCartesian(float radius, float polar, float elevation)
        {
            Vector3 res = new Vector3();
            float a = radius * Mathf.Cos(elevation);
            res.x = a * Mathf.Cos(polar);
            res.y = radius * Mathf.Sin(elevation);
            res.z = a * Mathf.Sin(polar);
            return res;
        }


        /// <summary>
        /// Converts a point from Cartesian coordinates (using positive Y as up) to Spherical and stores the results in a vector 3 (Radius, Polar, Elevation).
        /// </summary>
        /// <param name="cartCoords">Cartesian coordinates</param>
        /// <returns>Spherical coordinates</returns>
        public static Vector3 CartesianToSpherical(Vector3 cartCoords)
        {
            Vector3 res = new Vector3();
            if (cartCoords.x == 0)
                cartCoords.x = Mathf.Epsilon;
            res.x = Mathf.Sqrt((cartCoords.x * cartCoords.x)
                            + (cartCoords.y * cartCoords.y)
                            + (cartCoords.z * cartCoords.z));
            res.y = Mathf.Atan(cartCoords.z / cartCoords.x);
            if (cartCoords.x < 0)
                res.y += Mathf.PI;
            res.z = Mathf.Asin(cartCoords.y / res.x);

            return res;
        }

        /// <summary>
        /// Set the spherical position of the camera
        /// </summary>
        /// <param name="cameraAngle">pitch and yaw angles</param>
        /// <param name="cameraDistance">distance between the camera and the pivot</param>
        /// <param name="cameraPivot">pivot of the camera</param>
        public void SetCameraSphericalPosition(Vector2 cameraAngle, float cameraDistance, Vector3 cameraPivot)
        {
            this.cameraAngle = cameraAngle;
            this.cameraDistance = cameraDistance;
            this.cameraPivot = cameraPivot;
        }
    }
}