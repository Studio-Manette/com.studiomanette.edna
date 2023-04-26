using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace StudioManette.Edna
{
    public class CopyText : MonoBehaviour
    {
        public TextMeshProUGUI origin;

        // Start is called before the first frame update
        void OnEnable()
        {
            Process();
        }

        public void Process()
        {
            this.GetComponent<TextMeshProUGUI>().text = origin.text;
        }
    }
}
