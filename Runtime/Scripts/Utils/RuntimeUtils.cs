using StudioManette.Gradients;
using System;
using UnityEngine;
using static StudioManette.Edna.CommonUtils;

namespace StudioManette.Edna
{
    public static class RuntimeUtils
    {

        private static ViewerManager _viewerManager = null;

        static RuntimeUtils()
        {
            // Register all external modules callbacks
            PresetGradientManager.OnAddPresetFailure.AddListener(Alert);
            GradientPicker.OnFailure.AddListener(Alert);
            GradientPicker.OnCopyPasteConfirmation += ConfirmGradientPicker;
        }

        public static ViewerManager GetManager()
        {
            if (_viewerManager == null)
            {
                // This might fail if called from another thread than the main!
                try
                {
                    _viewerManager = GameObject.FindGameObjectWithTag("GameController").GetComponent<ViewerManager>();
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message);
                }
            }
            if (_viewerManager == null)
            {
                Debug.LogError("Could not find a GameController with ViewerManager cpnt!");
            }
            return _viewerManager;
        }

        public static void Alert(string errorMessage)
        {
            Debug.LogWarning("ALERT : " + errorMessage);
            GetManager().errorWindow.Alert(errorMessage);
        }

        public static void Confirm(string infoMessage, Action onConfirm, Action onCancel = null, string promptYes = "Yes", string promptNo = "No")
        {
            GetManager().confirmWindow.Confirm(infoMessage, onConfirm, onCancel, promptYes, promptNo);
        }

        public static void LoadingProgress(float completion, string message = null)
        {
            GetManager().loadingWindow.SetProgress(completion, message);

            if (completion == 1.0f)
            {
                LoadingFinish();
            }
        }

        public static void LoadingInit(string message, string subMessage = "  ")
        {
            GetManager().loadingWindow.gameObject.SetActive(true);
            GetManager().loadingWindow.SetMainText(message);
            GetManager().loadingWindow.SetProgress(0.0f, subMessage);
        }

        public static void LoadingFinish()
        {
            GetManager().loadingWindow.gameObject.SetActive(false);
        }

        public static void ConfirmGradientPicker(string infoMessage, Action onConfirm, Action onCancel = null)
        {
            Confirm(infoMessage, onConfirm, onCancel);
        }

        public static void LogDebug(string message)
        {
            Debug.Log(message);
        }

        public static void LogWarning(string message)
        {
            Debug.LogWarning(message);
        }

        public static void LogError(string message)
        {
            Debug.LogError(message);
        }

        public static void Log(string message, LogPriority priority = LogPriority.INFO)
        {
            switch (priority)
            {
                case LogPriority.ERROR_ALERT: Alert(message); break;
                case LogPriority.ERROR: LogError(message); break;
                case LogPriority.WARNING: LogWarning(message); break;
                case LogPriority.INFO: LogDebug(message); break;
            }
        }

        /// <summary>
        /// Change the parent material of the gameobject and its children
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="newParentMaterial"></param>
        public static void ChangeParentMaterial(GameObject gameObject, Material newParentMaterial)
        {
            Renderer renderer;
            if (gameObject.TryGetComponent<Renderer>(out renderer))
            {
                renderer.material.parent = newParentMaterial;
            }

            if (gameObject.transform.childCount > 0)
            {
                foreach (Transform child in gameObject.transform)
                {
                    ChangeParentMaterial(child.gameObject, newParentMaterial);
                }
            }
        }

        /// <summary>
        /// Retrieve the parent material of the gameobject
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        public static Material GetParentMaterial(GameObject gameObject)
        {
            Renderer renderer;
            if (gameObject.TryGetComponent<Renderer>(out renderer))
            {
                return renderer.material.parent;
            }
            else if (gameObject.transform.childCount > 0)
            {
                foreach (Transform child in gameObject.transform)
                {
                    Material res = GetParentMaterial(child.gameObject);
                    if (res != null)
                    {
                        return res;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Retrieve the material of the gameobject
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        public static Material GetMaterial(GameObject gameObject)
        {
            Renderer renderer;
            if (gameObject.TryGetComponent<Renderer>(out renderer))
            {
                return renderer.material;
            }
            else if (gameObject.transform.childCount > 0)
            {
                foreach (Transform child in gameObject.transform)
                {
                    Material res = GetMaterial(child.gameObject);
                    if (res != null)
                    {
                        return res;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Change the material of the gameobject and its children
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="newMaterial"></param>
        public static void ChangeMaterial(GameObject gameObject, Material newMaterial)
        {
            Renderer renderer;
            if (gameObject.TryGetComponent<Renderer>(out renderer))
            {
                renderer.material = newMaterial;
            }

            if (gameObject.transform.childCount > 0)
            {
                foreach (Transform child in gameObject.transform)
                {
                    ChangeMaterial(child.gameObject, newMaterial);
                }
            }
        }
    }
}
