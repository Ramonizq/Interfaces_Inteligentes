using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lleva la cuenta de los objetos y comunica los objetos de la escena mediante eventos:
/// el recuperador avisa con Recuperar() y los recolectables, que están suscritos, reaccionan.
/// </summary>
public class GestorRecoleccion : MonoBehaviour
{
    /// <summary>Se lanza cuando el jugador selecciona con la vista el objeto recuperador.</summary>
    public static event Action OnRecuperar;

    public Transform jugador;
    public Text textoEstado;
    public int total;

    public static GestorRecoleccion Instancia { get; private set; }

    public int Recolectados { get; private set; }
    public int Recuperados { get; private set; }

    private void Awake()
    {
        Instancia = this;
    }

    private void Start()
    {
        ActualizarTexto();
    }

    public void NotificarRecolectado()
    {
        Recolectados++;
        ActualizarTexto();
    }

    public void NotificarRecuperado()
    {
        Recuperados++;
        ActualizarTexto();
    }

    public static void Recuperar()
    {
        OnRecuperar?.Invoke();
    }

    private void ActualizarTexto()
    {
        if (textoEstado == null) return;

        string aviso = "";
        if (Recuperados == total) aviso = "\n¡Todos recuperados!";
        else if (Recolectados == total) aviso = "\nMira al zombi gigante para recuperarlos";

        textoEstado.text = $"Recolectados: {Recolectados}/{total}   Recuperados: {Recuperados}/{total}{aviso}";
    }
}
