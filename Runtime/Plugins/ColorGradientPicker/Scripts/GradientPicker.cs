using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using StudioManette.Gradients;

public class GradientPicker : MonoBehaviour
{
    /// <summary>
    /// Event that gets called by the GradientPicker.
    /// </summary>
    /// <param name="g">received Gradient</param>
    public delegate void GradientEvent(Gradient g);

    /// <summary>
    /// Event for copy/paste confirmation
    /// </summary>
    /// <param name="message">Display message</param>
    /// <param name="functionCalledOnTrue">The function to be called in the case of a confirmation</param>
    public delegate void CopyPasteConfirmation(string message, Action funcOnConfirm, Action funcOnCancel = null);

    /// <summary>
    /// True when the GradientPicker is closed
    /// </summary>
    public static bool done = true;
    public PresetGradientManager presetGradientManager;

    // Fired in the case of a failure
    public static UnityEvent<string> OnFailure = new UnityEvent<string>();
    public static CopyPasteConfirmation OnCopyPasteConfirmation;

    private static bool interact;
    private static GradientPicker instance;

    //onGradientChanged Event
    private static GradientEvent onGC;
    //onGradientSelected Event
    private static GradientEvent onGS;

    //Gradient before editing
    private static Gradient originalGradient;
    //current Gradient
    private static Gradient modifiedGradient;

    //key template
    private GameObject key;

    //all these objects only work on Prefab
    private InputField positionComponent;
    private Image colorComponent;
    private Transform alphaComponent;

    private List<Slider> colorKeyObjects;
    private List<GradientColorKey> colorKeys;
    private int selectedColorKey;
    private List<Slider> alphaKeyObjects;
    private List<GradientAlphaKey> alphaKeys;
    private int selectedAlphaKey;

    private void Awake()
    {
        instance = this;
        key = transform.GetChild(2).gameObject;
        positionComponent = transform.parent.GetChild(3).GetComponent<InputField>();
        colorComponent = transform.parent.GetChild(4).GetComponent<Image>();
        alphaComponent = transform.parent.GetChild(5);
        transform.parent.gameObject.SetActive(false);
    }
    /// <summary>
    /// Creates a new GradiantPicker
    /// </summary>
    /// <param name="original">Color before editing</param>
    /// <param name="message">Display message</param>
    /// <param name="onGradientChanged">Event that gets called when the gradient gets modified</param>
    /// <param name="onGradientSelected">Event that gets called when one of the buttons done or cancel gets pressed</param>
    /// <returns>False if the instance is already running</returns>
    public static bool Create(Gradient original, string message, GradientEvent onGradientChanged, GradientEvent onGradientSelected)
    {
        if (instance is null)
        {
            Debug.LogError("No Gradientpicker prefab active on 'Start' in scene");
            return false;
        }
        if (done)
        {
            done = false;
            originalGradient = new Gradient();
            originalGradient.SetKeys(original.colorKeys, original.alphaKeys);
            modifiedGradient = new Gradient();
            modifiedGradient.SetKeys(original.colorKeys, original.alphaKeys);
            onGC = onGradientChanged;
            onGS = onGradientSelected;
            instance.transform.parent.gameObject.SetActive(true);
            instance.transform.parent.GetChild(0).GetChild(0).GetComponent<Text>().text = message;
            instance.Setup();
            return true;
        }
        else
        {
            Done();
            return false;
        }
    }

    public static void UpdateGradient(Gradient original)
    {
        modifiedGradient.SetKeys(original.colorKeys, original.alphaKeys);
        instance.Setup(false);
    }


    public void OnClickPaste()
    {
        OnCopyPasteConfirmation?.Invoke("Do you really want to replace the current gradient by the stored one in your clipboard ?", Paste);
    }

    public void OnClickCopy()
    {
        Copy();
    }

    private void Copy()
    {
        string textJson = GradientUtils.GradientToJsonString(modifiedGradient);
        GUIUtility.systemCopyBuffer = textJson;
    }

    private void Paste()
    {
        Gradient grad = null;
        try
        {
            string textJson = GUIUtility.systemCopyBuffer;
            grad = GradientUtils.JsonToGradientString(textJson);
        }
        catch (Exception e)
        {
            OnFailure.Invoke(e.Message);
        }

        if (grad != null) UpdateGradient(grad);
    }

