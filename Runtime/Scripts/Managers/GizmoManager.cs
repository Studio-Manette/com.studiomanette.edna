using RuntimeSceneGizmo;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace StudioManette.Edna
{
    public class GizmoManager : MonoBehaviour
    {
        public SceneGizmoRenderer gizmoRenderer;
        public AssetViewerManager assetViewerManager;

        public Toggle toggle;

        private const float tweenDuration = 0.5f;

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyUp(KeyCode.G))
            {
                ToggleGizmo(!gizmoRenderer.gameObject.activeSelf);
            }
        }

        public void ToggleGizmo(bool isOn)
        {
            gizmoRenderer.gameObject.SetActive(isOn);
            toggle.isOn = isOn;
        }

        public void OnClickComponent(GizmoComponent gc)
        {
            switch (gc)
            {
                case GizmoComponent.Center:
                {
                    break;
                }
                case GizmoComponent.None:
                {
                    break;
                }
                case GizmoComponent.XPositive:
                {
                    assetViewerManager.TweenAngle(new Vector2(90, 0), tweenDuration);
                    break;
                }
                case GizmoComponent.XNegative:
                {
                    assetViewerManager.TweenAngle(new Vector2(270, 0), tweenDuration);
                    break;
                }
                case GizmoComponent.YPositive:
                {
                    assetViewerManager.TweenAngle(new Vector2(0, -80), tweenDuration);
                    break;
                }
                case GizmoComponent.YNegative:
                {
                    assetViewerManager.TweenAngle(new Vector2(0, 80), tweenDuration);
                    break;
                }
                case GizmoComponent.ZPositive:
                {
                    assetViewerManager.TweenAngle(new Vector2(0, 0), tweenDuration);
                    break;
                }
                case GizmoComponent.ZNegative:
                {
                    assetViewerManager.TweenAngle(new Vector2(180, 0), tweenDuration);
                    break;
                }
            }
        }
    }
}
