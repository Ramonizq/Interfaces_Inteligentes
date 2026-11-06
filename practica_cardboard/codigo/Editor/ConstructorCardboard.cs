using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Management;

/// <summary>
/// Menú "Práctica Cardboard": aplica la configuración de la presentación de la práctica,
/// crea la escena y genera el APK. Necesita los paquetes de la Asset Store importados.
/// </summary>
public static class ConstructorCardboard
{
    private const string RAIZ = "Assets/practica_cardboard";
    private const string RUTA_ESCENA = RAIZ + "/Escenas/Recoleccion.unity";
    private const string RUTA_APK = "Builds/practica_cardboard.apk";

    private const string FOOTMAN = "Assets/Mini Legion Footman PBR HP Polyart";
    private const string ZOMBIE = "Assets/Zombie";
    private const string RETICULA = "Packages/com.google.xr.cardboard/Runtime/Prefabs/CardboardReticlePointer.prefab";
    private const string CARGADOR_CARDBOARD = "Google.XR.Cardboard.XRLoader";

    // Capa que CardboardReticlePointer considera interactiva por defecto.
    private const int CAPA_INTERACTIVA = 8;

    private const float LADO_TERRENO = 120f;
    private const float ALTURA_TERRENO = 14f;

    private const string NATURALEZA = "Assets/SimpleNaturePack/Prefabs";
    private const int CAPA_IGNORAR_RAYCAST = 2;

    private static readonly Vector3 POSICION_JUGADOR = new Vector3(0, 0, -10);
    private static readonly Vector3 POSICION_RECUPERADOR = new Vector3(0, 0, 14);
    private static readonly Vector3[] POSICIONES_RECOLECTABLES =
    {
        new Vector3(0, 0, 8), new Vector3(14, 0, 2), new Vector3(-13, 0, 6), new Vector3(9, 0, 20),
        new Vector3(-8, 0, 24), new Vector3(24, 0, 16), new Vector3(-24, 0, 18), new Vector3(0, 0, 34)
    };

    // ===================================================================================
    // MENÚ
    // ===================================================================================

    [MenuItem("Práctica Cardboard/1 - Configurar proyecto para Android", false, 1)]
    public static void ConfigurarAndroid()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

        PlayerSettings.companyName = "Ramonizq";
        PlayerSettings.productName = "Cardboard Recoleccion";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.ramonizq.cardboardrecoleccion");

        // Valores de la presentación de la práctica y de la guía de Google.
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.Android.optimizedFramePacing = false;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.forceInternetPermission = true;
        PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.Activity;
        EditorUserBuildSettings.buildAppBundle = false;

        ConfigurarPlantillasGradle();
        ActivarCardboardXR();