    //Setup new GradientPicker
    private void Setup(bool isOriginalSetup = true)
    {
        interact = false;

        if (!isOriginalSetup)
        {
            while (colorKeyObjects.Count > 0)
            {
                Slider tmpSlider = colorKeyObjects[0];
                colorKeyObjects.RemoveAt(0);
                GameObject.DestroyImmediate(tmpSlider.gameObject);
            }
            while (alphaKeyObjects.Count > 0)
            {
                Slider tmpSlider = alphaKeyObjects[0];
                alphaKeyObjects.RemoveAt(0);
                GameObject.DestroyImmediate(tmpSlider.gameObject);
            }
        }

        colorKeyObjects = new List<Slider>();
        colorKeys = new List<GradientColorKey>();
        alphaKeyObjects = new List<Slider>();
        alphaKeys = new List<GradientAlphaKey>();

        GradientColorKey[] tmpColorKeys = isOriginalSetup ? originalGradient.colorKeys : modifiedGradient.colorKeys;
        GradientAlphaKey[] tmpAlphaKeys = isOriginalSetup ? originalGradient.alphaKeys : modifiedGradient.alphaKeys;

        foreach (GradientColorKey k in tmpColorKeys)
        {
            CreateColorKey(k);
        }
        foreach (GradientAlphaKey k in tmpAlphaKeys)
        {
            CreateAlphaKey(k);
        }
        CalculateTexture();
        interact = true;
    }
    //creates a ColorKey UI object
    private void CreateColorKey(GradientColorKey k)
    {
        if (colorKeys.Count < 8)
        {
            Slider s = Instantiate(key, transform.position, new Quaternion(), transform).GetComponent<Slider>();
            ((RectTransform)s.transform).anchoredPosition = new Vector2(0, -29f);
            s.name = "ColorKey";
            s.gameObject.SetActive(true);
            s.value = k.time;
            // GM: display the color in gamma space  // GB : restore by using the same color
            s.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Image>().color = k.color;
            colorKeyObjects.Add(s);
            colorKeys.Add(k);
            ChangeSelectedColorKey(colorKeys.Count - 1);
        }
    }
    //checks if new ColorKey should be created
    public void CreateNewColorKey(float time)
    {
        if (Input.GetMouseButtonDown(0))
        {
            interact = false;
            CreateColorKey(new GradientColorKey(modifiedGradient.Evaluate(time), time));
            interact = true;
        }
    }
    //creates a AlphaKey UI object
    private void CreateAlphaKey(GradientAlphaKey k)
    {
        if (alphaKeys.Count < 8)
        {
            Slider s = Instantiate(key, transform.position, new Quaternion(), transform).GetComponent<Slider>();
            ((RectTransform)s.transform).anchoredPosition = new Vector2(0, 25f);
            s.transform.GetChild(0).GetChild(0).rotation = new Quaternion();
            s.name = "AlphaKey";
            s.gameObject.SetActive(true);
            s.value = k.time;
            s.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Image>().color = new Color(k.alpha, k.alpha, k.alpha, 1f);
            alphaKeyObjects.Add(s);
            alphaKeys.Add(k);
            ChangeSelectedAlphaKey(alphaKeys.Count - 1);
        }
    }
    //checks if new AlphaKey should be created
    public void CreateNewAlphaKey(float time)
    {
        if (Input.GetMouseButtonDown(0))
        {
            interact = false;
            CreateAlphaKey(new GradientAlphaKey(modifiedGradient.Evaluate(time).a, time));
            interact = true;
        }
    }

