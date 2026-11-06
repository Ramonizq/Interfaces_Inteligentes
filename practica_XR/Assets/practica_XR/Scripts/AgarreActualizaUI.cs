using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Actualiza un texto de la UI cuando se agarra o se suelta el objeto.
/// En la escena lo usa el objeto de la Asset Store, al que solo llega el Direct Interactor.
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class AgarreActualizaUI : MonoBehaviour
{
    [Tooltip("Texto del Canvas que muestra el estado del objeto.")]
    public Text textoEstado;

    [Tooltip("Nombre del objeto que aparece en el texto.")]
    public string nombreObjeto = "Seta";

    private XRGrabInteractable agarrable;

    /// <summary>Veces que se ha agarrado el objeto desde que empezó la escena.</summary>
    public int Agarres { get; private set; }

    private void Awake()
    {
        agarrable = GetComponent<XRGrabInteractable>();
    }

    private void OnEnable()
    {
        agarrable.selectEntered.AddListener(AlAgarrar);
        agarrable.selectExited.AddListener(AlSoltar);
        Mostrar("sin agarrar");
    }

    private void OnDisable()
    {
        agarrable.selectEntered.RemoveListener(AlAgarrar);
        agarrable.selectExited.RemoveListener(AlSoltar);
    }

    private void AlAgarrar(SelectEnterEventArgs args)
    {
        Agarres++;
        Mostrar("agarrada");
    }

    private void AlSoltar(SelectExitEventArgs args)
    {
        Mostrar("sin agarrar");
    }

    private void Mostrar(string estado)
    {
        textoEstado.text = $"{nombreObjeto}: {estado}\nAgarres: {Agarres}";
    }
}
