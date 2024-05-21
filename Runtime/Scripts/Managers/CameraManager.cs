using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

namespace StudioManette.Edna
{
    /// <summary>
    /// Class representing the settings of a camera imported from an FBX file.
    /// </summary>
    [System.Serializable]
    public class CameraCaptureSettings
    {
        public string Name;                // Name of the camera.
        public float FocalLength;          // Focal length of the camera.
        public Vector3 OrbitalPivot;       // Pivot point for orbital rotation.
        public Vector2 OrbitalAngle;       // Angle for orbital rotation.
        public float OrbitalDistance;      // Distance for orbital rotation.
        public Transform Transform;        // Transform component of the camera.

        // Constructor to initialize the camera settings.
        public CameraCaptureSettings(string name, Transform transform, float focalLength, Vector2 orbitalAngle = new Vector2(), Vector3 orbitalPivot = new Vector3(), float orbitalDistance = 0f)
        {
            Name = name;
            FocalLength = focalLength;
            OrbitalAngle = orbitalAngle;
            OrbitalPivot = orbitalPivot;
            OrbitalDistance = orbitalDistance;
            Transform = transform;
        }
    }

    /// <summary>
    /// Manager class to handle actions related to cameras imported from an FBX file.
    /// </summary>
    public class CameraManager : MonoBehaviour
    {
        [SerializeField]
        private AssetViewerManager assetViewerManager;    // Reference to the asset viewer manager.

        [SerializeField]
        private GameObject rootGameObject;                // Root game object containing camera transforms.

        [SerializeField]
        private Slider fovSlider;                         // Slider for adjusting the field of view.

        [SerializeField]
        private float TransitionDuration;                 // Duration for camera transitions.

        // Public property to access the list of imported cameras.
        public List<CameraCaptureSettings> BlenderCameras
        {
            get => blenderCameras;
        }

        // Private list to store the imported cameras.
        private List<CameraCaptureSettings> blenderCameras = new List<CameraCaptureSettings>();

        // Event triggered when cameras are loaded.
        public Action CameraLoaded;

        // Coroutine for handling camera transitions.
        private Coroutine transition = null;

        // Constants for parsing camera names.
        private const string CAMERA_PREFIX = "camera";
        private const string SEPARATOR = "_";

        /// <summary>
        /// Loads cameras from the FBX file and adds them to the BlenderCameras list.
        /// </summary>
        public void LoadBlenderCameras()
        {
            blenderCameras.Clear();

            // Iterate through child transforms of the root game object.
            foreach (Transform child in rootGameObject.transform.GetChild(0).transform)
            {
                // Check if the child name contains the camera prefix.
                if (child.name.ToLower().Contains(CAMERA_PREFIX))
                {
                    string[] camParameters = child.name.Split(SEPARATOR);
                    float focalLength = 50.0f;

                    // Parse the focal length from the name if available.
                    if (camParameters.Length > 1)
                    {
                        float.TryParse(camParameters[1], out focalLength);
                    }

                    // Calculate the orbital pivot and distance.
                    float pivotDistance = (child.position - assetViewerManager.GetModelBoundCenter()).magnitude;
                    Vector3 orbitalPivot = child.position - pivotDistance * child.right;
                    Vector3 camPosFromPivot = child.position - orbitalPivot;
                    var coord = AssetViewerManager.CartesianToSpherical(camPosFromPivot);
                    child.rotation = Quaternion.LookRotation(-child.right);

                    // Add the camera to the list with its settings.
                    blenderCameras.Add(new CameraCaptureSettings(
                        camParameters.Length > 0 ? camParameters[0] : child.name, 
                        child, 
                        focalLength, 
                        new Vector2(coord.y * Mathf.Rad2Deg, coord.z * Mathf.Rad2Deg), 
                        orbitalPivot, 
                        pivotDistance));
                }
            }

            // Trigger the CameraLoaded event.
            CameraLoaded?.Invoke();
        }

        /// <summary>
        /// Sets the main camera to match the position and rotation of the camera at the specified index.
        /// </summary>
        /// <param name="camIndex">Index of the camera to set the view to.</param>
        public void SetCameraView(int camIndex)
        {
            // Check if the index is valid.
            if (camIndex < 0 || camIndex >= blenderCameras.Count)
                return;

            // Stop any ongoing transition.
            if (transition != null)
            {
                StopCoroutine(transition);
            }

            // Start the transition coroutine to move the camera.
            transition = StartCoroutine(MoveToIndexedCamera(camIndex, TransitionDuration));
        }

        /// <summary>
        /// Coroutine to move the main camera to the position and rotation of the camera at the specified index over a given duration.
        /// </summary>
        /// <param name="camIndex">Index of the camera to move to.</param>
        /// <param name="duration">Duration of the transition.</param>
        /// <returns></returns>
        IEnumerator MoveToIndexedCamera(int camIndex, float duration = 1.0f)
        {
            assetViewerManager.IsCameraAnimated = true;   // Indicate that the camera is being animated.
            float t = 0;                                  // Initialize the transition time.
            float initialFocalLength = Camera.main.focalLength;

            // Store the initial camera settings.
            Vector3 intialPivot = assetViewerManager.CameraPivot;
            Vector2 initialAngle = assetViewerManager.CameraAngle;
            float initialCamDist = assetViewerManager.CameraDistance;

            // Perform the transition if the duration is greater than zero.
            if (TransitionDuration > 0)
            {
                while (t < TransitionDuration)
                {
                    t += Time.deltaTime;

                    float a = easeOutCirc(t / duration);  // Calculate the easing value.

                    // Interpolate the camera settings based on the easing value.
                    Camera.main.focalLength = Mathf.Lerp(initialFocalLength, blenderCameras[camIndex].FocalLength, a);
                    fovSlider.value = Camera.main.fieldOfView;
                    assetViewerManager.SetCameraSphericalPosition(
                        Vector2.Lerp(initialAngle, blenderCameras[camIndex].OrbitalAngle, a),
                        Mathf.Lerp(initialCamDist, blenderCameras[camIndex].OrbitalDistance, a),
                        Vector3.Lerp(intialPivot, blenderCameras[camIndex].OrbitalPivot, a));
                    
                    yield return null;  // Wait for the next frame.
                }
            }

            // Ensure final camera settings match the target camera.
            Camera.main.focalLength = blenderCameras[camIndex].FocalLength;
            fovSlider.value = Camera.main.fieldOfView;
            assetViewerManager.SetCameraSphericalPosition(
                blenderCameras[camIndex].OrbitalAngle,
                blenderCameras[camIndex].OrbitalDistance,
                blenderCameras[camIndex].OrbitalPivot);

            yield return new WaitForEndOfFrame();  // Wait for the end of the frame.

            transition = null;  // Reset the transition.
            assetViewerManager.IsCameraAnimated = false;  // Indicate that the camera animation is complete.
        }

        /// <summary>
        /// Easing function for a smooth circular out transition.
        /// </summary>
        /// <param name="x">Input value between 0 and 1.</param>
        /// <returns>Output value between 0 and 1.</returns>
        float easeOutCirc(float x)
        {
            return Mathf.Sqrt(1 - Mathf.Pow(x - 1, 2));
        }
    }
}