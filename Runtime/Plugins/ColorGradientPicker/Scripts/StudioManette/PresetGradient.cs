using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StudioManette.Gradients
{
    public class PresetGradient : MonoBehaviour, IPointerClickHandler
    {
        public int position;
        public Gradient gradient;

        [HideInInspector]
        public UnityEvent<Gradient> onClick;

        private PresetGradientManager manager;
        private RawImage rawImage;

        void OnEnable()
        {
            rawImage = GetComponentInChildren<RawImage>();
        }

        public void Init(PresetGradientManager _pgm, Gradient _gradient, int _position)
        {
            manager = _pgm;
            position = _position;

            if (gradient == null) gradient = new Gradient();
            //créer une copie du gradient donné en paramètre
            gradient.SetKeys(_gradient.colorKeys, _gradient.alphaKeys);
            GenerateTexture();
        }

        public void SetTexture(Texture2D _tex)
        {
            rawImage.texture = _tex;
        }

        public void OnClickButton()
        {
            onClick.Invoke(gradient);
        }

        public void OnRightClick()
        {
            if (manager) manager.ShowContextMenu(this);
        }

        private void GenerateTexture()
        {
            // GM: display the color in gamma space // GB : replace Gamma by Linear then set last boolean to true
            Texture2D tex = StudioManette.Bob.Common.GradientMapper.MapToTexture(gradient, new Vector2Int(256, 1), ColorSpace.Linear, true);
            SetTexture(tex);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) OnClickButton();
            else if (eventData.button == PointerEventData.InputButton.Right) OnRightClick();
            /*
            else if (eventData.button == PointerEventData.InputButton.Middle)
                Debug.Log("Middle click");
            */
        }
    }
}
