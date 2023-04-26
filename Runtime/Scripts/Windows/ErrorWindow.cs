using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace StudioManette.Edna
{
    public class ErrorWindow : MonoBehaviour
    {
        public TextMeshProUGUI textField;
        public GameObject copyMessage;
        public AudioSource errorAudioSource;

        public void Alert(string _message)
        {
            this.gameObject.SetActive(true);
            errorAudioSource.Play();
            textField.text = _message;
            copyMessage.SetActive(false);
        }

        public void CopyToClipBoard()
        {
            GUIUtility.systemCopyBuffer = textField.text;
            copyMessage.SetActive(true);
        }
    }
}
