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
        public AssetViewerManager assetViewerManager;

        public GameObject rootGameObject;

        public Slider fovSlider;

        public float TransitionDuration;

        public List<CameraCaptureSettings> BlenderCameras = new List<CameraCaptureSettings>();

        public Action CameraLoaded;

        private Coroutine transition = null;

        /// <summary>
        /// Parse the fbx file to find camera object and add it to the BlenderCameras list
        /// </summary>
        public void LoadBlenderCameras()
        {
            BlenderCameras.Clear();

            foreach (Transform child in rootGameObject.transform.GetChild(0).transform)
            {
                if (child.name.ToLower().Contains("camera"))
                {
                    string[] camParameters = child.name.Split("_");
                    float focalLength = 50.0f;

                    if (camParameters.Length > 1)
                    {
                        float.TryParse(camParameters[1], out focalLength);
                    }

                    float pivotDistance = (child.position - assetViewerManager.GetModelBoundCenter()).magnitude;
                    Debug.Log(pivotDistance);
                    Vector3 orbitalPivot = child.position - pivotDistance * child.right;
                    Vector3 camPosFromPivot = child.position - orbitalPivot;
                    var coord = AssetViewerManager.CartesianToSpherical(camPosFromPivot);
                    child.rotation = Quaternion.LookRotation(-child.right);

                    BlenderCameras.Add(new CameraCaptureSettings(child.name, child, focalLength, new Vector2(coord.y * Mathf.Rad2Deg, coord.z * Mathf.Rad2Deg), orbitalPivot, pivotDistance));
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
            if (camIndex < 0 || camIndex >= BlenderCameras.Count)
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

                    Camera.main.focalLength = Mathf.Lerp(initialFocalLength, BlenderCameras[camIndex].FocalLength, a);
                    fovSlider.value = Camera.main.fieldOfView;
                    assetViewerManager.CameraAngle = Vector2.Lerp(initialAngle, BlenderCameras[camIndex].OrbitalAngle, a);
                    assetViewerManager.CameraDistance = Mathf.Lerp(initialCamDist, BlenderCameras[camIndex].OrbitalDistance, a);
                    assetViewerManager.CameraPivot = Vector3.Lerp(intialPivot, BlenderCameras[camIndex].OrbitalPivot, a);
                    
                    yield return null;
                }

            yield return null;

            Camera.main.focalLength = BlenderCameras[camIndex].FocalLength;
            fovSlider.value = Camera.main.fieldOfView;
            assetViewerManager.CameraAngle = BlenderCameras[camIndex].OrbitalAngle;
            assetViewerManager.CameraDistance = BlenderCameras[camIndex].OrbitalDistance;
            assetViewerManager.CameraPivot = BlenderCameras[camIndex].OrbitalPivot;
            transition = null;

            yield return null;

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
