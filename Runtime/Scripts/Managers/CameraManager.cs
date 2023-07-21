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
        public Transform Transform;

        public CameraCaptureSettings(string name, Transform transform, float focalLength)
        {
            Name = name;
            FocalLength = focalLength;
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

                    child.rotation = Quaternion.LookRotation(-child.right);

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
               
            assetViewerManager.IsCameraLocked = true;
            Camera.main.focalLength = BlenderCameras[camIndex].FocalLength;
            Camera.main.transform.position = BlenderCameras[camIndex].Transform.position;
            Camera.main.transform.rotation = BlenderCameras[camIndex].Transform.rotation;
        }
}

}