        AssetDatabase.SaveAssets();
        Debug.Log("Práctica Cardboard: proyecto configurado para Android");
    }

    [MenuItem("Práctica Cardboard/2 - Crear la escena", false, 2)]
    public static void CrearEscena()
    {
        Carpeta("Escenas");
        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Terrain terreno = CrearTerreno();
        CrearLuz();

        (Transform jugador, Camera camara) = CrearJugador(terreno);
        Text textoEstado = CrearInterfaz(camara);

        GameObject setup = new GameObject("Setup");
        setup.AddComponent<CardboardStartup>();

        GameObject gestorGO = new GameObject("GestorRecoleccion");
        GestorRecoleccion gestor = gestorGO.AddComponent<GestorRecoleccion>();
        gestor.jugador = jugador;
        gestor.textoEstado = textoEstado;

        gestor.total = CrearRecolectables(terreno);
        CrearRecuperador(terreno);
        CrearDecorado(terreno);
        CrearVegetacion(terreno);

        EditorSceneManager.SaveScene(escena, RUTA_ESCENA);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(RUTA_ESCENA, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("Práctica Cardboard: escena creada en " + RUTA_ESCENA);
    }

    [MenuItem("Práctica Cardboard/3 - Generar APK", false, 3)]
    public static void GenerarApk()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(RUTA_APK));
        BuildPlayerOptions opciones = new BuildPlayerOptions
        {
            scenes = new[] { RUTA_ESCENA },
            locationPathName = RUTA_APK,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.None
        };

        BuildReport informe = BuildPipeline.BuildPlayer(opciones);
        Debug.Log($"Práctica Cardboard: build {informe.summary.result}, {informe.summary.totalErrors} errores, " +
                  $"{informe.summary.totalSize / (1024 * 1024)} MB en {RUTA_APK}");

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(informe.summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }

    // ===================================================================================
    // CONFIGURACIÓN
    // ===================================================================================

    private static void ConfigurarPlantillasGradle()
    {
        string destino = "Assets/Plugins/Android";
        Directory.CreateDirectory(destino);
        string origen = Path.Combine(BuildPipeline.GetPlaybackEngineDirectory(BuildTarget.Android, BuildOptions.None),
            "Tools", "GradleTemplates");

        string main = File.ReadAllText(Path.Combine(origen, "mainTemplate.gradle"));
        const string DEPENDENCIAS =
            "    implementation 'androidx.appcompat:appcompat:1.6.1'\n" +
            "    implementation 'com.google.android.gms:play-services-vision:20.1.3'\n" +
            "    implementation 'com.google.android.material:material:1.12.0'\n" +
            "    implementation 'com.google.protobuf:protobuf-javalite:3.19.4'\n";
        File.WriteAllText(Path.Combine(destino, "mainTemplate.gradle"), main.Replace("**DEPS**", DEPENDENCIAS + "**DEPS**"));

        string propiedades = File.ReadAllText(Path.Combine(origen, "gradleTemplate.properties"));
        const string EXTRA = "android.enableJetifier=true\nandroid.useAndroidX=true\n";
        File.WriteAllText(Path.Combine(destino, "gradleTemplate.properties"),
            propiedades.Replace("**ADDITIONAL_PROPERTIES**", EXTRA + "**ADDITIONAL_PROPERTIES**"));

        AssetDatabase.Refresh();

        SerializedObject ajustes = new SerializedObject(Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings"));
        ajustes.FindProperty("useCustomMainGradleTemplate").boolValue = true;
        ajustes.FindProperty("useCustomGradlePropertiesTemplate").boolValue = true;
        ajustes.ApplyModifiedProperties();
    }

    private static void ActivarCardboardXR()
    {
        Carpeta("XR");
        if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget porPlataforma))
        {
            porPlataforma = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            AssetDatabase.CreateAsset(porPlataforma, "Assets/XR/XRGeneralSettings.asset");
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, porPlataforma, true);
        }

        if (!porPlataforma.HasSettingsForBuildTarget(BuildTargetGroup.Android))
            porPlataforma.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.Android);
        if (!porPlataforma.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android))
            porPlataforma.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);

        XRManagerSettings gestor = porPlataforma.ManagerSettingsForBuildTarget(BuildTargetGroup.Android);
        bool asignado = XRPackageMetadataStore.AssignLoader(gestor, CARGADOR_CARDBOARD, BuildTargetGroup.Android);
        EditorUtility.SetDirty(porPlataforma);
        Debug.Log($"Práctica Cardboard: cargador Cardboard asignado = {asignado}, cargadores activos = {gestor.activeLoaders.Count}");
    }

    // ===================================================================================
    // TERRENO
    // ===================================================================================

    private static Terrain CrearTerreno()
    {
        TerrainData datos = new TerrainData
        {
            heightmapResolution = 129,
            alphamapResolution = 128,
            size = new Vector3(LADO_TERRENO, ALTURA_TERRENO, LADO_TERRENO)
        };

        int res = datos.heightmapResolution;
        float[,] alturas = new float[res, res];
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float u = (float)x / (res - 1);
                float v = (float)y / (res - 1);
                // Colinas suaves en el centro, que suben hacia los bordes para cerrar el escenario.
                float distanciaCentro = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2f;
                float colinas = Mathf.PerlinNoise(u * 5f + 3.1f, v * 5f + 7.7f) * 0.1f;
                float borde = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.75f, 1.05f, distanciaCentro));
                alturas[y, x] = colinas + borde * 0.8f;
            }
        }
        datos.SetHeights(0, 0, alturas);

        datos.terrainLayers = new[]
        {
            CapaTerreno("Hierba", new Color(0.28f, 0.47f, 0.2f), new Color(0.2f, 0.36f, 0.14f)),
            CapaTerreno("Roca", new Color(0.46f, 0.42f, 0.37f), new Color(0.33f, 0.3f, 0.27f))
        };
        PintarTerreno(datos);

        AssetDatabase.CreateAsset(datos, $"{Carpeta("Terreno")}/Terreno.asset");

        GameObject go = Terrain.CreateTerrainGameObject(datos);
        go.name = "Terreno";
        go.transform.position = new Vector3(-LADO_TERRENO / 2f, 0, -LADO_TERRENO / 2f);

        Terrain terreno = go.GetComponent<Terrain>();
        string rutaMaterial = $"{Carpeta("Terreno")}/TerrenoURP.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(rutaMaterial);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit"));
            AssetDatabase.CreateAsset(material, rutaMaterial);
        }
        terreno.materialTemplate = material;
        terreno.drawInstanced = false;
        return terreno;
    }

    private static void PintarTerreno(TerrainData datos)
    {
        int res = datos.alphamapResolution;
        float[,,] mapa = new float[res, res, 2];
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float altura = datos.GetInterpolatedHeight((float)x / (res - 1), (float)y / (res - 1));
                float roca = Mathf.InverseLerp(2.5f, 5f, altura);
                mapa[y, x, 0] = 1f - roca;
                mapa[y, x, 1] = roca;
            }
        }
        datos.SetAlphamaps(0, 0, mapa);
    }

    private static TerrainLayer CapaTerreno(string nombre, Color claro, Color oscuro)
    {
        string carpeta = Carpeta("Terreno");
        string rutaTextura = $"{carpeta}/{nombre}.png";

        const int LADO = 256;
        Texture2D textura = new Texture2D(LADO, LADO, TextureFormat.RGB24, false);
        for (int y = 0; y < LADO; y++)
        {
            for (int x = 0; x < LADO; x++)
            {
                float ruido = Mathf.PerlinNoise(x * 0.06f, y * 0.06f) * 0.6f + Mathf.PerlinNoise(x * 0.25f, y * 0.25f) * 0.4f;
                textura.SetPixel(x, y, Color.Lerp(oscuro, claro, ruido));
            }
        }
        File.WriteAllBytes(rutaTextura, textura.EncodeToPNG());
        Object.DestroyImmediate(textura);
        AssetDatabase.ImportAsset(rutaTextura);

        string rutaCapa = $"{carpeta}/{nombre}.terrainlayer";
        TerrainLayer capa = AssetDatabase.LoadAssetAtPath<TerrainLayer>(rutaCapa);
        if (capa == null)
        {
            capa = new TerrainLayer();
            AssetDatabase.CreateAsset(capa, rutaCapa);
        }
        capa.diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(rutaTextura);
        capa.tileSize = new Vector2(8, 8);
        EditorUtility.SetDirty(capa);
        return capa;
    }

    private static void CrearLuz()
    {
        GameObject go = new GameObject("Directional Light");
        Light luz = go.AddComponent<Light>();
        luz.type = LightType.Directional;
        luz.shadows = LightShadows.None; // Sin sombras: el móvil renderiza la escena dos veces.
        go.transform.rotation = Quaternion.Euler(50, -30, 0);
    }

    // ===================================================================================
    // XR-RIG (Player > Head > Main Camera > retícula), como en la presentación
    // ===================================================================================

    private static (Transform, Camera) CrearJugador(Terrain terreno)
    {
        GameObject jugador = new GameObject("Player");
        jugador.transform.position = EnSuelo(terreno, POSICION_JUGADOR);

        CharacterController controlador = jugador.AddComponent<CharacterController>();
        controlador.height = 1.8f;
        controlador.radius = 0.35f;
        controlador.center = new Vector3(0, 0.9f, 0);
        controlador.slopeLimit = 60f;
        // Con el valor por defecto (0.001) se ignoran los pasos muy cortos y, a muchos FPS, el
        // jugador avanza más despacio: la velocidad no debe depender de los FPS.
        controlador.minMoveDistance = 0f;

        GameObject cabeza = new GameObject("Head");
        cabeza.transform.SetParent(jugador.transform, false);
        cabeza.transform.localPosition = new Vector3(0, 1.6f, 0);

        GameObject camaraGO = new GameObject("Main Camera") { tag = "MainCamera" };
        camaraGO.transform.SetParent(cabeza.transform, false);

        Camera camara = camaraGO.AddComponent<Camera>();
        camara.nearClipPlane = 0.03f;
        camara.farClipPlane = 300f;
        camara.stereoTargetEye = StereoTargetEyeMask.Both;
        camara.allowHDR = false;
        camaraGO.AddComponent<AudioListener>();

        TrackedPoseDriver seguimiento = camaraGO.AddComponent<TrackedPoseDriver>();
        seguimiento.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
        seguimiento.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
        seguimiento.positionInput = new InputActionProperty(new InputAction("Position", InputActionType.PassThrough,
            "<XRHMD>/centerEyePosition", expectedControlType: "Vector3"));
        seguimiento.rotationInput = new InputActionProperty(new InputAction("Rotation", InputActionType.PassThrough,
            "<XRHMD>/centerEyeRotation", expectedControlType: "Quaternion"));

        GameObject prefabReticula = AssetDatabase.LoadAssetAtPath<GameObject>(RETICULA);
        PrefabUtility.InstantiatePrefab(prefabReticula, camaraGO.transform);

        MovimientoJugador movimiento = jugador.AddComponent<MovimientoJugador>();
        movimiento.cabeza = camaraGO.transform;
        movimiento.cabezaEditor = cabeza.transform;

        return (jugador.transform, camara);
    }

    // La UI va en el espacio del mundo: en un visor no se puede usar Screen Space Overlay.
    private static Text CrearInterfaz(Camera camara)
    {
        GameObject go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
        go.transform.SetParent(camara.transform, false);

        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = camara;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(900, 200);
        rt.localPosition = new Vector3(0, 0.75f, 2.5f);
        rt.localScale = Vector3.one * 0.002f;

        GameObject textoGO = new GameObject("TextoEstado", typeof(RectTransform), typeof(Text), typeof(Outline));
        textoGO.transform.SetParent(go.transform, false);
        RectTransform rtTexto = textoGO.GetComponent<RectTransform>();
        rtTexto.anchorMin = Vector2.zero;
        rtTexto.anchorMax = Vector2.one;
        rtTexto.offsetMin = rtTexto.offsetMax = Vector2.zero;

        Text texto = textoGO.GetComponent<Text>();
        texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        texto.fontSize = 44;
        texto.fontStyle = FontStyle.Bold;
        texto.alignment = TextAnchor.MiddleCenter;
        texto.color = Color.white;
        texto.raycastTarget = false;
        return texto;
    }

    // ===================================================================================
    // OBJETOS DE LA ASSET STORE
    // ===================================================================================

    private static int CrearRecolectables(Terrain terreno)
    {
        Vector3[] posiciones = POSICIONES_RECOLECTABLES;

        GameObject padre = new GameObject("Recolectables");
        RuntimeAnimatorController controlador = ControladorFootman();

        for (int i = 0; i < posiciones.Length; i++)
        {
            GameObject go = Instanciar($"{FOOTMAN}/Prefabs/FootmanPolyart.prefab", terreno, posiciones[i], 1.7f);
            go.name = $"Recolectable_{i + 1}";
            go.transform.SetParent(padre.transform, true);
            go.transform.rotation = Quaternion.Euler(0, 180 + i * 40, 0);

            Animator animator = go.GetComponentInChildren<Animator>();
            if (animator == null) animator = go.AddComponent<Animator>();
            animator.runtimeAnimatorController = controlador;

            HacerInteractivo(go, 1.7f);
            go.AddComponent<Recolectable>().indice = i;
        }
        return posiciones.Length;
    }

    private static void CrearRecuperador(Terrain terreno)
    {
        // Se elige un zombi mucho más grande que los del decorado para que se distinga de lejos.
        GameObject zombi = Instanciar($"{ZOMBIE}/Prefabs/Zombie3.prefab", terreno, POSICION_RECUPERADOR, 3.2f);
        zombi.name = "Recuperador";
        zombi.transform.rotation = Quaternion.Euler(0, 180, 0);

        Animator animator = zombi.GetComponentInChildren<Animator>();
        if (animator == null) animator = zombi.AddComponent<Animator>();
        animator.runtimeAnimatorController = ControladorZombi();

        HacerInteractivo(zombi, 3.2f);
        zombi.AddComponent<ObjetoRecuperador>();
    }

    private static void CrearDecorado(Terrain terreno)
    {
        Vector3[] posiciones =
        {
            new Vector3(-30, 0, -14), new Vector3(30, 0, -18), new Vector3(-34, 0, 32),
            new Vector3(34, 0, 34), new Vector3(16, 0, 40), new Vector3(-16, 0, 42)
        };
        string[] prefabs = { "Zombie1", "Zombie2" };

        GameObject padre = new GameObject("Decorado");
        for (int i = 0; i < posiciones.Length; i++)
        {
            GameObject go = Instanciar($"{ZOMBIE}/Prefabs/{prefabs[i % prefabs.Length]}.prefab", terreno, posiciones[i], 1.8f);
            go.transform.SetParent(padre.transform, true);
            go.transform.rotation = Quaternion.Euler(0, i * 67, 0);

            Animator animator = go.GetComponentInChildren<Animator>();
            if (animator == null) animator = go.AddComponent<Animator>();
            animator.runtimeAnimatorController = ControladorZombi();
        }
    }

    // Vegetación del Low-Poly Simple Nature Pack. Se usa una semilla fija para que la escena
    // salga siempre igual al reconstruirla.
    private static void CrearVegetacion(Terrain terreno)
    {
        Random.State estadoPrevio = Random.state;
        Random.InitState(2026);

        Transform padre = new GameObject("Vegetacion").transform;

        // Bosque en las laderas que rodean la zona de juego y algunos árboles sueltos dentro.
        string[] arboles = { "Tree_01", "Tree_02", "Tree_03", "Tree_04", "Tree_05" };
        Esparcir(terreno, padre, arboles, 34, 36f, 56f, 6f, 10f, 5f, true);
        Esparcir(terreno, padre, arboles, 8, 10f, 34f, 6f, 9f, 6f, true);

        Esparcir(terreno, padre, new[] { "Rock_01", "Rock_02", "Rock_03", "Rock_04", "Rock_05" }, 14, 10f, 52f, 0.8f, 2.2f, 4f, true);
        Esparcir(terreno, padre, new[] { "Bush_01", "Bush_02", "Bush_03" }, 26, 6f, 52f, 0.7f, 1.3f, 2.5f, false);
        Esparcir(terreno, padre, new[] { "Grass_01", "Grass_02" }, 45, 4f, 46f, 0.3f, 0.5f, 1f, false);
        Esparcir(terreno, padre, new[] { "Flowers_01", "Flowers_02" }, 30, 4f, 42f, 0.3f, 0.5f, 1f, false);
        Esparcir(terreno, padre, new[] { "Mushroom_01", "Mushroom_02", "Stump_01", "Branch_01" }, 14, 6f, 44f, 0.3f, 0.6f, 2f, false);

        Random.state = estadoPrevio;
    }

    private static void Esparcir(Terrain terreno, Transform padre, string[] prefabs, int cantidad, float radioMin,
                                 float radioMax, float alturaMin, float alturaMax, float hueco, bool obstaculo)
    {
        for (int i = 0; i < cantidad; i++)
        {
            // Si tras varios intentos no hay sitio libre, se deja de colocar ese ejemplar.
            for (int intento = 0; intento < 20; intento++)
            {
                Vector2 punto = Random.insideUnitCircle.normalized * Random.Range(radioMin, radioMax);
                Vector3 posicion = new Vector3(punto.x, 0, punto.y);
                if (!SitioLibre(padre, posicion, hueco)) continue;

                string prefab = prefabs[Random.Range(0, prefabs.Length)];
                GameObject go = Instanciar($"{NATURALEZA}/{prefab}.prefab", terreno, posicion, Random.Range(alturaMin, alturaMax));
                go.transform.SetParent(padre, true);
                go.transform.Rotate(0, Random.Range(0f, 360f), 0, Space.World);
                PrepararVegetacion(go, obstaculo);
                break;
            }
        }
    }

    private static bool SitioLibre(Transform padre, Vector3 p, float hueco)
    {
        if (DistanciaPlana(p, POSICION_JUGADOR) < 5f || DistanciaPlana(p, POSICION_RECUPERADOR) < 5f) return false;
        foreach (Vector3 q in POSICIONES_RECOLECTABLES)
        {
            if (DistanciaPlana(p, q) < 3f) return false;
        }
        foreach (Transform hijo in padre)
        {
            if (DistanciaPlana(p, hijo.position) < hueco) return false;
        }
        return true;
    }

    private static float DistanciaPlana(Vector3 a, Vector3 b)
    {
        a.y = b.y = 0;
        return Vector3.Distance(a, b);
    }

    // Los obstáculos frenan al jugador, pero están en Ignore Raycast para que no bloqueen la
    // mirada: si no, un árbol podría impedir seleccionar un objeto que se ve entre las ramas.
    private static void PrepararVegetacion(GameObject go, bool obstaculo)
    {
        foreach (Collider c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);

        if (obstaculo)
        {
            Bounds limites = Limites(go);
            float escala = go.transform.lossyScale.x;
            go.layer = CAPA_IGNORAR_RAYCAST;
            CapsuleCollider capsula = go.AddComponent<CapsuleCollider>();
            capsula.radius = Mathf.Min(limites.extents.x, 0.6f) / escala;
            capsula.height = limites.size.y / escala;
            capsula.center = new Vector3(0, limites.size.y / 2f / escala, 0);
        }

        foreach (Transform t in go.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = true;
    }

    private static GameObject Instanciar(string ruta, Terrain terreno, Vector3 posicion, float altura)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
        if (prefab == null) throw new FileNotFoundException("No se encuentra " + ruta + ". ¿Están importados los paquetes?");

        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.position = EnSuelo(terreno, posicion);
        ConvertirMaterialesAURP(go);

        // Se escala para que todos los modelos tengan el tamaño pedido, sea cual sea el del paquete.
        if (altura > 0f)
        {
            Bounds limites = Limites(go);
            if (limites.size.y > 0.001f) go.transform.localScale *= altura / limites.size.y;
        }
        return go;
    }

    // El puntero solo envía mensajes a objetos con collider en su capa interactiva, y se queda
    // con el objeto del collider: por eso se pone en la raíz, donde está el script. El collider
    // es algo más ancho que el modelo para que sea cómodo acertar con la mirada.
    private static void HacerInteractivo(GameObject go, float altura)
    {
        foreach (Collider c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
        foreach (Rigidbody r in go.GetComponentsInChildren<Rigidbody>()) Object.DestroyImmediate(r);

        go.layer = CAPA_INTERACTIVA;
        CapsuleCollider capsula = go.AddComponent<CapsuleCollider>();
        float escala = go.transform.lossyScale.y;
        capsula.height = altura / escala;
        capsula.radius = altura * 0.35f / escala;
        capsula.center = new Vector3(0, altura / 2f / escala, 0);
        capsula.isTrigger = true;
    }

    private static Vector3 EnSuelo(Terrain terreno, Vector3 p)
    {
        p.y = terreno.SampleHeight(p) + terreno.transform.position.y;
        return p;
    }

    private static Bounds Limites(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        Bounds b = renderers[0].bounds;
        foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
        return b;
    }

    // Los paquetes usan el shader Standard, que en URP se ve de color magenta.
    private static void ConvertirMaterialesAURP(GameObject go)
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
        {
            foreach (Material m in r.sharedMaterials)
            {
                if (m == null || m.shader == lit || !AssetDatabase.IsMainAsset(m)) continue;

                Texture textura = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
                Color color = m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
                m.shader = lit;
                m.SetTexture("_BaseMap", textura);
                m.SetColor("_BaseColor", color);
                EditorUtility.SetDirty(m);
            }
        }
    }

    // ===================================================================================
    // ANIMACIÓN
    // ===================================================================================

    private static RuntimeAnimatorController ControladorFootman()
    {
        return Controlador("Footman", $"{FOOTMAN}/Animations/Footman_Idle.fbx", $"{FOOTMAN}/Animations/Footman_Run.fbx");
    }

    private static RuntimeAnimatorController ControladorZombi()
    {
        return Controlador("Zombie", $"{ZOMBIE}/Animations/Zombie@Z_Idle.FBX", $"{ZOMBIE}/Animations/Zombie@Z_Run_InPlace.FBX");
    }

    private static RuntimeAnimatorController Controlador(string nombre, string fbxReposo, string fbxCarrera)
    {
        string ruta = $"{Carpeta("Animacion")}/{nombre}.controller";
        AnimatorController existente = AssetDatabase.LoadAssetAtPath<AnimatorController>(ruta);
        if (existente != null) return existente;

        AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(ruta);
        ctrl.AddParameter("Corriendo", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine maquina = ctrl.layers[0].stateMachine;
        AnimatorState reposo = maquina.AddState("Idle");
        reposo.motion = Clip(fbxReposo);
        AnimatorState carrera = maquina.AddState("Run");
        carrera.motion = Clip(fbxCarrera);
        maquina.defaultState = reposo;

        AnimatorStateTransition aCarrera = reposo.AddTransition(carrera);
        aCarrera.AddCondition(AnimatorConditionMode.If, 0, "Corriendo");
        aCarrera.hasExitTime = false;
        aCarrera.duration = 0.1f;

        AnimatorStateTransition aReposo = carrera.AddTransition(reposo);
        aReposo.AddCondition(AnimatorConditionMode.IfNot, 0, "Corriendo");
        aReposo.hasExitTime = false;
        aReposo.duration = 0.1f;

        AssetDatabase.SaveAssets();
        return ctrl;
    }

    private static AnimationClip Clip(string rutaFbx)
    {
        return AssetDatabase.LoadAllAssetsAtPath(rutaFbx)
            .OfType<AnimationClip>()
            .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
    }

    private static string Carpeta(string nombre)
    {
        if (nombre == "XR") return "Assets/XR";

        string ruta = $"{RAIZ}/{nombre}";
        if (!AssetDatabase.IsValidFolder(RAIZ)) AssetDatabase.CreateFolder("Assets", "practica_cardboard");
        if (!AssetDatabase.IsValidFolder(ruta)) AssetDatabase.CreateFolder(RAIZ, nombre);
        return ruta;
    }
}
