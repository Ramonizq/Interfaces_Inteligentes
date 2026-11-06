using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.XR.CoreUtils.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.PackageManager.UI;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

/// <summary>
/// Menú "Práctica XR": importa los samples del XR Interaction Toolkit, activa OpenXR con los
/// perfiles de mandos del guion y crea la escena del ejercicio.
/// </summary>
public static class ConstructorXR
{
    private const string RAIZ = "Assets/practica_XR";
    public const string RUTA_ESCENA = RAIZ + "/Escenas/InteraccionXR.unity";

    private const string PAQUETE_XRI = "com.unity.xr.interaction.toolkit";
    private const string VERSION_XRI = "3.7.0";
    private const string SAMPLES = "Assets/Samples/XR Interaction Toolkit/" + VERSION_XRI;
    private const string STARTER = SAMPLES + "/Starter Assets/Prefabs";
    private const string RIG = STARTER + "/XR Origin (XR Rig).prefab";
    private const string RAY = STARTER + "/Interactors/Ray Interactor.prefab";
    private const string DIRECT = STARTER + "/Interactors/Direct Interactor.prefab";
    private const string SIMULADOR = SAMPLES + "/XR Interaction Simulator/XR Interaction Simulator.prefab";
    private const string SETA = "Assets/SimpleNaturePack/Prefabs/Mushroom_01.prefab";

    private const string NOMBRE_SIMULADOR = "XR Interaction Simulator";
    private const string CARGADOR_OPENXR = "UnityEngine.XR.OpenXR.OpenXRLoader";
    private const string VARIABLE_RUNTIME = "XR_RUNTIME_JSON";
    private const string RUNTIME_META = "/Applications/MetaXRSimulator.app/Contents/Resources/MetaXRSimulator/meta_openxr_simulator.json";

    // Capas de interacción: cada interactor solo puede actuar sobre los objetos de su capa.
    public const string CAPA_RAYO = "Rayo";
    public const string CAPA_DIRECTO = "Directo";

    // Cada objeto a una distancia distinta del jugador, que empieza en el origen mirando a +Z.
    // La seta queda dentro del campo de visión inicial, pero hay que acercarse para tocarla.
    private static readonly Vector3 POSICION_SETA = new Vector3(0.7f, 0f, 2.4f);
    private static readonly Vector3 POSICION_CUBO = new Vector3(-1.5f, 0.25f, 3.8f);
    private static readonly Vector3 POSICION_ESFERA = new Vector3(1.5f, 0.3f, 7f);
    private const float ALTURA_SETA = 0.35f;

    // ===================================================================================
    // MENÚ
    // ===================================================================================

    [MenuItem("Práctica XR/1 - Importar samples del XR Interaction Toolkit", false, 1)]
    public static void ImportarSamples()
    {
        string[] necesarios = { "Starter Assets", "XR Interaction Simulator" };
        foreach (Sample sample in Sample.FindByPackage(PAQUETE_XRI, VERSION_XRI))
        {
            if (necesarios.Contains(sample.displayName) && !sample.isImported)
            {
                sample.Import(Sample.ImportOptions.HideImportWindow);
                Debug.Log("Práctica XR: importado el sample " + sample.displayName);
            }
        }
    }

    [MenuItem("Práctica XR/2 - Configurar OpenXR", false, 2)]
    public static void ConfigurarXR()
    {
        PlayerSettings.companyName = "Ramonizq";
        PlayerSettings.productName = "Interaccion XR";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.ramonizq.interaccionxr");

        // Requisitos de Android para Quest.
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

        ActivarOpenXR(BuildTargetGroup.Standalone);
        ActivarOpenXR(BuildTargetGroup.Android);
        CrearCapasInteraccion();

        CorregirValidacion(BuildTargetGroup.Standalone);
        CorregirValidacion(BuildTargetGroup.Android);

        AssetDatabase.SaveAssets();
        Debug.Log("Práctica XR: OpenXR configurado");
    }

