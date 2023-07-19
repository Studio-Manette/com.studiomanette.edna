using Codice.Client.BaseCommands.CheckIn.Progress;

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace StudioManette.Edna
{
    /// <summary>
    /// Only used to workaround the fact the neither dictionaries nor tuples are automatically serialised by Unity
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

    public class CameraManager : MonoBehaviour
    {
        public AssetViewerManager assetViewerManager;

        public List<CameraCaptureSettings> BlenderCameras = new List<CameraCaptureSettings>();

        public GameObject rootGameObject;

        public Action CameraLoaded;

        public float TransitionDuration;

        private Coroutine transition = null;

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

        public void SetCameraView(int camIndex)
        {
            if (camIndex < 0 || camIndex >= BlenderCameras.Count)
                return;
               
            if(transition != null)
            {
                StopCoroutine(transition);
            }
            transition = StartCoroutine(MoveCamera(camIndex, TransitionDuration));
        }

        IEnumerator MoveCamera(int camIndex, float duration = 1.0f)
        {
            assetViewerManager.IsCameraAnimated = true;
            float t = 0;
            float initialFocalLength = Camera.main.focalLength;
            Vector3 intialPosition = Camera.main.transform.position;
            Quaternion initalRotation = Camera.main.transform.rotation; 
            
            Quaternion targetRotation = Quaternion.LookRotation(-BlenderCameras[camIndex].Transform.right);

            Vector3 pivot = assetViewerManager.CameraPivot;
            Vector3 cam = -rootGameObject.transform.forward;
            Vector3 dest = (BlenderCameras[camIndex].Transform.position - pivot).normalized;


            Debug.Log(Vector3.SignedAngle(cam, dest, Vector3.up));
            Debug.Log((cam - dest).magnitude);


            //Debug.Log(longitude + " " + latitude); 

            while (t < TransitionDuration)
            {
                t += Time.deltaTime;

                float a = easeOutCirc(t / duration);

                Camera.main.focalLength = Mathf.Lerp(initialFocalLength, BlenderCameras[camIndex].FocalLength, a);
                Camera.main.transform.position = Vector3.Lerp(intialPosition, BlenderCameras[camIndex].Transform.position, a);
                Camera.main.transform.rotation = Quaternion.Slerp(initalRotation, targetRotation, a);

                yield return null;
            }

            Camera.main.focalLength = BlenderCameras[camIndex].FocalLength;
            Camera.main.transform.position = BlenderCameras[camIndex].Transform.position;
            Camera.main.transform.rotation = targetRotation;

            transition = null;
            assetViewerManager.IsCameraAnimated = false;
        }

        float easeOutCirc(float x)
        {
            return Mathf.Sqrt(1 - Mathf.Pow(x - 1, 2));
        }
}

}
