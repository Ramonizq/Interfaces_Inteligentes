using UnityEngine;

/// <summary>
/// Objeto que hace volver los recolectados hacia el jugador cuando se selecciona con la vista:
/// hay que mantener la mirada sobre él un tiempo, o tocar el botón del visor mientras se mira.
/// </summary>
public class ObjetoRecuperador : MonoBehaviour
{
    private static readonly int ColorBase = Shader.PropertyToID("_BaseColor");

    public float tiempoMirada = 1.5f;
    public Color colorInicio = Color.white;
    public Color colorActivado = new Color(0.2f, 1f, 0.4f);

    private Renderer[] renderers;
    private MaterialPropertyBlock bloque;
    private bool mirado;
    private bool activado;
    private float tiempo;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        bloque = new MaterialPropertyBlock();
    }

    private void Update()
    {
        if (!mirado || activado) return;

        tiempo += Time.deltaTime;
        Pintar(Color.Lerp(colorInicio, colorActivado, tiempo / tiempoMirada));

        if (tiempo >= tiempoMirada) Activar();
    }

    private void Activar()
    {
        // Hasta que no se aparte la vista no se puede volver a activar.
        activado = true;
        Pintar(colorActivado);
        GestorRecoleccion.Recuperar();
    }

    private void Pintar(Color color)
    {
        foreach (Renderer r in renderers)
        {
            r.GetPropertyBlock(bloque);
            bloque.SetColor(ColorBase, color);
            r.SetPropertyBlock(bloque);
        }
    }

    // Eventos de la mirada (CardboardReticlePointer los envía con SendMessage).

    public void OnPointerEnter()
    {
        mirado = true;
        activado = false;
        tiempo = 0f;
    }

    public void OnPointerExit()
    {
        mirado = false;
        foreach (Renderer r in renderers) r.SetPropertyBlock(null);
    }

    public void OnPointerClick()
    {
        if (!activado) Activar();
    }
}
