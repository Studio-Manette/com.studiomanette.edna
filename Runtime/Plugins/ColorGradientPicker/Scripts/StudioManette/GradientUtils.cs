using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StudioManette.Gradients
{
    [System.Serializable]
    public class GradientList
    {
        public List<JsonGradient> gradients = new List<JsonGradient>();

        public string ToJSonString()
        {
            return JsonUtility.ToJson(this);
        }
    }

    [System.Serializable]
    public class JsonGradient
    {
        public int index;
        public ColorKey[] colorKeys;
        public AlphaKey[] alphaKeys;

        public JsonGradient(Gradient grad, int position = 0)
        {
            this.index = position;

            List<ColorKey> jsColorKeys = new List<ColorKey>();
            foreach (GradientColorKey gck in grad.colorKeys)
            {
                ColorKey cokey = new ColorKey();
                cokey.color = gck.color;
                cokey.time = gck.time;
                jsColorKeys.Add(cokey);
            }
            this.colorKeys = jsColorKeys.ToArray();

            List<AlphaKey> jsAlphaKeys = new List<AlphaKey>();
            foreach (GradientAlphaKey gak in grad.alphaKeys)
            {
                AlphaKey apkey = new AlphaKey();
                apkey.alpha = gak.alpha;
                apkey.time = gak.time;
                jsAlphaKeys.Add(apkey);
            }
            this.alphaKeys = jsAlphaKeys.ToArray();
        }

        public Gradient ToUnityGradient()
        {
            Gradient grad = new Gradient();

            List<GradientColorKey> gColorKeys = new List<GradientColorKey>();
            foreach (ColorKey cokey in colorKeys)
            {
                GradientColorKey gck = new GradientColorKey();
                gck.color = cokey.color;
                gck.time = cokey.time;
                gColorKeys.Add(gck);
            }
            grad.colorKeys = gColorKeys.ToArray();

            List<GradientAlphaKey> gAlphaKeys = new List<GradientAlphaKey>();
            foreach (AlphaKey apkey in alphaKeys)
            {
                GradientAlphaKey gak = new GradientAlphaKey();
                gak.alpha = apkey.alpha;
                gak.time = apkey.time;
                gAlphaKeys.Add(gak);
            }
            grad.alphaKeys = gAlphaKeys.ToArray();

            return grad;
        }
    }

    [System.Serializable]
    public class ColorKey
    {
        public Color color;
        public float time;
    }

    [System.Serializable]
    public class AlphaKey
    {
        public float alpha;
        public float time;
    }

    public class GradientUtils : MonoBehaviour
    {
        public static string GradientToJsonString(Gradient grad)
        {
            return JsonUtility.ToJson(new JsonGradient(grad));
        }

        public static Gradient JsonToGradientString(string str)
        {
            return JsonUtility.FromJson<JsonGradient>(str).ToUnityGradient();
        }
    }
}