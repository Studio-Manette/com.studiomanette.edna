using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StudioManette.Edna
{
    public class ButtonGroup : MonoBehaviour
    {
        public Image backgroundImage;
        public Color activedColorBG = Color.white;
        public Color desactivedColorBG = Color.grey;

        private void OnEnable()
        {
            //_transform = this.transform;
        }

        public void Activate(bool isActive)
        {
            backgroundImage.color = isActive ? activedColorBG : desactivedColorBG;

            foreach (Button button in GetComponentsInChildren<Button>())
            {
                button.interactable = isActive;
            }
            foreach (TMP_Dropdown dropdown in GetComponentsInChildren<TMP_Dropdown>())
            {
                dropdown.interactable = isActive;
            }
            foreach (Toggle toggle in GetComponentsInChildren<Toggle>())
            {
                toggle.interactable = isActive;
            }
        }
    }
}
