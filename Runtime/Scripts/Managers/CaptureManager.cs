using System.Collections.Generic;
using System.IO;
using TMPro;
using TriLibCore.SFB;
using UnityEngine;
using UnityEngine.UI;

using Utils = StudioManette.Edna.RuntimeUtils;

namespace StudioManette.Edna
{
    public class CaptureManager : MonoBehaviour
    {
        public Canvas canvasCapture;

        public Button quickCaptureButton;

        public TextMeshProUGUI folderCaptureUI;

        public Camera cameraCapture;
        public Vector2 captureDimensions = new Vector2(1920f, 1080f);

        private string folderCapturePath;
        private List<bool> uiToDIsableStates;

        private string filePath = "";

        public GameObject rootGameObject;

        private List<Transform> trCamerasToCapture;

        public float waitingTimeTakingScreenshot = 0.5f;
        public int refreshRenderCount = 50;

        public int camCaptureCount;

        public void OnEnable()
        {
            ActiveAll(false);
            quickCaptureButton.interactable = false;
            folderCaptureUI.text = "please capture a screenshot first";
        }

        private void UpdateTakeScreenShot()
        {
            int captureDimX = (int)captureDimensions.x;
            int captureDimY = (int)captureDimensions.y;

            RenderTexture targetTexture = new RenderTexture(captureDimX, captureDimY, 24, RenderTextureFormat.ARGB32);
            Texture2D renderResult = new Texture2D(captureDimX, captureDimY, TextureFormat.RGB24, false);
            cameraCapture.targetTexture = targetTexture;
            for (int i = 0; i < refreshRenderCount; i++)
            {
                //forcer plein de fois fois le render pour la GI
                cameraCapture.Render();
            }
            RenderTexture.active = targetTexture;
            renderResult.ReadPixels(new Rect(0, 0, captureDimX, captureDimY), 0, 0);
            renderResult.Apply();

            byte[] byteArray = renderResult.EncodeToPNG();
            File.WriteAllBytes(filePath, byteArray);

            Utils.Log("Capture well saved : " + filePath);

            cameraCapture.targetTexture = null;

            Invoke(nameof(RestoreAfterCapture), 0.1f);
        }

        public void OnClickCaptureScreenShot()
        {
            trCamerasToCapture = new List<Transform>();
            trCamerasToCapture.Add(Camera.main.transform);
            PrepareCapture();
            Invoke(nameof(LaunchFilePanel), 0.1f);
        }

        public void OnClickQuickCapture()
        {
            if (string.IsNullOrEmpty(folderCapturePath))
            {
                Utils.Alert("Before make a quick capture, you need to define a folder with a first screenshot. Click on the \"Capture Screenshot...\" button to configure it.");
            }
            else
            {
                trCamerasToCapture = new List<Transform>();
                trCamerasToCapture.Add(Camera.main.transform);
                LaunchMultipleCameraCapture();
            }
        }


        public void LaunchMultipleCameraCapture()
        {
            camCaptureCount = trCamerasToCapture.Count;
            PrepareCapture();
            QuickCapture();
        }

        public Button button;

        public void OnClickQuickCaptureBlenderCameras()
        {
            if (string.IsNullOrEmpty(folderCapturePath))
            {
                Utils.Alert("Before make a quick capture, you need to define a folder with a first screenshot. Click on the \"Capture Screenshot...\" button to configure it.");
            }
            else
            {
                trCamerasToCapture = new List<Transform>();
                Transform[] camsTrans = rootGameObject.GetComponentsInChildren<Transform>();
                foreach (Transform tr in camsTrans)
                {
                    if (tr.name.Contains("Camera"))
                    {

                        trCamerasToCapture.Add(tr);
                    }
                }
                if (trCamerasToCapture.Count == 0)
                {
                    Utils.Alert("Not custom Camera is available in fbx file");
                }
                else
                {
                    LaunchMultipleCameraCapture();
                }
            }
        }

        private void QuickCapture()
        {
            Capture(GetAvailablePathForCapture());
        }

        private string GetAvailablePathForCapture()
        {
            int increment = 0;
            string path;
            do
            {
                increment++;
                path = folderCapturePath + "/capture_" + increment.ToString("###") + ".png";
            }
            while (File.Exists(path));

            return path;
        }

        public void PrepareCapture()
        {
            Utils.LoadingInit("Take capture...", filePath);
            ActiveAll(true);
            canvasCapture.gameObject.SetActive(true);
            foreach (CopyText ct in canvasCapture.GetComponentsInChildren<CopyText>(true))
            {
                ct.Process();
            }
        }

        public void Capture(string capturePath)
        {
            filePath = capturePath;

            if (trCamerasToCapture == null || trCamerasToCapture.Count == 0)
            {
                camCaptureCount = 1;
                trCamerasToCapture = new List<Transform>();
                trCamerasToCapture.Add(Camera.main.transform);
            }
            float progress = (((camCaptureCount+1 - trCamerasToCapture.Count)*1.0f / camCaptureCount*1.0f) * 100.0f);

            Utils.LoadingProgress(progress, filePath);
            Camera mainCamera = Camera.main;
            Transform tr = trCamerasToCapture[0];

            if (tr == mainCamera.transform)
            {
                cameraCapture.transform.SetPositionAndRotation(mainCamera.transform.position, mainCamera.transform.rotation);
                cameraCapture.fieldOfView = mainCamera.fieldOfView;
            }
            else
            {
                cameraCapture.transform.position = tr.position;
                Vector3 newDirection = Vector3.RotateTowards(cameraCapture.transform.forward, -tr.transform.right, 100, 0);
                cameraCapture.transform.rotation = Quaternion.LookRotation(newDirection);
                cameraCapture.focalLength = 50.0f;
            }

            trCamerasToCapture.RemoveAt(0);

            Invoke(nameof(UpdateTakeScreenShot), waitingTimeTakingScreenshot);
        }


        private void ActiveAll(bool isActive)
        {
            cameraCapture.gameObject.SetActive(isActive);
            canvasCapture.gameObject.SetActive(isActive);
        }

        private void RestoreAfterCapture()
        {
            if (trCamerasToCapture.Count == 0)
            {
                ActiveAll(false);
                Utils.LoadingFinish();
            }
            else
            {
                QuickCapture();
            }
        }

        private void LaunchFilePanel()
        {
            var title = "Select a file to write";
            var extensions = new ExtensionFilter[]
            {
                new ExtensionFilter("PNG File (png)", "png")
            };
            StandaloneFileBrowser.SaveFilePanelAsync(title, null, "capture", extensions, OnSaveFileSelected);
        }

        private void OnSaveFileSelected(ItemWithStream file)
        {
            if (file != null && !string.IsNullOrEmpty(file.Name))
            {
                string fileNameWithExt = file.Name.EndsWith(".png") ? file.Name : (file.Name + ".png");

                folderCapturePath = CommonUtils.GetFolderfromPathName(fileNameWithExt);
                folderCaptureUI.text = folderCapturePath;
                quickCaptureButton.interactable = true;

                Capture(fileNameWithExt);
            }
            else RestoreAfterCapture();
        }
    }
}
