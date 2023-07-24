using System.Collections.Generic;

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

using StudioManette.Bill;
using StudioManette.ShaderProperties;

using Utils = StudioManette.Edna.CommonUtils;

namespace StudioManette.Edna
{
    public class CustomBuildManager : EditorWindow
    {
        [MenuItem("Studio Manette/Edna/Build Manager")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(CustomBuildManager), false, "EDNA - Custom Build Manager");
            OnEnable();
        }

        public static List<CustomBuildStep> steps = new List<CustomBuildStep>();

        private static readonly string lightsFolderLocal = "Assets/Renderer/Common/Lighting/Presets";
        private static readonly string shaderFolderLocal = "Assets/Renderer/Common/Shaders/Profiles";
        private static readonly string shaderFolderServer = "Z:/ADDONS/BlenderTools/qtutils/ShaderProfiles";

        private static readonly string shaderProfilesConfigPath = "Assets/Settings/ShaderProfilesConfig.asset";
        private static readonly string lightProfilesConfigPath = "Assets/Settings/LightProfilesConfig.asset";

        private static readonly string EdnaQualityName = "Edna";

        private static readonly string kMainScenePath = "Assets/Scenes/Edna.unity";

        private bool isBuildValid;

        public static void OnEnable()
        {
            steps = new List<CustomBuildStep>();
            steps.Add(new CustomBuildStep(10, "1. Open Main Scene", OpenMainScene));
            steps.Add(new CustomBuildStep(21, "2.1 Apply Shaders in Material Manager", ApplyShadersInMaterialManager));
            steps.Add(new CustomBuildStep(22, "2.2 Apply Light Prefabs in LightManager", ApplyLights));
            steps.Add(new CustomBuildStep(30, "3. Process Active State Manager", ProcessActiveStateManager));
            steps.Add(new CustomBuildStep(35, "3.5 Setup Quality and 3D API", SetupQuality));
            steps.Add(new CustomBuildStep(40, "4. Launch Build (" + BuildManager.PreviewIncrementedBuild() + ")", LaunchBuild));
            steps.Add(new CustomBuildStep(50, "5. Compile with ISS", CompileWizard));
            // steps.Add(new CustomBuildStep(60, "6. Push shaders profiles on Tools server and SVN commit", ExportShaderProfiles));
            steps.Add(new CustomBuildStep(65, "6.5 Restore Quality and 3D API", RestoreQuality));
        }

        private bool CheckValidity()
        {
            bool isValid = true;
            isValid = isValid && BuildManager.PreviewIncrementedBuild() != "BAD-VERSIONNING";
            return isValid;
        }

        /* these 3 functions are called by TeamCity */
        [MenuItem("Studio Manette/Edna/Build Manager Debug/Build From CI")]
        public static void BuildFromCI()
        {
            StartBuildFromCI(BuildManager.IncrementType.build);
        }

        public static void IncrementMinorFromCI()
        {
            PlayerSettings.bundleVersion = BuildManager.PreviewIncrementedMinor();
        }

        public static void IncrementMajorFromCI()
        {
            PlayerSettings.bundleVersion = BuildManager.PreviewIncrementedMajor();
        }

        private static void StartBuildFromCI(BuildManager.IncrementType incrementType)
        {
            steps = new List<CustomBuildStep>();
            steps.Add(new CustomBuildStep(10, "1. Open Main Scene", OpenMainScene));
            steps.Add(new CustomBuildStep(21, "2.1 Apply Shaders in Material Manager", ApplyShadersInMaterialManager));
            steps.Add(new CustomBuildStep(22, "2.2 Apply Light Prefabs in LightManager", ApplyLights));
            steps.Add(new CustomBuildStep(30, "3. Process Active State Manager", ProcessActiveStateManager));
            steps.Add(new CustomBuildStep(35, "3.5 Setup Quality and 3D API", SetupQuality));

            switch (incrementType)
            {
                case BuildManager.IncrementType.build:
                    steps.Add(new CustomBuildStep(40, "4. Launch Build", LaunchBuild));
                    break;
                case BuildManager.IncrementType.minor:
                    steps.Add(new CustomBuildStep(40, "4. Launch Build", LaunchBuildIncrementMinor));
                    break;
                case BuildManager.IncrementType.major:
                    steps.Add(new CustomBuildStep(40, "4. Launch Build", LaunchBuildIncrementMajor));
                    break;
            }

            steps.Add(new CustomBuildStep(50, "5. Compile with ISS", CompileWizard));
            steps.Add(new CustomBuildStep(65, "6.5 Restore Quality and 3D API", RestoreQuality));
            steps.Add(new CustomBuildStep(99, "9. Log Success", LogSuccess));

            DoAllSteps();
        }

