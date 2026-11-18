# PROMPT 01 — PLUGIN BepInEx MÍNIMO

## Objetivo
Crear un plugin mínimo que se cargue en la copia de pruebas local de Cuphead. Esta fase no implementa reglas de juego.

## Antes de empezar
Lee M0, `docs/COMPATIBILIDAD.md` y `docs/INSTALACION_LOCAL.md`. Comprueba que se aprobó la ruta de pruebas y que existe copia de seguridad. Si no, detente.

## Reglas Git
Crea una rama `feat/plugin-bepinex-minimo` desde `main` actualizada. No trabajes en main, no hagas merge ni push forzado.

## Implementación
1. Crea solución/proyecto C# usando el target framework que M0 haya verificado.
2. Referencia BepInEx y Harmony/HarmonyX de la instalación aprobada; evita duplicar bibliotecas que ya proporciona el loader.
3. Referencia ensamblados de Cuphead mediante rutas locales fuera del repositorio y excluidas de Git.
4. Implementa una clase de plugin con identificador, nombre y versión propios.
5. Al cargar, escribe mensajes de inicio, versión de juego y estado de servicios disponibles.
6. Añade un comando/hotkey de diagnóstico sencillo solo si se demuestra que no interfiere con controles del juego.
7. Añade configuración básica para activar/desactivar el diagnóstico.
8. No apliques hooks de muerte/revive todavía.

## Calidad
- Manejo de excepciones.
- Logs claros.
- No bloquear el hilo principal.
- No introducir dependencias de voz todavía.
- No copiar archivos propietarios al repositorio.
- Build reproducible desde documentación.

## Pruebas
- `dotnet build` o herramienta equivalente con el runtime verificado.
- Comprobar que el plugin aparece en el log de BepInEx.
- Comprobar que Cuphead inicia normalmente con y sin plugin.
- Registrar versión/build de juego usada.

## Documentación
Actualiza README, CHANGELOG, COMPATIBILIDAD y el procedimiento local. Añade el comando exacto para compilar y dónde copiar el DLL dentro de la copia de pruebas.

## Entrega
Abre una PR lista para revisión con pruebas y limitaciones. No la fusiones. No instales ni reemplaces el loader sin autorización adicional.
