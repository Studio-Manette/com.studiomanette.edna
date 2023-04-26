using System;
using System.Collections.Generic;
using UnityEngine;

namespace StudioManette.Edna
{
    public static class CommonUtils
    {
        public enum LogPriority
        {
            INFO = 0,
            WARNING = 3,
            ERROR = 6,
            ERROR_ALERT = 9
        }

        // 4 : nombre d'octets par pixel
        // 2.66 : ratio pour inclure les mipmaps
        // 2 : on divise par 2 pour la division RAM / VRAM
        public const float RATIO_PIXEL_VRAM = 4f * 2.66f / 2.0f;


        public static string GetArg(string name)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == name && args.Length > i + 1)
                {
                    return args[i + 1];
                }
            }
            return null;
        }

        public static string GetNameFromPath(string path, bool willGetExtension = true)
        {
            if (willGetExtension)
            {
                return System.IO.Path.GetFileName(path);
            }
            else
            {
                return System.IO.Path.GetFileNameWithoutExtension(path);
            }
        }

        public static string GetPathNameWithoutExtension(string path)
        {
            return GetFolderfromPathName(path) + "/" + GetNameFromPath(path, false);
        }

        public static string GetFolderfromPathName(string pathName)
        {
            string tmpPath = pathName.Replace("\\", "/");
            int posLastSlash = tmpPath.LastIndexOf("/");
            return tmpPath.Substring(0, posLastSlash);
        }

        public static List<Mesh> GetMeshes(GameObject go)
        {
            List<Mesh> meshes = new List<Mesh>();

            foreach (MeshRenderer mr in go.GetComponentsInChildren<MeshRenderer>())
            {
                meshes.Add(mr.gameObject.GetComponent<MeshFilter>().sharedMesh);
            }
            foreach (SkinnedMeshRenderer smr in go.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                meshes.Add(smr.sharedMesh);
            }
            return meshes;
        }

        public static float ParseStringToFloat(string strValue)
        {
            string newStr = strValue.Replace(".", ",");
            return float.Parse(newStr, System.Globalization.NumberStyles.Any);
        }

        public static bool IsNumeric(string s)
        {
            if (s == null)
            {
                return false;
            }
            return float.TryParse(s, out _);
        }

        //     Remove the xx in myVarName(xx) if existing
        public static string RemoveSuffixNumber(string myStr)
        {
            if (myStr.EndsWith(")"))
            {
                int indexOfPar = myStr.LastIndexOf("(");
                if (indexOfPar != -1)
                {
                    string betweenParenthesis = myStr.Substring(indexOfPar + 1, myStr.LastIndexOf(")") - (indexOfPar + 1));
                    if (IsNumeric(betweenParenthesis))
                    {
                        string myStr_Val = myStr.Substring(0, indexOfPar);
                        return myStr_Val;
                    }
                }
            }
            return myStr;
        }

        public static List<string> ListChildrenNames(Transform tr, bool isRecursive)
        {
            List<string> theList = new List<string>();
            foreach (Transform trChild in tr)
            {
                theList.Add(trChild.name);
                if (isRecursive) theList.AddRange(ListChildrenNames(trChild, true));
            }
            return theList;
        }
    }
}