    // Equivale al botón Fix All de Project Settings > XR Plug-in Management > Project Validation.
    // La consulta de problemas es interna al paquete, de ahí la reflexión.
    private static void CorregirValidacion(BuildTargetGroup grupo)
    {
        MethodInfo consulta = typeof(BuildValidator).GetMethod("GetCurrentValidationIssues", BindingFlags.NonPublic | BindingFlags.Static);
        var fallos = new HashSet<BuildValidationRule>();
        consulta.Invoke(null, new object[] { fallos, grupo });
        foreach (BuildValidationRule regla in fallos.ToList())
        {
            if (regla.FixIt != null && regla.FixItAutomatic) regla.FixIt();
        }

        consulta.Invoke(null, new object[] { fallos, grupo });
        Debug.Log($"Práctica XR: validación de {grupo}: quedan {fallos.Count} avisos");
        foreach (BuildValidationRule regla in fallos)
        {
            Debug.Log($"Práctica XR:   [{(regla.Error ? "error" : "aviso")}] {regla.Message}");
        }
    }

    [MenuItem("Práctica XR/3 - Crear la escena", false, 3)]
    public static void CrearEscena()
    {
        Carpeta("Escenas");
        CrearCapasInteraccion();

        // La escena por defecto trae Main Camera y luz. El XR Origin trae su propia cámara.
        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        Object.DestroyImmediate(GameObject.Find("Main Camera"));

        new GameObject("XR Interaction Manager").AddComponent<XRInteractionManager>();
        CrearRig();
        CrearSimulador();

        CrearPlano();
        CrearSeleccionable(PrimitiveType.Cube, "Cubo", POSICION_CUBO, Vector3.one * 0.5f);
        CrearSeleccionable(PrimitiveType.Sphere, "Esfera", POSICION_ESFERA, Vector3.one * 0.6f);
        CrearSeta(CrearInterfaz());

        EditorSceneManager.SaveScene(escena, RUTA_ESCENA);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(RUTA_ESCENA, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("Práctica XR: escena creada en " + RUTA_ESCENA);
    }

    // Activa la aplicación de Meta como runtime OpenXR solo para este proceso del editor, sin
    // cambiar el runtime del sistema. Hay que repetirlo cada vez que se abre Unity.
    [MenuItem("Práctica XR/4 - Usar Meta XR Simulator", false, 20)]
    public static void UsarSimuladorMeta()
    {
        if (!File.Exists(RUNTIME_META))
        {
            Debug.LogError("Práctica XR: no se encuentra el Meta XR Simulator en " + RUNTIME_META);
            return;
        }
        System.Environment.SetEnvironmentVariable(VARIABLE_RUNTIME, RUNTIME_META);
        ActivarSimuladorGenerico(false);
        Debug.Log("Práctica XR: Meta XR Simulator activado. Ya se puede pulsar Play.");
    }

    [MenuItem("Práctica XR/5 - Usar simulador genérico", false, 21)]
    public static void UsarSimuladorGenerico()
    {
        System.Environment.SetEnvironmentVariable(VARIABLE_RUNTIME, null);
        ActivarSimuladorGenerico(true);
        Debug.Log("Práctica XR: simulador genérico activado. Ya se puede pulsar Play.");
    }

    // Los dos simuladores no pueden convivir: ambos crearían su propia cabeza y sus mandos.
    private static void ActivarSimuladorGenerico(bool activo)
    {
        Scene escena = EditorSceneManager.OpenScene(RUTA_ESCENA, OpenSceneMode.Single);
        GameObject simulador = escena.GetRootGameObjects().First(go => go.name == NOMBRE_SIMULADOR);
        simulador.SetActive(activo);
        EditorSceneManager.SaveScene(escena);
    }

    // ===================================================================================
    // CONFIGURACIÓN
    // ===================================================================================

    private static void ActivarOpenXR(BuildTargetGroup grupo)
    {
        if (!AssetDatabase.IsValidFolder("Assets/XR")) AssetDatabase.CreateFolder("Assets", "XR");

        if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget porPlataforma))
        {
            porPlataforma = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            AssetDatabase.CreateAsset(porPlataforma, "Assets/XR/XRGeneralSettings.asset");
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, porPlataforma, true);
        }

        if (!porPlataforma.HasSettingsForBuildTarget(grupo)) porPlataforma.CreateDefaultSettingsForBuildTarget(grupo);
        if (!porPlataforma.HasManagerSettingsForBuildTarget(grupo)) porPlataforma.CreateDefaultManagerSettingsForBuildTarget(grupo);

        XRManagerSettings gestor = porPlataforma.ManagerSettingsForBuildTarget(grupo);
        bool asignado = XRPackageMetadataStore.AssignLoader(gestor, CARGADOR_OPENXR, grupo);
        EditorUtility.SetDirty(porPlataforma);

        FeatureHelpers.RefreshFeatures(grupo);
        OpenXRSettings ajustes = OpenXRSettings.GetSettingsForBuildTargetGroup(grupo);

        // Perfiles de mandos de Quest 2, Quest 3 / 3S y Quest Pro.
        ActivarCaracteristica<OculusTouchControllerProfile>(ajustes);
        ActivarCaracteristica<MetaQuestTouchPlusControllerProfile>(ajustes);
        ActivarCaracteristica<MetaQuestTouchProControllerProfile>(ajustes);
        if (grupo == BuildTargetGroup.Android) ActivarCaracteristica<MetaQuestFeature>(ajustes);

        EditorUtility.SetDirty(ajustes);
        Debug.Log($"Práctica XR: OpenXR en {grupo}: cargador = {asignado}, activos = {gestor.activeLoaders.Count}");
    }

    private static void ActivarCaracteristica<T>(OpenXRSettings ajustes) where T : OpenXRFeature
    {
        T caracteristica = ajustes.GetFeature<T>();
        if (caracteristica == null)
        {
            Debug.LogWarning("Práctica XR: no existe la característica " + typeof(T).Name);
            return;
        }
        caracteristica.enabled = true;
        EditorUtility.SetDirty(caracteristica);
    }

    // InteractionLayerSettings es interna al paquete, de ahí la reflexión. Equivale a escribir
    // los nombres en Edit > Project Settings > XR Plug-in Management > XR Interaction Toolkit.
    private static void CrearCapasInteraccion()
    {
        System.Type tipo = typeof(XRInteractionManager).Assembly.GetTypes().First(t => t.Name == "InteractionLayerSettings");
        Object instancia = (Object)tipo.BaseType.GetProperty("Instance").GetValue(null);

        SerializedObject ajustes = new SerializedObject(instancia);
        SerializedProperty nombres = ajustes.FindProperty("m_LayerNames");
        nombres.GetArrayElementAtIndex(1).stringValue = CAPA_RAYO;
        nombres.GetArrayElementAtIndex(2).stringValue = CAPA_DIRECTO;
        ajustes.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
    }

    // ===================================================================================
    // XR ORIGIN Y SIMULADOR
    // ===================================================================================

    private static void CrearRig()
    {
        GameObject rig = (GameObject)PrefabUtility.InstantiatePrefab(CargarPrefab(RIG));
        PrefabUtility.UnpackPrefabInstance(rig, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        rig.name = "XR Origin (XR Rig)";

        ConfigurarMando(rig.transform.Find("Camera Offset/Left Controller"));
        ConfigurarMando(rig.transform.Find("Camera Offset/Right Controller"));
    }

    // El prefab del XR Origin usa un Near-Far Interactor, que reúne rayo y agarre directo. El
    // ejercicio pide un interactor de cada tipo, así que se sustituye por los dos prefabs de
    // Starter Assets, conectados a las mismas acciones de entrada.
    private static void ConfigurarMando(Transform mando)
    {
        NearFarInteractor nearFar = mando.GetComponentInChildren<NearFarInteractor>(true);

        XRDirectInteractor direct = InstanciarInteractor<XRDirectInteractor>(DIRECT, mando, nearFar, CAPA_DIRECTO);
        XRRayInteractor ray = InstanciarInteractor<XRRayInteractor>(RAY, mando, nearFar, CAPA_RAYO);
        ray.maxRaycastDistance = 15f;
        ray.enableUIInteraction = false; // La UI de la escena solo muestra información.
        EditorUtility.SetDirty(ray);
        HacerVisibleLineaRayo(ray);

        // Dentro del grupo, el primero tiene prioridad: si la mano toca la seta, manda el agarre.
        XRInteractionGroup grupo = mando.GetComponent<XRInteractionGroup>();
        grupo.startingGroupMembers = new List<Object> { direct, ray };
        SerializedObject grupoSerializado = new SerializedObject(grupo);
        grupoSerializado.FindProperty("m_StartingInteractionOverridesMap").arraySize = 0;
        grupoSerializado.ApplyModifiedProperties();

        // El gestor de entrada del sample activa el rayo o el teletransporte según el stick. El
        // ejercicio no usa teletransporte: el jugador se mueve con la locomoción continua.
        Component gestorEntrada = mando.GetComponent("ControllerInputActionManager");
        SerializedObject gestor = new SerializedObject(gestorEntrada);
        gestor.FindProperty("m_RayInteractor").objectReferenceValue = ray;
        gestor.FindProperty("m_NearFarInteractor").objectReferenceValue = null;
        gestor.FindProperty("m_TeleportInteractor").objectReferenceValue = null;
        gestor.ApplyModifiedProperties();

        nearFar.gameObject.SetActive(false);
        mando.Find("Teleport Interactor").gameObject.SetActive(false);
        mando.Find("Poke Interactor").gameObject.SetActive(false);
    }

    // Tal como viene, el prefab no dibuja la línea en esta escena: es corta, se desvanece y su
    // material no se pinta en URP. Queda blanca si no apunta a nada y azul sobre un objeto
    // que se puede seleccionar.
    private static void HacerVisibleLineaRayo(XRRayInteractor ray)
    {
        XRInteractorLineVisual linea = ray.GetComponent<XRInteractorLineVisual>();
        linea.lineWidth = 0.006f;
        linea.overrideInteractorLineLength = false; // La línea mide lo que alcanza el rayo (15 m).
        linea.autoAdjustLineLength = false;
        linea.invalidColorGradient = new Gradient
        {
            colorKeys = new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            alphaKeys = new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.5f, 1f) }
        };
        Color azul = new Color(0f, 0.63f, 1f);
        linea.validColorGradient = new Gradient
        {
            colorKeys = new[] { new GradientColorKey(azul, 0f), new GradientColorKey(azul, 1f) },
            alphaKeys = new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
        };
        EditorUtility.SetDirty(linea);

        LineRenderer trazo = ray.GetComponent<LineRenderer>();
        trazo.sharedMaterial = MaterialLinea();
        EditorUtility.SetDirty(trazo);
    }

    private static Material MaterialLinea()
    {
        string ruta = $"{Carpeta("Materiales")}/LineaRayo.mat";
        Shader particulas = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (material == null)
        {
            material = new Material(particulas);
            AssetDatabase.CreateAsset(material, ruta);
        }
        material.shader = particulas;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static T InstanciarInteractor<T>(string ruta, Transform mando, NearFarInteractor modelo, string capa)
        where T : XRBaseInputInteractor
    {
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(CargarPrefab(ruta), mando);
        PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        T interactor = go.GetComponent<T>();
        interactor.handedness = modelo.handedness;
        interactor.interactionLayers = InteractionLayerMask.GetMask(capa);
        interactor.selectInput.inputActionReferencePerformed = modelo.selectInput.inputActionReferencePerformed;
        interactor.selectInput.inputActionReferenceValue = modelo.selectInput.inputActionReferenceValue;
        interactor.activateInput.inputActionReferencePerformed = modelo.activateInput.inputActionReferencePerformed;
        interactor.activateInput.inputActionReferenceValue = modelo.activateInput.inputActionReferenceValue;
        EditorUtility.SetDirty(interactor);
        return interactor;
    }

    // Con la etiqueta EditorOnly, Unity no incluye el simulador al compilar para las gafas.
    private static void CrearSimulador()
    {
        GameObject simulador = (GameObject)PrefabUtility.InstantiatePrefab(CargarPrefab(SIMULADOR));
        simulador.tag = "EditorOnly";
    }

    // ===================================================================================
    // OBJETOS
    // ===================================================================================

    private static void CrearPlano()
    {
        GameObject plano = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plano.name = "Plano";
        plano.transform.localScale = new Vector3(3f, 1f, 3f); // 30 x 30 m
        plano.GetComponent<Renderer>().sharedMaterial = Material("Plano", new Color(0.42f, 0.5f, 0.4f));
    }

    // Cubo y esfera: XR Simple Interactable se puede seleccionar, pero no se mueve al hacerlo.
    private static void CrearSeleccionable(PrimitiveType tipo, string nombre, Vector3 posicion, Vector3 escala)
    {
        GameObject go = GameObject.CreatePrimitive(tipo);
        go.name = nombre;
        go.transform.position = posicion;
        go.transform.localScale = escala;
        go.GetComponent<Renderer>().sharedMaterial = Material("Seleccionable", new Color(0.85f, 0.85f, 0.85f));

        XRSimpleInteractable interactuable = go.AddComponent<XRSimpleInteractable>();
        interactuable.interactionLayers = InteractionLayerMask.GetMask(CAPA_RAYO);
        go.AddComponent<CambioColorSeleccion>();
    }

    private static Text CrearInterfaz()
    {
        GameObject lienzo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(Image));
        lienzo.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        lienzo.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.14f, 0.85f);

        RectTransform marco = lienzo.GetComponent<RectTransform>();
        marco.sizeDelta = new Vector2(500, 160);
        marco.position = new Vector3(0f, 2.1f, 3f);
        marco.localScale = Vector3.one * 0.004f; // 2 x 0,64 m

        GameObject textoGO = new GameObject("TextoEstado", typeof(RectTransform), typeof(Text));
        textoGO.transform.SetParent(lienzo.transform, false);
        RectTransform rt = textoGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        Text texto = textoGO.GetComponent<Text>();
        texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        texto.fontSize = 44;
        texto.alignment = TextAnchor.MiddleCenter;
        texto.color = Color.white;
        texto.raycastTarget = false;
        return texto;
    }

    // Objeto de la Asset Store: agarrable solo con el Direct Interactor.
    private static void CrearSeta(Text textoEstado)
    {
        GameObject seta = (GameObject)PrefabUtility.InstantiatePrefab(CargarPrefab(SETA));
        seta.name = "Seta";
        ConvertirMaterialesAURP(seta);
        foreach (Collider c in seta.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);

        seta.transform.position = POSICION_SETA;
        Bounds limites = Limites(seta);
        seta.transform.localScale *= ALTURA_SETA / limites.size.y;

        // Se apoya sobre el plano, sea cual sea el punto de origen del modelo.
        limites = Limites(seta);
        seta.transform.position += Vector3.up * (POSICION_SETA.y - limites.min.y);
        limites = Limites(seta);

        float escala = seta.transform.lossyScale.y;
        BoxCollider caja = seta.AddComponent<BoxCollider>();
        caja.center = (limites.center - seta.transform.position) / escala;
        caja.size = limites.size / escala;

        Rigidbody cuerpo = seta.AddComponent<Rigidbody>();
        cuerpo.mass = 0.3f;

        XRGrabInteractable agarrable = seta.AddComponent<XRGrabInteractable>();
        agarrable.interactionLayers = InteractionLayerMask.GetMask(CAPA_DIRECTO);
        agarrable.useDynamicAttach = true; // Se agarra por donde la toca la mano, sin saltos.

        AgarreActualizaUI actualizador = seta.AddComponent<AgarreActualizaUI>();
        actualizador.textoEstado = textoEstado;
        textoEstado.text = "Seta: sin agarrar\nAgarres: 0";
    }

    // ===================================================================================
    // UTILIDADES
    // ===================================================================================

    private static GameObject CargarPrefab(string ruta)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
        if (prefab == null) throw new FileNotFoundException($"No se encuentra {ruta}. ¿Están importados los paquetes y samples?");
        return prefab;
    }

    private static Material Material(string nombre, Color color)
    {
        string ruta = $"{Carpeta("Materiales")}/{nombre}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, ruta);
        }
        material.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(material);
        return material;
    }

    // El paquete de la Asset Store usa el shader Standard, que en URP se ve de color magenta.
    private static void ConvertirMaterialesAURP(GameObject go)
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
        {
            foreach (Material m in r.sharedMaterials)
            {
                if (m == null || m.shader == lit) continue;

                Texture textura = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
                Color color = m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
                m.shader = lit;
                m.SetTexture("_BaseMap", textura);
                m.SetColor("_BaseColor", color);
                EditorUtility.SetDirty(m);
            }
        }
    }

    private static Bounds Limites(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        Bounds b = renderers[0].bounds;
        foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
        return b;
    }

    private static string Carpeta(string nombre)
    {
        string ruta = $"{RAIZ}/{nombre}";
        if (!AssetDatabase.IsValidFolder(RAIZ)) AssetDatabase.CreateFolder("Assets", "practica_XR");
        if (!AssetDatabase.IsValidFolder(ruta)) AssetDatabase.CreateFolder(RAIZ, nombre);
        return ruta;
    }
}