    private void CalculateTexture()
    {
        // GM: display the color in gamma space // GB : replace the last paramater by true
        // Debug.Log("Calculate Texture");
        Texture2D tex = StudioManette.Bob.Common.GradientMapper.MapToTexture(modifiedGradient, new Vector2Int(256, 1), ColorSpace.Linear, true);
        GetComponent<RawImage>().texture = tex;

        if (presetGradientManager) presetGradientManager.UpdateCurrentGradient(modifiedGradient, tex);

        onGC?.Invoke(modifiedGradient);
    }
    //accessed by alpha Slider
    public void SetAlpha(float value)
    {
        if (interact)
        {
            alphaKeys[selectedAlphaKey] = new GradientAlphaKey(value, alphaKeys[selectedAlphaKey].time);
            modifiedGradient.SetKeys(colorKeys.ToArray(), alphaKeys.ToArray());
            CalculateTexture();
            alphaComponent.GetChild(4).GetComponent<InputField>().text = Mathf.RoundToInt(value * 255f).ToString();
            alphaKeyObjects[selectedAlphaKey].transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Image>().color = new Color(value, value, value, 1f);
        }
    }
    //accessed by alpha InputField
    public void SetAlpha(string value)
    {
        alphaComponent.GetComponent<Slider>().value = Mathf.Clamp(int.Parse(value), 0, 255) / 255f;
        CalculateTexture();
    }

    private void ChangeSelectedColorKey(int value)
    {
        if (colorKeyObjects.Count() > selectedColorKey)
        {
            colorKeyObjects[selectedColorKey].transform.GetChild(0).GetChild(0).GetComponent<Image>().color = Color.gray;
        }
        if (alphaKeyObjects.Count() > 0)
        {
            alphaKeyObjects[selectedAlphaKey].transform.GetChild(0).GetChild(0).GetComponent<Image>().color = Color.gray;
        }
        colorKeyObjects[value].transform.GetChild(0).GetChild(0).GetComponent<Image>().color = Color.green;
        if (selectedColorKey != value && !ColorPicker.done)
        {
            ColorPicker.Done();
        }
        selectedColorKey = value;
        colorKeyObjects[value].Select();
    }

