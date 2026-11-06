using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Desplaza al jugador (el XR-Rig) con un mando o con la vista.
/// - Mando: el stick izquierdo mueve en la dirección hacia la que se mira.
/// - Vista: inclinar la cabeza hacia abajo hace caminar hacia delante; al levantarla se para.
/// El mando tiene prioridad. En el editor también se puede usar WASD y girar la cabeza con el
/// botón derecho del ratón, ya que allí no hay seguimiento de la cabeza.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class MovimientoJugador : MonoBehaviour
{
    public Transform cabeza;
    public float velocidad = 4f;
    public float gravedad = 9.81f;

    [Header("Movimiento con la vista")]
    public bool moverConVista = true;
    [Tooltip("Grados por debajo del horizonte a partir de los que se empieza a caminar.")]
    public float anguloInicio = 25f;
    [Tooltip("Por encima de este ángulo (mirando a los pies) el jugador se para.")]
    public float anguloFin = 70f;

    [Header("Límites del terreno")]
    public float margenBorde = 3f;

#if UNITY_EDITOR
    [Header("Solo en el editor")]
    public Transform cabezaEditor;
    public float sensibilidadRaton = 0.15f;
    private float guinada;
    private float cabeceo;
#endif

    private CharacterController controlador;
    private float velocidadVertical;

    private void Awake()
    {
        controlador = GetComponent<CharacterController>();
    }

    private void Update()
    {
#if UNITY_EDITOR
        GirarCabezaEnEditor();
#endif
        Vector3 horizontal = CalcularDesplazamiento() * velocidad;
        horizontal = LimitarAlTerreno(horizontal);

        velocidadVertical = controlador.isGrounded ? -1f : velocidadVertical - gravedad * Time.deltaTime;

        controlador.Move((horizontal + Vector3.up * velocidadVertical) * Time.deltaTime);
    }

    private Vector3 CalcularDesplazamiento()
    {
        Vector3 delante = cabeza.forward;
        delante.y = 0;
        delante.Normalize();
        Vector3 derecha = new Vector3(delante.z, 0, -delante.x);

        Vector2 entrada = LeerEntrada();
        if (entrada.sqrMagnitude > 0.01f)
        {
            return Vector3.ClampMagnitude(delante * entrada.y + derecha * entrada.x, 1f);
        }

        return moverConVista && MirandoAlSueloParaCaminar() ? delante : Vector3.zero;
    }

    private bool MirandoAlSueloParaCaminar()
    {
        float anguloBajoHorizonte = Mathf.Asin(Mathf.Clamp(-cabeza.forward.y, -1f, 1f)) * Mathf.Rad2Deg;
        return anguloBajoHorizonte >= anguloInicio && anguloBajoHorizonte <= anguloFin;
    }

    private static Vector2 LeerEntrada()
    {
        Vector2 entrada = Vector2.zero;

        if (Gamepad.current != null) entrada = Gamepad.current.leftStick.ReadValue();

#if UNITY_EDITOR
        Keyboard teclado = Keyboard.current;
        if (entrada == Vector2.zero && teclado != null)
        {
            if (teclado.wKey.isPressed) entrada.y += 1;
            if (teclado.sKey.isPressed) entrada.y -= 1;
            if (teclado.dKey.isPressed) entrada.x += 1;
            if (teclado.aKey.isPressed) entrada.x -= 1;
        }
#endif
        return entrada;
    }

    // Anula el movimiento que sacaría al jugador fuera del terreno.
    private Vector3 LimitarAlTerreno(Vector3 horizontal)
    {
        Terrain terreno = Terrain.activeTerrain;
        if (terreno == null) return horizontal;

        Vector3 origen = terreno.transform.position;
        Vector3 tamano = terreno.terrainData.size;
        Vector3 siguiente = transform.position + horizontal * Time.deltaTime;

        if (siguiente.x < origen.x + margenBorde || siguiente.x > origen.x + tamano.x - margenBorde) horizontal.x = 0;
        if (siguiente.z < origen.z + margenBorde || siguiente.z > origen.z + tamano.z - margenBorde) horizontal.z = 0;

        return horizontal;
    }

#if UNITY_EDITOR
    private void GirarCabezaEnEditor()
    {
        Mouse raton = Mouse.current;
        if (raton == null || cabezaEditor == null || !raton.rightButton.isPressed) return;

        Vector2 delta = raton.delta.ReadValue() * sensibilidadRaton;
        guinada += delta.x;
        cabeceo = Mathf.Clamp(cabeceo - delta.y, -80f, 80f);
        cabezaEditor.localRotation = Quaternion.Euler(cabeceo, guinada, 0);
    }
#endif
}
