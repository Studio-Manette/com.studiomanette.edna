using System;

using UnityEngine;

using StudioManette.ShaderProperties;

namespace StudioManette.Edna
{
    [Serializable]
    [CreateAssetMenu(fileName = "ShaderProfilesConfig", menuName = "StudioManette/ShaderProfilesConfig", order = 1)]
    public class ShaderProfilesConfig : ScriptableObject
    {
        public CustomizableShader[] shaderProfiles;
    }
}
