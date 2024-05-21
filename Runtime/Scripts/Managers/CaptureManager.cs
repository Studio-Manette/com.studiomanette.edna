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
        // UI elements
        public Canvas canvasCapture;
        public Button quickCaptureButton;
        public TextMeshProUGUI folderCaptureUI;

        // Camera for capturing screenshots
        public Camera cameraCapture;
        public Vector2 captureDimensions = new Vector2(1920f, 1080f); // Default dimensions for the capture

        // Capture related fields
        private string folderCapturePath;
        private List<bool> uiToDIsableStates;
        private string filePath = "";

        // Root game object and camera manager
        public GameObject rootGameObject;
        public CameraManager cameraManager;

        // Shortcut for taking a capture
        public KeyCode captureShortcut = KeyCode.Insert;

        // List of cameras to capture from
        private List<CameraCaptureSettings> trCamerasToCapture = new List<CameraCaptureSettings>();

        // Capture timing settings
        public float waitingTimeTakingScreenshot = 0.5f;
        public int refreshRenderCount = 50;
        public int camCaptureCount;

        private const string MAIN_CAMERA_NAME = "Main"; // Name of the main camera

        public void OnEnable()
        {
            // Disable all UI elements initially and set default capture folder text
            ActiveAll(false);
            quickCaptureButton.interactable = false;
            folderCaptureUI.text = "please capture a screenshot first";
        }

        private void UpdateTakeScreenShot()
        {
            int captureDimX = (int)captureDimensions.x;
            int captureDimY = (int)captureDimensions.y;

            // Create a new RenderTexture and Texture2D for capturing the screenshot
            RenderTexture targetTexture = new RenderTexture(captureDimX, captureDimY, 24, RenderTextureFormat.ARGB32);
            Texture2D renderResult = new Texture2D(captureDimX, captureDimY, TextureFormat.RGB24, false);

            // Set the camera's target texture and render the scene multiple times to improve quality
            cameraCapture.targetTexture = targetTexture;
            for (int i = 0; i < refreshRenderCount; i++)
            {
                cameraCapture.Render();
            }

            // Copy the rendered image into the Texture2D
            RenderTexture.active = targetTexture;
            renderResult.ReadPixels(new Rect(0, 0, captureDimX, captureDimY), 0, 0);
            renderResult.Apply();

            // Encode the image to PNG and save it to the specified file path
            byte[] byteArray = renderResult.EncodeToPNG();
            File.WriteAllBytes(filePath, byteArray);

            Utils.Log("Capture well saved : " + filePath);

            // Reset the camera's target texture
            cameraCapture.targetTexture = null;

            // Restore UI and state after a short delay
            Invoke(nameof(RestoreAfterCapture), 0.1f);
        }

        public void OnClickCaptureScreenShot()
        {
            // Prepare to capture from the main camera
            trCamerasToCapture = new List<CameraCaptureSettings>
            {
                new CameraCaptureSettings(MAIN_CAMERA_NAME, Camera.main.transform, Camera.main.focalLength)
            };

            // Prepare for capture and open the file panel to choose a save location
            PrepareCapture();
            Invoke(nameof(LaunchFilePanel), 0.1f);
        }

        public void OnClickQuickCapture()
        {
            // Check if the capture folder exists, if not prompt the user to select one
            if (!Directory.Exists(folderCapturePath))
            {
                if (!OnSaveFolderSelected(StandaloneFileBrowser.OpenFolderPanel("Select a root folder for your captures", "", false)))
                {
                    Utils.Alert("Before make a capture, you need to define a folder.");
                    return;
                }
            }

            // Prepare to capture from the main camera
            trCamerasToCapture = new List<CameraCaptureSettings>
            {
                new CameraCaptureSettings(MAIN_CAMERA_NAME, Camera.main.transform, Camera.main.focalLength)
            };

            // Launch the capture process
            LaunchMultipleCameraCapture();
        }

        public void OnClickChooseCaptureRootDirectory()
        {
            // Open a folder panel for selecting the capture root directory
            OnSaveFolderSelected(StandaloneFileBrowser.OpenFolderPanel("Select a root folder for your captures", "", false));
        }

        private bool OnSaveFolderSelected(IList<ItemWithStream> folders)
        {
            // Check if a valid folder was selected
            if (folders.Count < 1)
                return false;

            if (folders[0] != null && !string.IsNullOrEmpty(folders[0].Name) && Directory.Exists(folders[0].Name))
            {
                folderCapturePath = folders[0].Name;
                folderCaptureUI.text = folderCapturePath;
                return true;
            }
            else return false;
        }

        public void LaunchMultipleCameraCapture()
        {
            // Set the capture count and prepare for capture
            camCaptureCount = trCamerasToCapture.Count;
            PrepareCapture();
            QuickCapture();
        }

        public Button button;

        public void OnClickQuickCaptureBlenderCameras()
        {
            // Check if the capture folder exists, if not prompt the user to select one
            if (!Directory.Exists(folderCapturePath))
            {
                if (!OnSaveFolderSelected(StandaloneFileBrowser.OpenFolderPanel("Select a root folder for your captures", "", false)))
                {
                    Utils.Alert("Before make a capture, you need to define a folder.");
                    return;
                }
            }

            // Clear the list of cameras to capture from and add Blender cameras
            trCamerasToCapture.Clear();
            trCamerasToCapture = new List<CameraCaptureSettings>(cameraManager.BlenderCameras);

            if (trCamerasToCapture.Count == 0)
            {
                Utils.Alert("No custom Camera is available in fbx file");
            }
            else
            {
                LaunchMultipleCameraCapture();
            }
        }

        private void QuickCapture()
        {
            // Start the capture process
            Capture();
        }

        private string GetAvailablePathForCapture(string captureName = "capture")
        {
            // Generate a unique file path for the capture to avoid overwriting existing files
            int increment = 0;
            string path;
            do
            {
                increment++;
                path = folderCapturePath + "/" + captureName + "_" + increment.ToString("###") + ".png";
            }
            while (File.Exists(path));

            return path;
        }

        public void PrepareCapture()
        {
            // Initialize the capture process and activate the capture canvas
            Utils.LoadingInit("Take capture...", filePath);
            ActiveAll(true);
            canvasCapture.gameObject.SetActive(true);

            // Process all CopyText components in the capture canvas
            foreach (CopyText ct in canvasCapture.GetComponentsInChildren<CopyText>(true))
            {
                ct.Process();
            }
        }

        public void Capture(string capturePath = "")
        {
            // Check if there are any cameras to capture from, if not add the main camera
            if (trCamerasToCapture == null || trCamerasToCapture.Count == 0)
            {
                camCaptureCount = 1;
                trCamerasToCapture = new List<CameraCaptureSettings>
                {
                    new CameraCaptureSettings(MAIN_CAMERA_NAME, Camera.main.transform, Camera.main.focalLength)
                };
            }

            // Calculate the progress of the capture process
            float progress = (((camCaptureCount + 1 - trCamerasToCapture.Count) * 1.0f / camCaptureCount * 1.0f));
            Utils.LoadingProgress(progress, filePath);

            Camera mainCamera = Camera.main;
            Transform tr = trCamerasToCapture[0].Transform;

            if (tr == mainCamera.transform)
            {
                // If capturing from the main camera, set the capture camera's position and rotation to match
                cameraCapture.transform.SetPositionAndRotation(mainCamera.transform.position, mainCamera.transform.rotation);
                cameraCapture.fieldOfView = mainCamera.fieldOfView;

                // Set the file path for the capture
                filePath = capturePath == "" ? GetAvailablePathForCapture(rootGameObject.transform.GetChild(0).transform.name) : capturePath;
            }
            else
            {
                // If capturing from a custom camera, set the capture camera's settings and create necessary directories
                cameraCapture.transform.SetPositionAndRotation(trCamerasToCapture[0].Transform.position, trCamerasToCapture[0].Transform.rotation);
                cameraCapture.focalLength = trCamerasToCapture[0].FocalLength;

                string previousFolderPath = folderCapturePath;
                folderCapturePath = Path.Combine(folderCapturePath, rootGameObject.transform.GetChild(0).name);
                if (!File.Exists(folderCapturePath))
                {
                    Directory.CreateDirectory(folderCapturePath);
                }

                // Set the file path for the capture
                filePath = GetAvailablePathForCapture(rootGameObject.transform.GetChild(0).transform.name + "_" + trCamerasToCapture[0].Name);
                folderCapturePath = previousFolderPath;
            }

            // Remove the captured camera from the list and schedule the screenshot update
            trCamerasToCapture.RemoveAt(0);
            Invoke(nameof(UpdateTakeScreenShot), waitingTimeTakingScreenshot);
        }

        private void ActiveAll(bool isActive)
        {
            // Activate or deactivate the capture camera and canvas
            cameraCapture.gameObject.SetActive(isActive);
            canvasCapture.gameObject.SetActive(isActive);
        }

        private void RestoreAfterCapture()
        {
            // Restore UI and state after capture
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
            // Open a file panel for selecting the save location for the capture
            var title = "Select a file to write";
            var extensions = new ExtensionFilter[]
            {
                new ExtensionFilter("PNG File (png)", "png")
            };
            StandaloneFileBrowser.SaveFilePanelAsync(title, null, "capture", extensions, OnSaveFileSelected);
        }

        private void OnSaveFileSelected(ItemWithStream file)
        {
            // Handle the file selected from the save file panel
            if (file != null && !string.IsNullOrEmpty(file.Name))
            {
                string fileNameWithExt = file.Name.EndsWith(".png") ? file.Name : (file.Name + ".png");

                folderCapturePath = CommonUtils.GetFolderfromPathName(fileNameWithExt);
                folderCaptureUI.text = folderCapturePath;
                quickCaptureButton.interactable = true;

                Capture(fileNameWithExt);
            }
            else
            {
                trCamerasToCapture.Clear();
                RestoreAfterCapture();
            }
        }

        private void Update()
        {
            // Check if the capture shortcut key is pressed and initiate quick capture
            if (Input.GetKeyDown(captureShortcut))
            {
                OnClickQuickCapture();
            }
        }
    }
}