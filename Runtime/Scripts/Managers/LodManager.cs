using System.Collections.Generic;
using UnityEngine;


namespace StudioManette.Edna
{
    /// <summary>
    /// Manager that handles all actions related to LOD
    /// </summary>
    public class LodManager : MonoBehaviour
    {
        [SerializeField]
        private MaterialManager MaterialManager;
        [SerializeField]
        private GameObject RootGameObject;
        [SerializeField]
        private List<LODGroup> Groups = new List<LODGroup>();
        [SerializeField]
        private TMPro.TMP_Dropdown dropdownUI;

        private int maxLod = 0;

        private void Awake()
        {
            MaterialManager.OnWireframeCreatedCallback += LoadWireframeLOD;
        }

        /// <summary>
        /// This function is responsible for loading the Level of Detail (LOD) groups of a given GameObject.
        /// </summary>
        public void LoadLODs()
        {
            Groups.Clear();

            AddLODGroup(RootGameObject.transform);
            LoadWireframeLOD();

            dropdownUI.ClearOptions();

            if(Groups.Count > 0)
            {
                List<string> options = new List<string>();

                for (int i = 0; i < maxLod; i++)
                {
                    options.Add("LOD " + i.ToString());
                }

                dropdownUI.AddOptions(options);

                SetLOD(0);
            }
            
        }

        /// <summary>
        /// This function recursively searches for LODGroups in the children of the given Transform and adds them to the 'Groups' list.
        /// </summary>
        /// <param name="transform">Parent transform used to search LODGroups</param>
        void AddLODGroup(Transform transform)
        {
            LODGroup group;
            if (transform.TryGetComponent<LODGroup>(out group))
            {
                Groups.Add(group);
                maxLod = Mathf.Max(maxLod, group.GetLODs().Length);
                group.enabled = false;
                return;
            }
            else
            {
                foreach(Transform t in transform)
                {
                    AddLODGroup(t);
                }
            }
        }

        /// <summary>
        /// This function loads the wireframe LOD by adding its LODGroup component to the 'Groups' list.
        /// </summary>
        public void LoadWireframeLOD()
        {
            AddLODGroup(MaterialManager.WireFrameGo.transform);
        }

        /// <summary>
        /// This function sets the LOD level for all the LODGroups stored in the 'Groups' list.
        /// </summary>
        /// <param name="index">Index of the LOD</param>
        public void SetLOD(int index)
        {
            foreach (LODGroup group in Groups)
            {
                foreach(Transform t in group.gameObject.transform)
                {
                    t.gameObject.SetActive(false);
                }

                var LODs = group.GetLODs();

                if (index < LODs.Length)
                {
                    foreach(Renderer r in LODs[index].renderers)
                    {
                        r.gameObject.SetActive(true);
                    }
                }
            }
        }
    }
}
