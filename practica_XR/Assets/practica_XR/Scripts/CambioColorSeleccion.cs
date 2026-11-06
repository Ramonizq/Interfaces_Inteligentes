using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Cambia el color del objeto cada vez que un interactor lo selecciona.
/// En la escena lo usan el cubo y la esfera, a los que solo llega el Ray Interactor.
/// </summary>
[RequireComponent(typeof(XRSimpleInteractable), typeof(MeshRenderer))]
public class CambioColorSeleccion : MonoBehaviour
{
    [Tooltip("Colores por los que pasa el objeto, uno por cada selección.")]
    public Color[] colores = { Color.red, Color.green, Color.blue, Color.yellow };

    private XRSimpleInteractable interactuable;
    private Renderer rendererObjeto;
    private MaterialPropertyBlock propiedades;
    private int siguiente;

    private static readonly int ColorBase = Shader.PropertyToID("_BaseColor");

    /// <summary>Veces que se ha seleccionado el objeto desde que empezó la escena.</summary>
    public int Selecciones { get; private set; }

    private void Awake()
    {
        interactuable = GetComponent<XRSimpleInteractable>();
        rendererObjeto = GetComponent<Renderer>();
        propiedades = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        interactuable.selectEntered.AddListener(AlSeleccionar);
    }

    private void OnDisable()
    {
        interactuable.selectEntered.RemoveListener(AlSeleccionar);
    }

    private void AlSeleccionar(SelectEnterEventArgs args)
    {
        Selecciones++;

        // El bloque de propiedades cambia solo este objeto, sin tocar el material compartido.
        rendererObjeto.GetPropertyBlock(propiedades);
        propiedades.SetColor(ColorBase, colores[siguiente]);
        rendererObjeto.SetPropertyBlock(propiedades);

        siguiente = (siguiente + 1) % colores.Length;
    }
}