    private void ChangeSelectedAlphaKey(int value)
    {
        if (alphaKeyObjects.Count > selectedAlphaKey)
        {
            alphaKeyObjects[selectedAlphaKey].transform.GetChild(0).GetChild(0).GetComponent<Image>().color = Color.gray;
        }
        if (colorKeyObjects.Count > 0)
        {
            colorKeyObjects[selectedColorKey].transform.GetChild(0).GetChild(0).GetComponent<Image>().color = Color.gray;
        }
        alphaKeyObjects[value].transform.GetChild(0).GetChild(0).GetComponent<Image>().color = Color.green;
        selectedAlphaKey = value;
        alphaKeyObjects[value].Select();
    }
    //checks if Key can be deleted
    public void CheckDeleteKey(Slider s)
    {
        if (Input.GetMouseButtonDown(1))
        {
            if (s.name == "ColorKey" && colorKeys.Count > 2)
            {
                if (!ColorPicker.done)
                {
                    ColorPicker.Done();
                    return;
                }
                int index = colorKeyObjects.IndexOf(s);
                Destroy(colorKeyObjects[index].gameObject);
                colorKeyObjects.RemoveAt(index);
                colorKeys.RemoveAt(index);
                if (index <= selectedColorKey)
                {
                    ChangeSelectedColorKey(selectedColorKey - 1);
                }
                modifiedGradient.SetKeys(colorKeys.ToArray(), alphaKeys.ToArray());
                CalculateTexture();
            }
            if(s.name == "AlphaKey" && alphaKeys.Count > 2)
            {
                int index = alphaKeyObjects.IndexOf(s);
                Destroy(alphaKeyObjects[index].gameObject);
                alphaKeyObjects.RemoveAt(index);
                alphaKeys.RemoveAt(index);
                if (index <= selectedAlphaKey)
                {
                    ChangeSelectedAlphaKey(selectedAlphaKey - 1);
                }
                modifiedGradient.SetKeys(colorKeys.ToArray(), alphaKeys.ToArray());
                CalculateTexture();
            }
        }
    }
    //changes Selected Key
    public void Select()
    {
        Slider s = EventSystem.current.currentSelectedGameObject.GetComponent<Slider>();
        s.transform.SetAsLastSibling();
        if (s.name == "ColorKey")
        {
            ChangeSelectedColorKey(colorKeyObjects.IndexOf(s));
            alphaComponent.gameObject.SetActive(false);
            colorComponent.gameObject.SetActive(true);
            positionComponent.text = Mathf.RoundToInt(colorKeys[selectedColorKey].time * 100f).ToString();
            // GM: display the color in gamma space // GB : restore by using the same color
            colorComponent.GetComponent<Image>().color = colorKeys[selectedColorKey].color;
        }
        else
        {
            ChangeSelectedAlphaKey(alphaKeyObjects.IndexOf(s));
            colorComponent.gameObject.SetActive(false);
            alphaComponent.gameObject.SetActive(true);
            positionComponent.text = Mathf.RoundToInt(alphaKeys[selectedAlphaKey].time * 100f).ToString();
            alphaComponent.GetComponent<Slider>().value = alphaKeys[selectedAlphaKey].alpha;
            alphaComponent.GetChild(4).GetComponent<InputField>().text = Mathf.RoundToInt(alphaKeys[selectedAlphaKey].alpha * 255f).ToString();
        }
    }
    //accessed by position Slider
    public void SetTime(float time)
    {
        if (interact)
        {
            Slider s = EventSystem.current.currentSelectedGameObject.GetComponent<Slider>();
            if (s.name == "ColorKey")
            {
                int index = colorKeyObjects.IndexOf(s);
                colorKeys[index] = new GradientColorKey(colorKeys[index].color, time);
            }
            else
            {
                int index = alphaKeyObjects.IndexOf(s);
                alphaKeys[index] = new GradientAlphaKey(alphaKeys[index].alpha, time);
            }
            modifiedGradient.SetKeys(colorKeys.ToArray(), alphaKeys.ToArray());
            CalculateTexture();
            positionComponent.text = Mathf.RoundToInt(time * 100f).ToString();
        }
    }
    //accessed by position InputField
    public void SetTime(string time)
    {
        interact = false;
        float t = Mathf.Clamp(int.Parse(time), 0, 100) * 0.01f;
        if (colorComponent.gameObject.activeSelf)
        {
            colorKeyObjects[selectedColorKey].value = t;
            colorKeys[selectedColorKey] = new GradientColorKey(colorKeys[selectedColorKey].color, t);
        }
        else
        {
            alphaKeyObjects[selectedAlphaKey].value = t;
            alphaKeys[selectedAlphaKey] = new GradientAlphaKey(alphaKeys[selectedAlphaKey].alpha, t);
        }
        modifiedGradient.SetKeys(colorKeys.ToArray(), alphaKeys.ToArray());
        CalculateTexture();
        interact = true;
    }
    //choose color button call
    public void ChooseColor()
    {
        // GM: edit the color in gamma space, retrieve the linear version // GB : restore by using the same color
        ColorPicker.Create(colorKeys[selectedColorKey].color, "Gradient Color Key", (c) => UpdateColor(selectedColorKey, c), null);
    }

    private void UpdateColor(int index, Color c)
    {
        interact = false;
        colorKeys[index] = new GradientColorKey(c, colorKeys[index].time);
        // GM: display the color in gamma space // GB : restore by using the same color
        colorKeyObjects[index].transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Image>().color = c;
        colorComponent.color = c;
        modifiedGradient.SetKeys(colorKeys.ToArray(), alphaKeys.ToArray());
        CalculateTexture();
        interact = true;
    }
    //cancel button call
    public void CCancel()
    {
        Cancel();
    }
    /// <summary>
    /// Manually cancel the GradientPicker and recovers the default value
    /// </summary>
    public static void Cancel()
    {
        modifiedGradient = originalGradient;
        Done();
    }
    //done button call
    public void CDone()
    {
        Done();
    }
    /// <summary>
    /// Manually close the GradientPicker and apply the selected color
    /// </summary>
    public static void Done()
    {
        if(!ColorPicker.done)
            ColorPicker.Done();
        foreach (Slider s in instance.colorKeyObjects)
        {
            Destroy(s.gameObject);
        }
        foreach (Slider s in instance.alphaKeyObjects)
        {
            Destroy(s.gameObject);
        }
        instance.colorKeyObjects = null;
        instance.colorKeys = null;
        instance.alphaKeyObjects = null;
        instance.alphaKeys = null;
        done = true;
        onGC?.Invoke(modifiedGradient);
        onGS?.Invoke(modifiedGradient);
        instance.transform.parent.gameObject.SetActive(false);
    }
}
