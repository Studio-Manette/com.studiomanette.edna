using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace StudioManette.Edna
{
    public class MultiTab : MonoBehaviour
    {
        public Transform[] fileContents;
        public Transform[] hierarchyContents;

        public Transform[] cameraContents;
        public CameraList cameraList;

        public HistoryManager historyManager;
        public Transform[] historyContents;


        // Start is called before the first frame update
        void Awake()
        {
            //Default is file tab
            OnClickFileTab(true);
            OnClickCameraTab(false);
            OnClickHierarchyTab(false);
            OnClickHistoryTab(false);

            ActiveButtons(false);
        }

        public void ActiveButtons(bool isActive)
        {
            foreach (ButtonGroup bg in GetComponentsInChildren<ButtonGroup>())
            {
                bg.Activate(isActive);
            }
        }

        public void OnClickFileTab(bool isActive)
        {
            SetActiveTab(fileContents, isActive);
        }

        public void OnClickHierarchyTab(bool isActive)
        {
            SetActiveTab(hierarchyContents, isActive);
        }

        public void OnClickHistoryTab(bool isActive)
        {
            SetActiveTab(historyContents, isActive);

            historyManager.CheckSanity();
        }

        public void OnClickCameraTab(bool isActive)
        {
            SetActiveTab(cameraContents, isActive);
            cameraList.UpdateLayout();
        }

        private void SetActiveTab(Transform[] contents, bool isActive)
        {
            foreach (Transform tr in contents)
            {
                tr.gameObject.SetActive(isActive);
            }
        }
    }
}
