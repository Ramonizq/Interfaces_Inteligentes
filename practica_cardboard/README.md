# Práctica 5: Google Cardboard en Unity

**Asignatura:** Interfaces Inteligentes  
**Fecha:** Noviembre 2026  
**Autor:** Ramón Izquierdo Izquierdo

---

## 1. Objetivo y contenido de la entrega

Escena de realidad virtual para móvil con Google Cardboard en la que el jugador recorre un terreno, recolecta objetos con la vista al llegar hasta ellos y, seleccionando otro objeto con la vista, hace que los recolectados acudan hacia él.

```
practica_cardboard/
├── README.md
├── apk/practica_cardboard.apk          APK para Android
└── codigo/
    ├── Scripts/                        Lógica de la escena
    │   ├── GestorRecoleccion.cs
    │   ├── Recolectable.cs
    │   ├── ObjetoRecuperador.cs
    │   └── MovimientoJugador.cs
    ├── Editor/ConstructorCardboard.cs  Configura el proyecto, crea la escena y genera el APK
    ├── SDK_modificado/CardboardStartup.cs   Script del SDK con la corrección del enunciado
    └── Android/                        Plantillas Gradle que exige el SDK
```

* **Para instalar la aplicación:** carpeta `apk/`, que contiene únicamente el APK.
* **Para revisar la lógica del juego:** carpeta `codigo/Scripts/`.
* **Para ver cómo se crea la escena y se configura Android:** `codigo/Editor/ConstructorCardboard.cs`.

Los paquetes de la Asset Store no se incluyen en el repositorio: hay que importarlos para abrir la escena (apartado 5).

---

## 2. Ejercicios

### Ejercicio 1: proyecto Cardboard y APK

Proyecto creado con Unity 6000.6.4f1 y URP, con el plugin Google Cardboard XR 1.36.0 y su ejemplo *Hello Cardboard*. Se ha aplicado la corrección obligatoria en la línea 70 de `CardboardStartup.cs`:

```csharp
#if !UNITY_EDITOR //Modificación
        Api.UpdateScreenParams(); //Línea en el código
#endif //Modificación
```

### Ejercicio 2: recolección con la vista y desplazamiento

* **Recolección:** el jugador recolecta uno de los 8 soldados cuando lo mira y está a 4 m o menos. Mirarlo de lejos, o estar cerca sin mirarlo, no lo recolecta. Se comprueba en cada frame, de modo que también funciona si se mira desde lejos y se sigue mirando mientras se camina hacia él.
* **Desplazamiento con la vista:** inclinar la cabeza entre 25° y 70° por debajo del horizonte hace caminar hacia delante a 4 m/s. Al levantarla, el jugador se para.
* **Desplazamiento con mando:** el stick izquierdo mueve en la dirección de la mirada y tiene prioridad sobre la vista.
* **Terreno y objetos:** terreno de 120 × 120 m con árboles, rocas y vegetación del *Low-Poly Simple Nature Pack*. Los personajes proceden de los paquetes *Mini Legion Footman* y *Zombie*.

### Ejercicio 3: objeto que atrae a los recolectables

El zombi gigante es el objeto recuperador. Al mantener la mirada sobre él durante 1,5 s (o tocar el botón del visor mientras se mira) lanza el evento `GestorRecoleccion.OnRecuperar`. Los recolectables están suscritos a ese evento: reaparecen y corren hasta colocarse en círculo alrededor del jugador, a 2,5 m.

**Interpretación del enunciado:** solo acuden los objetos ya recolectados; los demás siguen en su sitio para poder recogerlos después.

---

## 3. Scripts

| Script | Función | Parámetros principales |
|---|---|---|
| `GestorRecoleccion` | Lleva los contadores, actualiza el texto y publica el evento `OnRecuperar` | `total` = 8 |
| `Recolectable` | Estados libre, recolectado y volviendo; recibe los eventos de la mirada | `distanciaRecoleccion` = 4, `velocidadVuelta` = 6, `radioAlrededorJugador` = 2,5 |
| `ObjetoRecuperador` | Selección por mirada mantenida, con cambio de color progresivo | `tiempoMirada` = 1,5 |
| `MovimientoJugador` | Mueve el `CharacterController` con mando o vista y lo limita al terreno | `velocidad` = 4, `anguloInicio` = 25, `anguloFin` = 70, `margenBorde` = 3 |
| `ConstructorCardboard` | Menú *Práctica Cardboard* del editor: configurar Android, crear la escena y generar el APK | |

