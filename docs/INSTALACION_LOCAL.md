# Instalación en una copia local de Cuphead

## Mortar
La web general de Mortar indica que puede localizar juegos desde una tienda o desde una carpeta elegida por el usuario. La ficha de Cuphead indica BepInEx 5. Debe probarse con la carpeta concreta del usuario; no se garantiza que todas las builds no comerciales sean detectadas.

## BepInEx manual
La documentación de BepInEx indica que la instalación se realiza extrayendo los archivos correctos en la raíz del juego, donde está el ejecutable, y ejecutando el juego una vez para generar las carpetas. Hay builds x86 y x64; elegir según el ejecutable.

Según la evidencia comunitaria (`docs/INFORME_M0.md`): BepInEx **5.4.x x64** para Cuphead 1.3 o posterior y x86 para versiones anteriores. Para arrancar sin plugins, DebugMod usa un `.bat` que desactiva Doorstop; si el plugin no carga en Windows, recomienda añadir `steam_appid.txt` junto al ejecutable. Comprobar ambos puntos en la copia local antes de adoptarlos.

## Instalar Coophead en la copia de pruebas
1. Compilar con `dotnet build Coophead.sln -c Release` o descargar el artefacto `Coophead-plugin-sin-probar` de la última ejecución de la CI.
2. Copiar `src/Coophead.Plugin/bin/Release/net35/Coophead.dll` y `src/Coophead.Core/bin/Release/net35/Coophead.Core.dll` a `BepInEx/plugins/Coophead/` de la copia de pruebas. No copiar BepInEx, Harmony ni UnityEngine: los aporta la instalación.
   - Para la voz (opcional, Windows): copiar también `src/Coophead.Voice/bin/Release/net48/Coophead.Voice.exe` y `Coophead.Voice.exe.config` en la misma carpeta y activar `VozActivada`.
3. Arrancar el juego y revisar `BepInEx/LogOutput.log`. Deben aparecer las líneas «Coophead 0.1.0 cargado.» y «Juego: versión …, Unity …, plataforma …, proceso de … bits». Anota esos datos en `docs/COMPATIBILIDAD.md`.
4. Desinstalar = borrar `BepInEx/plugins/Coophead/` y su archivo de configuración en `BepInEx/config/`.

## Recomendación de desarrollo
- Mantener la instalación original sin modificar.
- Crear una copia de prueba independiente.
- Probar primero Mortar seleccionando esa carpeta.
- Si no funciona, instalar manualmente BepInEx compatible en la copia de pruebas.
- No instalar dos loaders ni mezclar perfiles en la misma carpeta.
- Mantener un procedimiento de desinstalación que retire solo los archivos añadidos por el loader/plugin.
- No copiar DLLs privadas del juego al repositorio. Se pueden referenciar desde una ruta local ignorada por Git para compilar.

## Verificaciones antes de instalar
- Ruta exacta de `Cuphead.exe`.
- Versión/build del juego.
- Arquitectura del ejecutable.
- Unity y backend de scripting (Mono o IL2CPP).
- DLC instalado y su versión, si corresponde.
- Compatibilidad de BepInEx y del plugin.

No se debe asumir que el paquete x64 para Cuphead sirve para cualquier ejecutable local.
