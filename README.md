# Prácticas de Interfaces Inteligentes

**Autor:** Ramón Izquierdo Izquierdo

Este repositorio contiene dos prácticas independientes de realidad virtual hechas con Unity 6000.6.4f1. Cada carpeta tiene su propio README con la explicación completa.

| Carpeta | Práctica | Qué contiene | Qué hacer con ella |
|---|---|---|---|
| [`practica_cardboard/`](practica_cardboard/README.md) | Google Cardboard | APK para Android y código | Instalar el APK en un móvil |
| [`practica_XR/`](practica_XR/README.md) | XR Interaction Toolkit | Proyecto de Unity | Abrirlo con Unity y pulsar Play |

---

## `practica_cardboard/`

Escena para móvil con visor Cardboard. El jugador recorre un terreno, recolecta objetos con la vista y, al seleccionar con la vista un objeto recuperador, los recolectados acuden hacia él.

**Para probarla:**

1. Instalar `apk/practica_cardboard.apk` en un móvil Android.
2. Abrir la aplicación y colocar el móvil en el visor.

**Para revisar el código:** la lógica está en `codigo/Scripts/`, y la creación de la escena en `codigo/Editor/ConstructorCardboard.cs`.

Esta carpeta no es un proyecto de Unity completo. Para reconstruirlo hay que seguir el apartado 5 de [su README](practica_cardboard/README.md). Para probarla en un iPhone, están las instrucciones de [`IOS/README.md`](practica_cardboard/IOS/README.md).

---

## `practica_XR/`

Escena con un cubo, una esfera y una seta de la Asset Store sobre un plano. El cubo y la esfera cambian de color al seleccionarlos con un Ray Interactor, y la seta actualiza la UI al agarrarla con un Direct Interactor.

**Para probarla:**

1. Abrir la carpeta `practica_XR` con Unity 6000.6.4f1 desde Unity Hub, con *Add project from disk*.
2. Importar el [*Low-Poly Simple Nature Pack*](https://assetstore.unity.com/packages/3d/environments/landscapes/low-poly-simple-nature-pack-162153) de la Asset Store, que no se incluye en el repositorio.
3. Ejecutar *Práctica XR > 3 - Crear la escena*.
4. Abrir `Assets/practica_XR/Escenas/InteraccionXR.unity` y pulsar Play.

Por defecto usa el simulador genérico del XR Interaction Toolkit, y también puede usarse con el Meta XR Simulator. Los controles de ambos están en el apartado 5 de [su README](practica_XR/README.md), y hay capturas en `capturas/`.

**Para revisar el código:** la lógica está en `Assets/practica_XR/Scripts/`, y la creación de la escena en `Assets/practica_XR/Editor/ConstructorXR.cs`.
