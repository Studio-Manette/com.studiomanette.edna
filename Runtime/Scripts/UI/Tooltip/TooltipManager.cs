using TMPro;
using UnityEngine;

namespace StudioManette.Edna
{
    public class TooltipManager : MonoBehaviour
    {
        public Transform TooltipUI;

        public float windowOffsetX = 10.0f;
        public float windowOffsetY = 10.0f;

        public float timeBeforeShow = 1.0f;
        public float timeBeforeHide = 0.2f;

        private static TooltipManager instance;

        private delegate void DelegateVoid();
        private DelegateVoid methodToCall;

        private TextMeshProUGUI textUI;
        private bool isPointerOnUI;

        //timer operations
        private float dt;
        private float targetTime;
        private bool isCounting;


        //public static functions
        public static void Activate(string txt)
        {
            instance.textUI.text = txt;
            if (instance.TooltipUI.gameObject.activeInHierarchy)
            {
                instance.LaunchFunctionDirectly(instance.ShowTooltip);
            }
            else
            {
                instance.LaunchFunctionAfterTimer(instance.timeBeforeShow, instance.ShowTooltip);
            }
        }

        public static void CancelToolTip()
        {
            instance.LaunchFunctionAfterTimer(instance.timeBeforeHide, instance.ReallyCancelToolTip);
        }

        public static void OnPointerIsOnUI(bool isOnUI)
        {
            instance.isPointerOnUI = isOnUI;
        }

        // Start is called before the first frame update
        void OnEnable()
        {
            instance = this;
            textUI = instance.TooltipUI.GetComponentInChildren<TextMeshProUGUI>();
            instance.TooltipUI.gameObject.SetActive(false);
        }

        void Update()
        {
            if (isCounting)
            {
                dt += Time.deltaTime;
                if (dt >= targetTime)
                {
                    isCounting = false;
                    methodToCall.Invoke();
                }
            }
        }

        private void LaunchFunctionDirectly(DelegateVoid func)
        {
            instance.dt = 0;
            instance.isCounting = false;
            instance.methodToCall = null;
            func.Invoke();
        }

        private void LaunchFunctionAfterTimer(float time, DelegateVoid func)
        {
            // on lance _func_ dans _time_ seconde
            instance.methodToCall = func;
            instance.targetTime = time;
            instance.dt = 0;
            instance.isCounting = true;
        }

        private void ShowTooltip()
        {
            TooltipUI.GetComponent<Transform>().position = GetWindowOffsets() + Input.mousePosition;
            TooltipUI.gameObject.SetActive(true);
        }

        private Vector3 GetWindowOffsets()
        {
            bool isPointerToTheLeftOfTheScreen = (Input.mousePosition.x < Screen.width / 2.0f);
            bool isPointerToTheTopOfTheScreen = (Input.mousePosition.y > Screen.height / 2.0f);

            return new Vector3(isPointerToTheLeftOfTheScreen ? windowOffsetX : -windowOffsetX,
                isPointerToTheTopOfTheScreen ? windowOffsetY : -windowOffsetY,
                0);
        }

        private void ReallyCancelToolTip()
        {
            if (!instance.isPointerOnUI) HideTooltip();
        }

        private void HideTooltip()
        {
            isPointerOnUI = false;
            TooltipUI.gameObject.SetActive(false);
        }
    }
}
