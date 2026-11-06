using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.XR.Management;

/// <summary>
/// Opción para probar la práctica en un iPhone. La versión principal es el APK de Android.
/// Configura el proyecto para iOS, así que debe usarse en una copia del proyecto de Unity y no
/// en el que genera el APK (ver IOS/README.md). Genera un proyecto de Xcode en Builds/iOS.
/// </summary>
public static class ConstruirIOS
{
    private const string RUTA_ESCENA = "Assets/practica_cardboard/Escenas/Recoleccion.unity";
    private const string RUTA_XCODE = "Builds/iOS";
    private const string CARGADOR_CARDBOARD = "Google.XR.Cardboard.XRLoader";

    [MenuItem("Práctica Cardboard/Pruebas iOS - Generar proyecto de Xcode")]
    public static void Generar()
    {
        Configurar();
        ActivarCardboard();
        AssetDatabase.SaveAssets();

        Directory.CreateDirectory(RUTA_XCODE);
        BuildReport informe = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { RUTA_ESCENA },
            locationPathName = RUTA_XCODE,
            target = BuildTarget.iOS,
            targetGroup = BuildTargetGroup.iOS,
            options = BuildOptions.None
        });

        Debug.Log($"Pruebas iOS: build {informe.summary.result}, {informe.summary.totalErrors} errores, en {RUTA_XCODE}");

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(informe.summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }

    private static void Configurar()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);

        PlayerSettings.productName = "Cardboard Pruebas";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.ramonizq.cardboardrecoleccion.pruebasios");
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;

        PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.iOS, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.iOS, new[] { UnityEngine.Rendering.GraphicsDeviceType.Metal });

        PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
        PlayerSettings.iOS.targetOSVersionString = "15.0";
        PlayerSettings.iOS.appleEnableAutomaticSigning = true;
        // Cardboard usa la cámara para leer el QR del visor; sin este texto iOS cierra la app.
        PlayerSettings.iOS.cameraUsageDescription = "La cámara se usa para escanear el código QR del visor Cardboard.";
    }

    private static void ActivarCardboard()
    {
        EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget porPlataforma);

        if (!porPlataforma.HasSettingsForBuildTarget(BuildTargetGroup.iOS))
            porPlataforma.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.iOS);
        if (!porPlataforma.HasManagerSettingsForBuildTarget(BuildTargetGroup.iOS))
            porPlataforma.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.iOS);

        XRManagerSettings gestor = porPlataforma.ManagerSettingsForBuildTarget(BuildTargetGroup.iOS);
        bool asignado = XRPackageMetadataStore.AssignLoader(gestor, CARGADOR_CARDBOARD, BuildTargetGroup.iOS);
        EditorUtility.SetDirty(porPlataforma);
        Debug.Log($"Pruebas iOS: cargador Cardboard asignado = {asignado}, cargadores activos = {gestor.activeLoaders.Count}");
    }
}
