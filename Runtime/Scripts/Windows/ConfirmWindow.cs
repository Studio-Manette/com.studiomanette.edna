using System;

using UnityEngine;

using TMPro;
using UnityEngine.UI;

public class ConfirmWindow : MonoBehaviour
{
    public TextMeshProUGUI textField;
    public AudioSource audioSource;

    public Action onActionConfirm;
    public Action onActionCancel;

    public Button buttonYes;
    public Button buttonNo;
    public Button buttonExit;

    public void Awake()
    {
        buttonYes.onClick.AddListener(OnClickYes);
        buttonNo.onClick.AddListener(OnClickCancel);
        buttonExit.onClick.AddListener(OnClickExit);
    }

    public void Confirm(string _message, Action _actionConfirm, Action _actionCancel, string promptYes = "Yes", string promptNo = "No")
    {
        onActionConfirm = _actionConfirm;
        onActionCancel = _actionCancel;

        textField.text = _message;

        buttonYes.GetComponentInChildren<TextMeshProUGUI>().text = promptYes;
        buttonNo.GetComponentInChildren<TextMeshProUGUI>().text = promptNo;

        //On delaie l'active window au cas où un autre confirm est appelé dans une fonction actionConfirm ou actionCancel
        Invoke(nameof(ActiveWindow), 0.1f);
    }

    private void ActiveWindow()
    {
        this.gameObject.SetActive(true);
        audioSource.Play();
    }

    public void OnClickYes()
    {
        onActionConfirm.Invoke();
        OnClickExit();
    }

    public void OnClickCancel()
    {
        if (onActionCancel != null) onActionCancel.Invoke();
        OnClickExit();
    }

    public void OnClickExit()
    {
        this.gameObject.SetActive(false);
    }
}
