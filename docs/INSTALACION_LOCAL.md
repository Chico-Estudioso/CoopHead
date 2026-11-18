# Instalación en una copia local de Cuphead

## Mortar
La web general de Mortar indica que puede localizar juegos desde una tienda o desde una carpeta elegida por el usuario. La ficha de Cuphead indica BepInEx 5. Debe probarse con la carpeta concreta del usuario; no se garantiza que todas las builds no comerciales sean detectadas.

## BepInEx manual
La documentación de BepInEx indica que la instalación se realiza extrayendo los archivos correctos en la raíz del juego, donde está el ejecutable, y ejecutando el juego una vez para generar las carpetas. Hay builds x86 y x64; elegir según el ejecutable.

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
