using StudioManette.Edna;

using System.Collections;
using System.Collections.Generic;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

public class CameraList : MonoBehaviour
{
    [SerializeField]
    CameraManager cameraManager;

    [SerializeField]
    GameObject noCameraObject;

    [SerializeField]
    GameObject CameraButtonPrefab;

    [SerializeField]
    GameObject SpacePrefab;

    List<GameObject> instantiatedPrefabs = new List<GameObject>();

    private void Awake()
    {
        cameraManager.CameraLoaded += OnCameraLoaded;
        OnCameraLoaded();
    }

    private void OnCameraLoaded()
    {
        foreach (GameObject go in instantiatedPrefabs) 
        { 
            Destroy(go);
        }

        noCameraObject.SetActive(cameraManager.BlenderCameras.Count < 1);

        for(int i = 0; i < cameraManager.BlenderCameras.Count; i++)
        {
            GameObject go = Instantiate(CameraButtonPrefab, this.transform);
            int x = i;
            go.GetComponent<Button>().onClick.AddListener(() => { cameraManager.SetCameraView(x); });
            go.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = cameraManager.BlenderCameras[i].Name;
            instantiatedPrefabs.Add(go);
            instantiatedPrefabs.Add(Instantiate(SpacePrefab, this.transform));
        }

        UpdateLayout();
    }

    public void UpdateLayout()
    {
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent.GetComponent<RectTransform>());
    }
}
