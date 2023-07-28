using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;



namespace StudioManette.Edna
{
    /// <summary>
    /// Element used to describe a camera imported from fbx file
    /// </summary>
    [System.Serializable]
    public class CameraCaptureSettings
    {
        public string Name;
        public float FocalLength;
        public Vector3 OrbitalPivot;
        public Vector2 OrbitalAngle;
        public float OrbitalDistance;
        public Transform Transform;

        public CameraCaptureSettings(string name, Transform transform, float focalLength, Vector2 orbitalAngle = new Vector2(), Vector3 orbitalPivot = new Vector3(), float orbitalDistance =0f)
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
    ///  Manager that handles all actions related to cameras imported from a fbx file
    /// </summary>
    public class CameraManager : MonoBehaviour
    {
        [SerializeField]
        private AssetViewerManager assetViewerManager;

        [SerializeField]
        private GameObject rootGameObject;

        [SerializeField]
        private Slider fovSlider;

        [SerializeField]
        private float TransitionDuration;

        public List<CameraCaptureSettings> BlenderCameras
        {
            get => blenderCameras;
        }

        private List<CameraCaptureSettings> blenderCameras = new List<CameraCaptureSettings>();

        public Action CameraLoaded;

        private Coroutine transition = null;

        private const string CAMERA_PREFIX = "camera";
        private const string SEPARATOR = "_";

        /// <summary>
        /// Parse the fbx file to find camera object and add it to the BlenderCameras list
        /// </summary>
        public void LoadBlenderCameras()
        {
            blenderCameras.Clear();

            foreach (Transform child in rootGameObject.transform.GetChild(0).transform)
            {
                if (child.name.ToLower().Contains(CAMERA_PREFIX))
                {
                    string[] camParameters = child.name.Split(SEPARATOR);
                    float focalLength = 50.0f;

                    if (camParameters.Length > 1)
                    {
                        float.TryParse(camParameters[1], out focalLength);
                    }

                    float pivotDistance = (child.position - assetViewerManager.GetModelBoundCenter()).magnitude;
                    Vector3 orbitalPivot = child.position - pivotDistance * child.right;
                    Vector3 camPosFromPivot = child.position - orbitalPivot;
                    var coord = AssetViewerManager.CartesianToSpherical(camPosFromPivot);
                    child.rotation = Quaternion.LookRotation(-child.right);

                    blenderCameras.Add(new CameraCaptureSettings(child.name, child, focalLength, new Vector2(coord.y * Mathf.Rad2Deg, coord.z * Mathf.Rad2Deg), orbitalPivot, pivotDistance));
                }
            }

            CameraLoaded?.Invoke();
        }

        /// <summary>
        /// Move the main camera until it has the same position and rotation as the camera obtained from the given index.
        /// </summary>
        /// <param name="camIndex"></param>
        public void SetCameraView(int camIndex)
        {
            if (camIndex < 0 || camIndex >= blenderCameras.Count)
                return;
               
            if(transition != null)
            {
                StopCoroutine(transition);
            }
            transition = StartCoroutine(MoveToIndexedCamera(camIndex, TransitionDuration));
        }

        /// <summary>
        /// Routine that moves the main camera until it has the same position and rotation as the camera obtained from the given index within the given time.
        /// </summary>
        /// <param name="camIndex"></param>
        /// <param name="duration"></param>
        /// <returns></returns>
        IEnumerator MoveToIndexedCamera(int camIndex, float duration = 1.0f)
        {
            assetViewerManager.IsCameraAnimated = true;
            float t = 0;
            float initialFocalLength = Camera.main.focalLength;

            Vector3 intialPivot = assetViewerManager.CameraPivot;
            Vector2 initialAngle = assetViewerManager.CameraAngle;
            float initialCamDist = assetViewerManager.CameraDistance;

            if(TransitionDuration > 0)
                while (t < TransitionDuration)
                {
                    t += Time.deltaTime;

                    float a = easeOutCirc(t / duration);

                    Camera.main.focalLength = Mathf.Lerp(initialFocalLength, blenderCameras[camIndex].FocalLength, a);
                    fovSlider.value = Camera.main.fieldOfView;
                    assetViewerManager.SetCameraSphericalPosition(Vector2.Lerp(initialAngle, blenderCameras[camIndex].OrbitalAngle, a),
                                                        Mathf.Lerp(initialCamDist, blenderCameras[camIndex].OrbitalDistance, a),
                                                        Vector3.Lerp(intialPivot, blenderCameras[camIndex].OrbitalPivot, a));
                    
                    yield return null;
                }

            yield return null;

            Camera.main.focalLength = blenderCameras[camIndex].FocalLength;
            fovSlider.value = Camera.main.fieldOfView;
            assetViewerManager.SetCameraSphericalPosition(blenderCameras[camIndex].OrbitalAngle,
                                                blenderCameras[camIndex].OrbitalDistance,
                                                blenderCameras[camIndex].OrbitalPivot);

            yield return new WaitForEndOfFrame();

            transition = null;
            assetViewerManager.IsCameraAnimated = false;
        }

        /// <summary>
        /// Function that describes out circ easing curve
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        float easeOutCirc(float x)
        {
            return Mathf.Sqrt(1 - Mathf.Pow(x - 1, 2));
        }
    }

}
