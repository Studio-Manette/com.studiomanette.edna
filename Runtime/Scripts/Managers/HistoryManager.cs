using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using StudioManette.Bob.Helpers;
using StudioManette.Bob.MBX;

using Utils = StudioManette.Edna.RuntimeUtils;

namespace StudioManette.Edna
{
    /*
     * Placement des snapshots :
     * C:\Users\gabt\AppData\Local\Temp\StudioManette\Edna - Studio Manette
     */

    public class HistoryManager : MonoBehaviour
    {
        public Transform SnapShotParent;
        public GameObject SnapShotPrefab;
        public Transform AddButton;

        private const string SNAPSHOT_KEY_SEPARATOR = "_#";

        private int numberSnaps = 0;

        public void OnClickAddSnapshot()
        {
            int fhLength = MBXWriter.Instance.FilesHistory.Length;

            if (fhLength > 0)
            {
                string oldFilepath = MBXWriter.Instance.FilesHistory[MBXWriter.Instance.FilesHistory.Length - 1];
                string newFilePath = CommonUtils.GetPathNameWithoutExtension(oldFilepath) + SNAPSHOT_KEY_SEPARATOR + numberSnaps + ".mbx";

                GameObject goSnap = GameObject.Instantiate(SnapShotPrefab);
                goSnap.transform.parent = SnapShotParent;
                goSnap.transform.SetSiblingIndex(0);

                System.IO.File.Copy(oldFilepath, newFilePath);

                goSnap.GetComponentInChildren<SnasphotHistory>().Init(newFilePath, numberSnaps);

                numberSnaps++;
            }
            else
            {
                Utils.Alert("No History available.");
            }
        }

        public void CheckSanity()
        {
            if (numberSnaps > 0)
            {
                MBXData currentData = Utils.GetManager().materialManager.GetCurrentMBXData();
                MBXData snapData = null;

                foreach (SnasphotHistory sh in SnapShotParent.GetComponentsInChildren<SnasphotHistory>())
                {
                    snapData = Serialiser.GetDataFromFile<MBXData>(sh.filePath);

                    sh.MarkAsDirty(TexturesHasChanged(snapData, currentData));
                }
            }
        }

        /// <summary>
        /// Obsolete function, do not use
        /// </summary>
        /// <param name="before"></param>
        /// <param name="after"></param>
        /// <returns> returns false everytime until the big is fixed</returns>
        private bool TexturesHasChanged(MBXData before, MBXData after)
        {
            return false;
            /*
             * Function obsolete because of Bob version ( #1004 )
             * https://studio-manette.mantishub.io/view.php?id=1004
             */

            /*
            MBXData diff = MBXData.Diff(before, after);

            foreach (MBXMaterial material in diff.materials)
            {
                if (material.properties != null)
                {
                    foreach (MBXMaterialProperty prop in material.properties)
                    {
                        if (prop.IsTexture)
                        {
                            // Modif de texture !
                            return true;
                        }
                    }
                }
            }
            // Pas de modif de texture
            return false;
            */
        }

        public void Clean()
        {
            numberSnaps = 0;
            //remove children of Snapshots transform

            List<Transform> childrenToDestroy = new List<Transform>();
            foreach (Transform child in SnapShotParent.transform)
            {
                childrenToDestroy.Add(child);
            }
            foreach (Transform child in childrenToDestroy)
            {
                GameObject.Destroy(child.gameObject);
            }
        }

        private void Awake()
        {
            AddButton.GetComponentInChildren<Button>().onClick.AddListener(OnClickAddSnapshot);
            Clean();
        }
    }
}
