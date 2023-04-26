using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace StudioManette.Edna
{
    public class LoadingWindow : MonoBehaviour
    {
        public Slider slider;
        public TMPro.TextMeshProUGUI mainMessageText; //= loadingWindow.transform.Find("LoadingFrame/SubText").GetComponent<TMPro.TextMeshProUGUI>();
        public TMPro.TextMeshProUGUI subMessageText; //= loadingWindow.transform.Find("LoadingFrame/SubText").GetComponent<TMPro.TextMeshProUGUI>();

        public void SetMainText(string mainText)
        {
            mainMessageText.text = mainText;
        }

        public void SetProgress(float completion, string message = null)
        {
            slider.value = completion;

            if (!string.IsNullOrEmpty(message))
            {
                subMessageText.text = message;
            }
        }
    }
}
