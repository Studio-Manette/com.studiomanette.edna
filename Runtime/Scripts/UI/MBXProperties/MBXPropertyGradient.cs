using StudioManette.ShaderProperties;
using UnityEngine;
using UnityEngine.UI;

namespace StudioManette.Edna
{
    public class MBXPropertyGradient : MBXProperty
    {
        public Button colorButton;
        public Gradient propGradient;

        private Image colorPreview;
        private bool needToRefresh = false;

        float dt = 0.0f;
        float updateRate = 2.0f;  // 2 updates per sec.

        public void Awake()
        {
            colorButton.onClick.AddListener(OnClickGradient);
            colorPreview = colorButton.GetComponent<Image>();
        }

        public void Update()
        {
            if (needToRefresh)
            {
                dt += Time.deltaTime;
                if (dt > 1.0 / updateRate)
                {
                    dt -= 1.0f / updateRate;
                    DrawPreviewGradient();
                    needToRefresh = false;
                }
            }
        }

        public void Setup()
        {
            // Explicitly call lower level methods so we do not trigger a new MBX refresh
            propGradient = propManager.GetValueGradientFromMBX(materialID, propID);
            DrawPreviewGradient();
        }

        private void OnClickGradient()
        {
            GradientPicker.Create(propGradient, "Gradient", item => SetGradient(item, false), GradientFinished);
        }

        private void SetGradient(Gradient currentGradient, bool writeFile)
        {
            propGradient = currentGradient;
            if (propManager) propManager.SetValueGradientToMBX(materialID, propID, propGradient, writeFile);
            needToRefresh = true;
        }

        private void GradientFinished(Gradient currentGradient)
        {
            SetGradient(currentGradient, true);
            needToRefresh = false;
            DrawPreviewGradient();
        }

        private void DrawPreviewGradient()
        {
            // Notice that we explicitly ask for non-linear texture here as it is required for onscreen display
            // However we want a non-linear UI, hence the 1st "false" boolean
            // GB rollback to true and Linear
            Texture2D texture = Bob.Common.GradientMapper.MapToTexture(propGradient, new Vector2Int(256, 1), ColorSpace.Linear, true);
            colorPreview.sprite = Sprite.Create(texture, new Rect(0, 0, 256, 1), Vector2.zero);
        }

        public override void Activate(bool _isInteractable)
        {
            colorButton.interactable = _isInteractable;
        }
    }
}