---

## 4. Escena y físicas

* **Mirada:** `CardboardReticlePointer` lanza un rayo de hasta 20 m y envía `OnPointerEnter`, `OnPointerExit` y `OnPointerClick` a los objetos de la capa 8.
* **Interactivos (capa 8):** soldados y zombi recuperador, con `CapsuleCollider` en modo *trigger*. El rayo los detecta, pero no bloquean al jugador.
* **Decorado (capa 2, *Ignore Raycast*):** árboles y rocas con cápsula sólida. Bloquean al jugador, pero no tapan la mirada.
* **Jugador:** `CharacterController` sin `Rigidbody`, con `Min Move Distance` = 0. Con el valor por defecto (0,001), a tasas de frames muy altas cada paso quedaba por debajo del mínimo y el jugador apenas avanzaba.
* **Objeto recolectado:** se ocultan sus renderers y colliders, pero no se destruye, porque el puntero aún tiene que enviarle `OnPointerExit` y `SendMessage` exige un receptor.

---

## 5. Configuración y reproducción

| Ajuste | Valor |
|---|---|
| Paquete Android | `com.ramonizq.cardboardrecoleccion` |
| Orientación | Horizontal izquierda |
| API de gráficos | OpenGLES3 |
| Scripting backend | IL2CPP |
| Arquitectura | ARM64 |
| API mínima | 26 |
| Punto de entrada | Activity |
| Optimized Frame Pacing | Desactivado |
| Permiso de Internet | Forzado |
| XR Plug-in Management | Cargador Cardboard activo en Android |

Para reproducir el proyecto:

1. Crear un proyecto URP con Unity 6000.6.4f1 e instalar el plugin de Cardboard siguiendo la [guía oficial](https://developers.google.com/cardboard/develop/unity/quickstart).
2. Importar los tres paquetes de la Asset Store.
3. Copiar `codigo/Scripts` y `codigo/Editor` a `Assets/practica_cardboard/`, y aplicar la corrección de `CardboardStartup.cs`.
4. Ejecutar en orden las tres opciones del menú *Práctica Cardboard*.

---

## 6. Recursos externos

* [Low-Poly Simple Nature Pack](https://assetstore.unity.com/packages/3d/environments/landscapes/low-poly-simple-nature-pack-162153), de JustCreate: árboles, rocas y vegetación.
* *Mini Legion Footman PBR HP Polyart* (Asset Store): soldados recolectables.
* *Zombie* (Asset Store): zombi recuperador y zombis de decorado.
* [Google Cardboard XR Plugin for Unity](https://developers.google.com/cardboard/develop/unity/quickstart) 1.36.0.

Los materiales de estos paquetes se convierten a URP Lit por código desde `ConstructorCardboard`.

---

## 7. Verificación y limitaciones

**Comprobado en el editor de Unity**, en modo Play y con Android como plataforma de destino, con un verificador temporal que no forma parte de la entrega (21 de 21 comprobaciones correctas):

* Velocidad de 4 m/s con la vista, tanto a 60 FPS como sin límite de frames, y con un mando virtual.
* De lejos, o de cerca sin mirar, no se recolecta; de cerca y mirando, sí, y el objeto se oculta.
* El recuperador no se activa antes de tiempo. Al activarse, los objetos recolectados reaparecen y llegan junto al jugador, y uno no recolectado sigue en su sitio.

**Comprobado en el APK** con `aapt2`: paquete `com.ramonizq.cardboardrecoleccion`, API mínima 26, API objetivo 36, `arm64-v8a`, e inclusión de la librería nativa de Cardboard.

**Sin comprobar:**

* El APK no se ha ejecutado en un móvil ni con un visor real. Por tanto, tampoco se han probado el seguimiento de la cabeza, el rendimiento en dispositivo ni la lectura del código QR.
* No se ha usado un mando físico, solo uno virtual.

**Avisos conocidos:**

* La compilación termina con resultado *Succeeded*, pero registra un error de `Unity.Pipeline.Editor.BuildProcessors.PipelineRuntimeBuildProcessor` (`Subtarget property is not available for the platform (Android)`). Procede del paquete `com.unity.pipeline` de la plantilla del proyecto, no del código de la práctica. No impide generar el APK; su efecto en dispositivo no se ha podido comprobar.
* El APK declara los permisos `INTERNET`, `CAMERA`, `NFC` y de lectura y escritura del almacenamiento externo, que no se piden desde los scripts de la práctica.
