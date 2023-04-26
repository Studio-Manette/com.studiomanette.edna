using StudioManette.ShaderProperties;
using System.Collections.Generic;
using UnityEngine;

namespace StudioManette.Edna
{
    [CreateAssetMenu(fileName = "LightProfilesConfig", menuName = "StudioManette/LightProfilesConfig", order = 1)]
    public class LightProfilesConfig : ScriptableObject
    {
        public List<GameObject> lightProfiles;
    }
}