        private void OnGUI()
        {
            foreach (CustomBuildStep step in steps)
            {
                DrawStep(step);
            }

            isBuildValid = CheckValidity();
            EditorGUI.BeginDisabledGroup(!isBuildValid);
            if (GUILayout.Button("PROCESS ALL SELECTED STEPS"))
            {
                DoAllSteps();
            }
            EditorGUI.EndDisabledGroup();
        }

        private void DrawStep(CustomBuildStep step)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                step.willDoThisStep = GUILayout.Toggle(step.willDoThisStep, step.description);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Do it now"))
                {
                    step.callback.Invoke();
                }
            }
        }

        private static void DoAllSteps()
        {
            Debug.Log("Do All Steps : ");
            foreach (CustomBuildStep step in steps)
            {
                if (step.willDoThisStep)
                {
                    Debug.Log(step.description);
                    step.callback.Invoke();
                }
            }
        }

        private static void OpenMainScene()
        {
            EditorSceneManager.OpenScene(kMainScenePath, OpenSceneMode.Single);
        }

        private static void ApplyShadersInMaterialManager()
        {
            string[] guids = GetCustomizableShadersGuids();
            List<CustomizableShader> shaProfiles = new();
            foreach (string guid in guids)
            {
                shaProfiles.Add((CustomizableShader)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(CustomizableShader)));
            }

            ShaderProfilesConfig shaderConfig = (ShaderProfilesConfig)AssetDatabase.LoadAssetAtPath(shaderProfilesConfigPath, typeof(ShaderProfilesConfig));
            shaderConfig.shaderProfiles = shaProfiles.ToArray();
            EditorUtility.SetDirty(shaderConfig);
            AssetDatabase.SaveAssets();
        }

        private static void ApplyLights()
        {
            string[] guidsLight = AssetDatabase.FindAssets("t:prefab", new[] { lightsFolderLocal });
            List<GameObject> lightProfiles = new();
            foreach (string guid in guidsLight)
            {
                lightProfiles.Add((GameObject)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(GameObject)));
            }

            LightProfilesConfig lightConfig = (LightProfilesConfig)AssetDatabase.LoadAssetAtPath(lightProfilesConfigPath, typeof(LightProfilesConfig));
            lightConfig.lightProfiles = lightProfiles;
            EditorUtility.SetDirty(lightConfig);
            AssetDatabase.SaveAssets();
        }

        private static void ExportShaderProfiles()
        {
            string[] guids = GetCustomizableShadersGuids();
            foreach (string guid in guids)
            {
                string filePath = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Utils.GetNameFromPath(filePath);
                string serverFilePath = shaderFolderServer + "/" + fileName;
                FileUtil.DeleteFileOrDirectory(serverFilePath);
                FileUtil.CopyFileOrDirectory(filePath, serverFilePath);
            }

            //push to svn
            string logMessage = "Shaders from Edna v" + Application.version;
            string arguments = "/command:commit /path:\"" + shaderFolderServer + "\" /logmsg:\"" + logMessage + "\" /closeonend:1'";
            var processInfo = new System.Diagnostics.ProcessStartInfo("TortoiseProc.exe", arguments);

            Debug.Log("TortoiseProc.exe " + arguments);

            var process = System.Diagnostics.Process.Start(processInfo);

            process.WaitForExit();
        }

        private static void ProcessActiveStateManager()
        {
            BuildStateManager.Instance.Process();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        private static void SetupQuality()
        {
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                if (QualitySettings.names[i].ToLower().Trim() == EdnaQualityName.ToLower().Trim())
                {
                    QualitySettings.SetQualityLevel(i, true);
                    break;
                }
            }
        }

        private static void RestoreQuality()
        {
            QualitySettings.SetQualityLevel(0, true);
        }

        private static void LaunchBuild()
        {
            BuildManager.Build(true, BuildManager.IncrementType.build, true);
        }

        private static void LaunchBuildIncrementMinor()
        {
            BuildManager.Build(true, BuildManager.IncrementType.minor, true);
        }

        private static void LaunchBuildIncrementMajor()
        {
            BuildManager.Build(true, BuildManager.IncrementType.major, true);
        }

        private static void CompileWizard()
        {
            BuildManager.CompileInnoSetup();
        }

        private static string[] GetCustomizableShadersGuids()
        {
            return AssetDatabase.FindAssets("t:customizableshader", new[] { shaderFolderLocal });
        }

        private static void LogSuccess()
        {
            //Do not remove this log, it is mandatory for the CI
            Debug.Log("SUCCESS !!! : ###" + BuildManager.GetLastBuildPath() + "###");
        }
    }
}
