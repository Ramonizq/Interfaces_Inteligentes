# Opción para iPhone

Permite probar la práctica en un iPhone con Google Cardboard. **La versión principal de la práctica es el APK de Android**: esta opción es una compilación aparte, con la misma escena y los mismos scripts de `codigo/`.

No se incluye una aplicación ya compilada: en iOS debe firmarse con la cuenta de Apple de quien la instala. Tampoco se incluye el proyecto de Xcode generado, porque ocupa 1,4 GB. Ambos se obtienen con los pasos de este documento.

---

## 1. Requisitos

* Un Mac con Unity 6000.6.4f1, el módulo **iOS Build Support** y Xcode.
* Un Apple ID. Basta con una cuenta gratuita, pero la firma caduca a los 7 días.
* Un iPhone y un cable. Un visor Cardboard es opcional para comprobar el juego.
* El proyecto de Unity ya preparado, con los paquetes y la escena (apartado 5 del [README principal](../README.md)).

## 2. Generar el proyecto de Xcode

1. **Haz una copia del proyecto de Unity.** El script cambia la plataforma y los ajustes, y así el proyecto que genera el APK no se ve afectado.
2. Copia `ConstruirIOS.cs` en `Assets/practica_cardboard/Editor/` dentro de la copia.
3. Abre la copia con Unity y usa el menú **Práctica Cardboard > Pruebas iOS - Generar proyecto de Xcode**.
4. El proyecto se genera en `Builds/iOS/Unity-iPhone.xcodeproj`, dentro de la copia.

## 3. Instalar en el iPhone

1. Abre `Unity-iPhone.xcodeproj` con Xcode y añade tu Apple ID en **Xcode > Settings > Accounts**.
2. Conecta el iPhone y pulsa **Confiar en este ordenador**.
3. Selecciona **Unity-iPhone** (icono azul) y, en **TARGETS**, **Unity-iPhone**. En **Signing & Capabilities**, deja marcado **Automatically manage signing** y elige tu cuenta en **Team**. Si Xcode indica que el identificador ya está en uso, cambia el **Bundle Identifier** por otro propio.
4. En el iPhone, activa **Ajustes > Privacidad y seguridad > Modo de desarrollador**. Hay que reiniciarlo, sin que se borren datos.
5. En Xcode, elige el iPhone como destino y pulsa **Run** (▶).
6. Si la app no abre, ve a **Ajustes > General > VPN y gestión de dispositivos**, entra en tu cuenta y pulsa **Confiar**. El iPhone necesita Internet para este paso.

Cuando caduque la firma, la app deja de abrirse. Para renovarla, vuelve a pulsar **Run**.

## 4. Cómo se usa

Se juega igual que en Android: ver los ejercicios 2 y 3 del [README principal](../README.md).

## 5. Ajustes que aplica el script

| Ajuste | Valor |
|---|---|
| Identificador | `com.ramonizq.cardboardrecoleccion.pruebasios` |
| Orientación | Horizontal izquierda |
| API de gráficos | Metal |
| Scripting backend | IL2CPP |
| iOS mínimo | 15.0 |
| Permiso de cámara | Necesario para escanear el QR del visor |
| XR Plug-in Management | Cargador Cardboard activo en iOS |

## 6. Verificación

Se compiló con Xcode 27.2 en macOS 27.2 y su autor la probó en un iPhone 17 Pro con iOS 27.0.1, donde funciona correctamente. El proyecto se generó ejecutando el mismo método `ConstruirIOS.Generar` en modo *batch*, no desde el menú.

Esta prueba valida la escena y el juego en un móvil real, pero **no el APK**, que usa OpenGLES3 en lugar de Metal.
