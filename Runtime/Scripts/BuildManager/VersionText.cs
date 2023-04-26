using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace StudioManette.Edna
{
    public class VersionText : MonoBehaviour
    {
        public TextMeshProUGUI textVersion;
        public string textBerforeVersion = "Edna";

        public TextMeshProUGUI textBuildVersion;
        public string textBerforeBuildVersion = "build ";

        void Awake()
        {
            string[] version = Application.version.Split("-");
            textVersion.text = textBerforeVersion + "" + version[0];
            textBuildVersion.text = textBerforeBuildVersion + "" + version[1];
        }
    }
}
