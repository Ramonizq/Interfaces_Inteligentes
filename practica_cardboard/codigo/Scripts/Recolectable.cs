using UnityEngine;

/// <summary>
/// Objeto que se recolecta al mirarlo cuando el jugador ha llegado hasta él.
/// Recibe los mensajes OnPointerEnter/Exit/Click que envía CardboardReticlePointer, por lo que
/// debe estar en la capa 8 (la interactiva del puntero) y tener un collider.
/// </summary>
public class Recolectable : MonoBehaviour
{
    private enum Estado { Libre, Recolectado, Volviendo }

    private static readonly int ColorBase = Shader.PropertyToID("_BaseColor");
    private static readonly int Corriendo = Animator.StringToHash("Corriendo");

    [Tooltip("Distancia a la que el jugador puede recolectar el objeto con la vista.")]
    public float distanciaRecoleccion = 4f;
    public float velocidadVuelta = 6f;
    [Tooltip("Radio del círculo en el que se colocan los objetos alrededor del jugador.")]
    public float radioAlrededorJugador = 2.5f;
    public Color colorMirado = new Color(1f, 0.85f, 0.2f);

    [HideInInspector] public int indice;

    private Estado estado = Estado.Libre;
    private bool mirado;
    private bool contadoComoRecuperado;
    private Renderer[] renderers;
    private Collider[] colliders;
    private Animator animator;
    private MaterialPropertyBlock bloque;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        colliders = GetComponentsInChildren<Collider>();
        animator = GetComponentInChildren<Animator>();
        bloque = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        GestorRecoleccion.OnRecuperar += Volver;
    }

    private void OnDisable()
    {
        GestorRecoleccion.OnRecuperar -= Volver;
    }

    private void Update()
    {
        GestorRecoleccion gestor = GestorRecoleccion.Instancia;
        if (gestor == null || gestor.jugador == null) return;

        if (estado == Estado.Libre) ComprobarRecoleccion(gestor);
        else if (estado == Estado.Volviendo) MoverHaciaJugador(gestor);
    }

    // Se comprueba en cada frame y no solo en OnPointerEnter para cubrir también el caso de
    // mirar el objeto desde lejos y seguir mirándolo mientras el jugador se acerca.
    private void ComprobarRecoleccion(GestorRecoleccion gestor)
    {
        if (!mirado) return;
        if (DistanciaHorizontal(transform.position, gestor.jugador.position) > distanciaRecoleccion) return;

        estado = Estado.Recolectado;
        // No se desactiva ni se destruye el objeto: el puntero aún le enviará OnPointerExit y
        // SendMessage da error si el objeto no puede recibirlo.
        Mostrar(false);
        gestor.NotificarRecolectado();
    }

    private void Volver()
    {
        // Solo vuelven los que se han recolectado: los demás siguen esperando en su sitio.
        if (estado != Estado.Recolectado) return;

        estado = Estado.Volviendo;
        Mostrar(true);
        // Sin collider no tapan la vista del jugador cuando le rodean.
        foreach (Collider c in colliders) c.enabled = false;
    }

    private void MoverHaciaJugador(GestorRecoleccion gestor)
    {
        // Cada objeto tiene su propio hueco para que no se amontonen todos en el mismo punto.
        float angulo = 360f * indice / Mathf.Max(1, gestor.total);
        Vector3 destino = gestor.jugador.position + Quaternion.Euler(0, angulo, 0) * Vector3.forward * radioAlrededorJugador;

        Vector3 nueva = Vector3.MoveTowards(transform.position, destino, velocidadVuelta * Time.deltaTime);
        nueva.y = AlturaTerreno(nueva);

        Vector3 direccion = gestor.jugador.position - nueva;
        direccion.y = 0;
        if (direccion.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(direccion);

        transform.position = nueva;

        bool haLlegado = DistanciaHorizontal(nueva, destino) < 0.1f;
        if (animator != null) animator.SetBool(Corriendo, !haLlegado);

        if (haLlegado && !contadoComoRecuperado)
        {
            contadoComoRecuperado = true;
            gestor.NotificarRecuperado();
        }
    }

    private void Mostrar(bool visible)
    {
        foreach (Renderer r in renderers) r.enabled = visible;
        foreach (Collider c in colliders) c.enabled = visible;
    }

    private void Resaltar(bool activo)
    {
        foreach (Renderer r in renderers)
        {
            if (activo)
            {
                r.GetPropertyBlock(bloque);
                bloque.SetColor(ColorBase, colorMirado);
                r.SetPropertyBlock(bloque);
            }
            else
            {
                r.SetPropertyBlock(null);
            }
        }
    }

    private static float DistanciaHorizontal(Vector3 a, Vector3 b)
    {
        a.y = b.y = 0;
        return Vector3.Distance(a, b);
    }

    private static float AlturaTerreno(Vector3 p)
    {
        Terrain t = Terrain.activeTerrain;
        return t != null ? t.SampleHeight(p) + t.transform.position.y : p.y;
    }

    // Eventos de la mirada (CardboardReticlePointer los envía con SendMessage).

    public void OnPointerEnter()
    {
        mirado = true;
        Resaltar(true);
    }

    public void OnPointerExit()
    {
        mirado = false;
        Resaltar(false);
    }

    public void OnPointerClick()
    {
        // La recolección es solo con la vista, pero el puntero exige que exista el receptor.
    }
}
