using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;

public static class BuildScript
{
    private const string MainScenePath = "Assets/Scenes/Main.unity";
    private const string BundleId = "com.moad.moad3dscan";

    public static void BuildIOS()
    {
        EnsureMainScene();
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
        PlayerSettings.productName = "MOAD 3D Scan";
        PlayerSettings.companyName = "MOAD";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, BundleId);
        PlayerSettings.iOS.cameraUsageDescription = "MOAD 3D Scan uses the camera to capture images for your 3D scan.";
        PlayerSettings.iOS.targetOSVersionString = "13.0";
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.iOS, ScriptingImplementation.IL2CPP);
        PlayerSettings.iOS.appleEnableAutomaticSigning = false;
        ConfigureARKitLoader();

        Directory.CreateDirectory("build/ios");
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { MainScenePath },
            locationPathName = "build/ios",
            target = BuildTarget.iOS,
            options = BuildOptions.None
        });

        if (report.summary.result != BuildResult.Succeeded)
            throw new System.Exception($"iOS Unity build failed: {report.summary.result}");
    }

    [PostProcessBuild(1)]
    public static void LinkTorchFramework(BuildTarget target, string buildPath)
    {
        if (target != BuildTarget.iOS)
            return;

        string projectPath = PBXProject.GetPBXProjectPath(buildPath);
        PBXProject project = new PBXProject();
        project.ReadFromFile(projectPath);
        string frameworkTarget = project.GetUnityFrameworkTargetGuid();
        project.AddFrameworkToProject(frameworkTarget, "AVFoundation.framework", false);
        project.WriteToFile(projectPath);
    }

    private static void ConfigureARKitLoader()
    {
        XRGeneralSettingsPerBuildTarget settings = XRGeneralSettingsPerBuildTarget.GetOrCreate();
        if (!settings.HasSettingsForBuildTarget(BuildTargetGroup.iOS))
            settings.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.iOS);
        if (!settings.HasManagerSettingsForBuildTarget(BuildTargetGroup.iOS))
            settings.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.iOS);

        XRGeneralSettings general = settings.SettingsForBuildTarget(BuildTargetGroup.iOS);
        bool assigned = XRPackageMetadataStore.AssignLoader(
            general.Manager, "UnityEngine.XR.ARKit.ARKitLoader", BuildTargetGroup.iOS);
        if (!assigned)
            throw new System.Exception("Could not enable the ARKit loader for iOS.");

        EditorUtility.SetDirty(general);
        AssetDatabase.SaveAssets();
    }

    private static void EnsureMainScene()
    {
        Directory.CreateDirectory("Assets/Scenes");
        if (!File.Exists(MainScenePath))
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("MOAD App").AddComponent<MoadAppBootstrap>();
            EditorSceneManager.SaveScene(scene, MainScenePath);
        }

        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(MainScenePath, true) };
    }
}
