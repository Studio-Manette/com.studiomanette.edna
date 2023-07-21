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
                    //Vector3 orbitalPivot = child.position - pivotDistance * child.right;
                    Vector3 orbitalPivot = Vector3.zero;
                    Vector3 camPosFromPivot = child.position - orbitalPivot;

                    float r = camPosFromPivot.magnitude;
                    float theta = - Mathf.Acos(camPosFromPivot.z / r) * Mathf.Rad2Deg;
                    float phi = Mathf.Atan(camPosFromPivot.y / -camPosFromPivot.x) * Mathf.Rad2Deg;

                   //Debug.Log("r = " + r);
                   //Debug.Log("theta = " + theta + " | " + theta * Mathf.Rad2Deg);
                   //Debug.Log("phi = " + phi + " | " + phi * Mathf.Rad2Deg);

                    Vector3 forwardDirection = Vector3.forward;
                    Vector3 cameraDirection = (child.position - orbitalPivot).normalized;
                    //Vector2 orbitalAngle = new Vector2(Vector3.SignedAngle(forwardDirection, cameraDirection, Vector3.up), Vector2.SignedAngle(new Vector2(1, 0), new Vector2(cameraDirection.z, cameraDirection.y)));
                    Vector2 orbitalAngle = new Vector2(theta, phi);
                    Vector3 sphericalCoords = new Vector3(theta, phi, r);
                    Debug.Log("SphericalCoords = " + sphericalCoords);
                    Debug.Log("CartesianCoords = " + child.transform.position);
                    Debug.Log("SphericalCoordsConvertToCartesian = " + ConvertSphericalToCartesian(sphericalCoords));

                    //float orbitalDistance = (child.position - orbitalPivot).magnitude;
                    float orbitalDistance = r;
                    //Debug.Log(orbitalPivot + Quaternion.AngleAxis(orbitalAngle.x, Vector3.up) * Quaternion.AngleAxis(orbitalAngle.y, Vector3.right) * new Vector3(0f, 0f, Mathf.Max(0.01f, orbitalDistance)));
                    //child.position = orbitalPivot + Quaternion.AngleAxis(orbitalAngle.x, Vector3.up) * Quaternion.AngleAxis(orbitalAngle.y, Vector3.right) * new Vector3(0f, 0f, Mathf.Max(0.01f, orbitalDistance));
                    //child.LookAt(orbitalPivot);

                    Vector3 sphericalCoords1 = assetViewerManager.getSphericalCoordinates(camPosFromPivot);
                    Vector3 sphericalCoords2 = assetViewerManager.getSphericalCoordinates(new Vector3(camPosFromPivot.z, camPosFromPivot.x, camPosFromPivot.y));
                    Vector3 sphericalCoords3 = assetViewerManager.getSphericalCoordinates(new Vector3(camPosFromPivot.z, camPosFromPivot.y, camPosFromPivot.x));
                    Vector3 sphericalCoords4 = assetViewerManager.getSphericalCoordinates(new Vector3(camPosFromPivot.x, camPosFromPivot.z, camPosFromPivot.y));
                    Vector3 sphericalCoords5 = assetViewerManager.getSphericalCoordinates(new Vector3(camPosFromPivot.y, camPosFromPivot.z, camPosFromPivot.x));
                    Vector3 sphericalCoords6 = assetViewerManager.getSphericalCoordinates(new Vector3(camPosFromPivot.y, camPosFromPivot.x, camPosFromPivot.z));
                    Debug.LogWarning("SphericalCoords = " + sphericalCoords2);
                    Debug.LogWarning("CartesianCoords = " + child.transform.position);
                    //Debug.LogWarning("SphericalCoordsConvertToCartesian = " + assetViewerManager.getCartesianCoordinates(sphericalCoords2));
                    Debug.LogWarning("SphericalCoordsConvertToCartesian = " + assetViewerManager.getCartesianCoordinates(sphericalCoords1));
                    Debug.LogWarning("SphericalCoordsConvertToCartesian = " + assetViewerManager.getCartesianCoordinates(sphericalCoords2));
                    Debug.LogWarning("SphericalCoordsConvertToCartesian = " + assetViewerManager.getCartesianCoordinates(sphericalCoords3));
                    Debug.LogWarning("SphericalCoordsConvertToCartesian = " + assetViewerManager.getCartesianCoordinates(sphericalCoords4));
                    Debug.LogWarning("SphericalCoordsConvertToCartesian = " + assetViewerManager.getCartesianCoordinates(sphericalCoords5));
                    Debug.LogWarning("SphericalCoordsConvertToCartesian = " + assetViewerManager.getCartesianCoordinates(sphericalCoords6));

                    BlenderCameras.Add(new CameraCaptureSettings(child.name, child, focalLength, orbitalAngle, orbitalPivot, orbitalDistance));
                }
            }

            CameraLoaded?.Invoke();
        }

        public Vector3 ConvertSphericalToCartesian(Vector3 sphericalCoord)
        {
            Vector3 ret = new Vector3();
            ret.x = sphericalCoord.z * Mathf.Sin(sphericalCoord.x * Mathf.Deg2Rad) * Mathf.Cos(sphericalCoord.y * Mathf.Deg2Rad);
            ret.y = -sphericalCoord.z * Mathf.Sin(sphericalCoord.x * Mathf.Deg2Rad) * Mathf.Sin(sphericalCoord.y * Mathf.Deg2Rad);
            ret.z = sphericalCoord.z * Mathf.Cos(sphericalCoord.x * Mathf.Deg2Rad);
            return ret;
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
