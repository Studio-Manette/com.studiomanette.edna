using System.Collections.Generic;
using UnityEngine;

namespace StudioManette.Edna
{
    /// <summary>
    /// Manager that handles all actions related to Level of Detail (LOD).
    /// </summary>
    public class LodManager : MonoBehaviour
    {
        [SerializeField]
        private MaterialManager MaterialManager; // Reference to the material manager.

        [SerializeField]
        private GameObject RootGameObject; // Root game object containing LODGroups.

        [SerializeField]
        private List<LODGroup> Groups = new List<LODGroup>(); // List of LODGroups.

        [SerializeField]
        private TMPro.TMP_Dropdown dropdownUI; // UI dropdown for selecting LOD levels.

        private int maxLod = 0; // Maximum LOD level available.

        private void Awake()
        {
            // Subscribe to the OnWireframeCreatedCallback event to load wireframe LODs.
            MaterialManager.OnWireframeCreatedCallback += LoadWireframeLOD;
        }

        /// <summary>
        /// This function is responsible for loading the Level of Detail (LOD) groups of a given GameObject.
        /// </summary>
        public void LoadLODs()
        {
            // Clear the current list of LODGroups.
            Groups.Clear();

            // Add LODGroups found in the root game object.
            AddLODGroup(RootGameObject.transform);
            LoadWireframeLOD();

            // Clear the dropdown UI options.
            dropdownUI.ClearOptions();

            if (Groups.Count > 0)
            {
                List<string> options = new List<string>();

                // Add LOD options to the dropdown UI.
                for (int i = 0; i < maxLod; i++)
                {
                    options.Add("LOD " + i.ToString());
                }

                dropdownUI.AddOptions(options);

                // Set the initial LOD level to 0.
                SetLOD(0);
            }
        }

        /// <summary>
        /// This function recursively searches for LODGroups in the children of the given Transform and adds them to the 'Groups' list.
        /// </summary>
        /// <param name="transform">Parent transform used to search for LODGroups.</param>
        void AddLODGroup(Transform transform)
        {
            LODGroup group;

            // Try to get the LODGroup component from the transform.
            if (transform.TryGetComponent<LODGroup>(out group))
            {
                // Add the found LODGroup to the list and update the maximum LOD level.
                Groups.Add(group);
                maxLod = Mathf.Max(maxLod, group.GetLODs().Length);
                group.enabled = false; // Disable the LODGroup initially.
                return;
            }
            else
            {
                // Recursively search in child transforms.
                foreach (Transform t in transform)
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
            // Add the wireframe LODGroup to the list.
            AddLODGroup(MaterialManager.WireFrameGo.transform);
        }

        /// <summary>
        /// This function sets the LOD level for all the LODGroups stored in the 'Groups' list.
        /// </summary>
        /// <param name="index">Index of the LOD level to set.</param>
        public void SetLOD(int index)
        {
            foreach (LODGroup group in Groups)
            {
                // Deactivate all child game objects.
                foreach (Transform t in group.gameObject.transform)
                {
                    t.gameObject.SetActive(false);
                }

                var LODs = group.GetLODs();

                // Activate the renderers of the specified LOD level.
                if (index < LODs.Length)
                {
                    foreach (Renderer r in LODs[index].renderers)
                    {
                        r.gameObject.SetActive(true);
                    }
                }
            }
        }
    }
}