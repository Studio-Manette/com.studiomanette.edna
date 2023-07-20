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
        public Transform Transform;
        public float FocalLength;

        public CameraCaptureSettings(string name, Transform transform, float focalLength)
        {
            Name = name;
            Transform = transform;
            FocalLength = focalLength;
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

                    BlenderCameras.Add(new CameraCaptureSettings(child.name, child, focalLength));
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

            Vector3 actualPivot = assetViewerManager.CameraPivot;
            float pivotDistance = (BlenderCameras[camIndex].Transform.position - assetViewerManager.GetModelBoundCenter()).magnitude;
            Vector3 newPivot = BlenderCameras[camIndex].Transform.position - pivotDistance * BlenderCameras[camIndex].Transform.right;

            Vector3 forwardDirection = -rootGameObject.transform.forward;
            Vector3 cameraDirection = (BlenderCameras[camIndex].Transform.position - newPivot).normalized;

            Vector2 actualAngle = assetViewerManager.CameraAngle;
            Vector2 newAngle = new Vector2(Vector3.SignedAngle(forwardDirection, cameraDirection, Vector3.up), (forwardDirection - cameraDirection).magnitude);
            float newCamDist = (BlenderCameras[camIndex].Transform.position - newPivot).magnitude;
            float actualCamDist = assetViewerManager.CameraDistance;

            if(TransitionDuration > 0)
                while (t < TransitionDuration)
                {
                    t += Time.deltaTime;

                    float a = easeOutCirc(t / duration);

                    Camera.main.focalLength = Mathf.Lerp(initialFocalLength, BlenderCameras[camIndex].FocalLength, a);
                    fovSlider.value = Camera.main.fieldOfView;
                    assetViewerManager.CameraAngle = Vector2.Lerp(actualAngle, newAngle, a);
                    assetViewerManager.CameraDistance = Mathf.Lerp(actualCamDist, newCamDist, a);
                    assetViewerManager.CameraPivot = Vector3.Lerp(actualPivot, newPivot, a);
                    
                    yield return null;
                }

            yield return null;

            Camera.main.focalLength = BlenderCameras[camIndex].FocalLength;
            fovSlider.value = Camera.main.fieldOfView;
            assetViewerManager.CameraAngle = newAngle;
            assetViewerManager.CameraDistance = newCamDist;
            assetViewerManager.CameraPivot = newPivot;
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
