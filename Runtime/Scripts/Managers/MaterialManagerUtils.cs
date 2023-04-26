using StudioManette.Bob.MBX;
using StudioManette.ShaderProperties;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace StudioManette.Edna
{
    public static class MaterialManagerUtils
    {
        /*
            if (typeLoading == 0) : load new FBX + MBX
            if (typeLoading == 1) : load new FBX only
            if (typeLoading == 2) : load new MBX only
            if (typeLoading == 3) : load existing FBX and MBX
            if (typeLoading == 4) : load new other MBX
        */
        public enum LoadingType
        {
            NewFiles = 0,
            ReloadFBX = 1,
            ReloadMBX = 2,
            ReloadFiles = 3,
            NewMBX = 4
        }

        public static Renderer GetRendererByName(Renderer[] rendList, string objName)
        {
            foreach (Renderer tmpR in rendList)
            {
                if (tmpR.gameObject.name == objName) return tmpR;
            }
            return null;
        }

        public static Material FindMaterialProfileByName(CustomizableShader[] shaderList, string spname)
        {
            foreach (CustomizableShader cs in shaderList)
            {
                if (cs.material.name == spname) return cs.material;
            }
            return null;
        }

        public static CustomizableShader FindShaderProfileByMaterialName(CustomizableShader[] shaderList, string spname)
        {
            foreach (CustomizableShader cs in shaderList)
            {
                if (cs.material.name == spname) return cs;
            }
            return null;
        }

        public static Material ApplyMaterialToGameObject(Material mat, GameObject go)
        {
            Material lastSharedMaterial = null;
            foreach (Renderer rend in go.GetComponentsInChildren<Renderer>())
            {
                Material[] sharedMaterialsCopy = rend.sharedMaterials;
                for (int i = 0; i < sharedMaterialsCopy.Length; i++) sharedMaterialsCopy[i] = mat;

                rend.sharedMaterials = sharedMaterialsCopy;
                lastSharedMaterial = rend.sharedMaterial;
            }

            return lastSharedMaterial;
        }
    }
}
