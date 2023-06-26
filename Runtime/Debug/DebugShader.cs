using UnityEngine;

using Utils = StudioManette.Edna.RuntimeUtils;

public class DebugShader : MonoBehaviour
{
    public enum DebugRenderer
    { Default, AlbedoOnly, Curvature, Coat, Dirt, AmbientOcclusion, IDColor, DebugPixelRatio };

    public bool IsEdna = false;

    [Header("Debug")]
    public DebugRenderer m_DebugRenderer;
    public TMPro.TMP_Dropdown m_DropdownDebugRenderer;
    public GameObject m_prefabDebugTexelDensity;
    GameObject m_goDebugTexelDensity;
    public Material m_defaultMaterial;
    public Shader m_defaultShader;
    public Shader m_debugShaderPixelRatio;

    private void Start()
    {
        Shader.SetGlobalInt("_debugDefault", 1);
        Shader.SetGlobalInt("_debugCurvature", 0);
        Shader.SetGlobalInt("_debugIDColor", 0);
        Shader.SetGlobalInt("_debugCoat", 0);
        Shader.SetGlobalInt("_debugDirt", 0);
        Shader.SetGlobalInt("_debugAOPP", 0);
        Shader.SetGlobalInt("_debugAlbedoOnly", 0);
        Shader.SetGlobalInt("_activeChecker", 0);
        ActiveDebugShader(0);
        m_goDebugTexelDensity = Instantiate<GameObject>(m_prefabDebugTexelDensity);
        m_goDebugTexelDensity.SetActive(false);
        UpdateDebugEdna();
    }

    public void SetRendererGui(int value)
    {
        m_DebugRenderer = (DebugRenderer)value;
        SetDebug();
    }

    private void UpdateDebugEdna()
    {
        if (IsEdna)
        {
            Shader.SetGlobalInt("_debugIsEdna", 1);
        }
        else
        {
            Shader.SetGlobalInt("_debugIsEdna", 0);
        }
    }

    private void SetDebug()
    {
        //Debug
        if (m_DebugRenderer == DebugRenderer.Default)
        {
            Shader.SetGlobalInt("_debugDefault", 1);
        }
        else
        {
            Shader.SetGlobalInt("_debugDefault", 0);
        }

        if (m_DebugRenderer == DebugRenderer.AlbedoOnly)
        {
            Shader.SetGlobalInt("_debugAlbedoOnly", 1);
        }
        else
        {
            Shader.SetGlobalInt("_debugAlbedoOnly", 0);
        }

        if (m_DebugRenderer == DebugRenderer.Curvature)
        {
            Shader.SetGlobalInt("_debugCurvature", 1);
        }
        else
        {
            Shader.SetGlobalInt("_debugCurvature", 0);
        }
        if (m_DebugRenderer == DebugRenderer.AmbientOcclusion)
        {
            Shader.SetGlobalInt("_debugAOPP", 1);
        }
        else
        {
            Shader.SetGlobalInt("_debugAOPP", 0);
        }
        if (m_DebugRenderer == DebugRenderer.Coat)
        {
            Shader.SetGlobalInt("_debugCoat", 1);
        }
        else
        {
            Shader.SetGlobalInt("_debugCoat", 0);
        }
        if (m_DebugRenderer == DebugRenderer.Dirt)
        {
            Shader.SetGlobalInt("_debugDirt", 1);
        }
        else
        {
            Shader.SetGlobalInt("_debugDirt", 0);
        }
        if (m_DebugRenderer == DebugRenderer.IDColor)
        {
            Shader.SetGlobalInt("_debugIDColor", 1);
        }
        else
        {
            Shader.SetGlobalInt("_debugIDColor", 0);
        }

        if (m_DebugRenderer == DebugRenderer.DebugPixelRatio)
        {
            ActiveDebugPixelRatio(true);
        }
        else
        {
            ActiveDebugPixelRatio(false);
        }

        UpdateDebugEdna();
    }

    private void ActiveDebugPixelRatio(bool IsActive)
    {
        if (IsActive)
        {
            Shader.SetGlobalInt("_activeChecker", 1);
            m_defaultMaterial.shader = m_debugShaderPixelRatio;
            //Debug BoxDebug Pixel Ratio around Asset
            m_goDebugTexelDensity.SetActive(true);
            GameObject go = Utils.GetManager().rootGameObject;
            Bounds bounds = TriLibCore.Extensions.GameObjectExtensions.CalculateBounds(go);

            var szA = bounds.size;
            var szACenter = bounds.center;
            var scale = new Vector3(szA.x, szA.y, szA.z) * 1.5f;
            m_goDebugTexelDensity.transform.localScale = scale;
            m_goDebugTexelDensity.transform.position = szACenter;
        }
        else
        {
            m_goDebugTexelDensity.SetActive(false);
            Shader.SetGlobalInt("_activeChecker", 0);
            if (m_defaultShader != null)
                m_defaultMaterial.shader = m_defaultShader;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ActiveDebugShader(0);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ActiveDebugShader(1);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            ActiveDebugShader(2);
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            ActiveDebugShader(3);
        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            ActiveDebugShader(4);
        }
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            ActiveDebugShader(5);
        }
        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            ActiveDebugShader(6);
        }
        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            ActiveDebugShader(7);
        }
    }

    private void ActiveDebugShader(int DropdownDebugRendererValue)
    {
        if (m_goDebugTexelDensity) m_goDebugTexelDensity.SetActive(false);
        m_DropdownDebugRenderer.value = DropdownDebugRendererValue;
    }

    private void OnDestroy()
    {
        m_defaultMaterial.shader = m_defaultShader;
    }
}
