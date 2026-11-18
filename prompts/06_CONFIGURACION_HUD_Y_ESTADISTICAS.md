# PROMPT 06 — CONFIGURACIÓN, HUD Y ESTADÍSTICAS

## Objetivo
Hacer que el mod sea configurable sin editar archivos manualmente y mostrar a ambos jugadores qué reglas están activas.

## Primera opción
Usa el sistema de configuración de BepInEx para valores y hotkeys. Solo crea una interfaz propia dentro del juego si la integración es estable y no requiere hooks frágiles.

## Configuración necesaria
- Activar/desactivar reglas de reanimación.
- Límite de reanimaciones recibidas por jugador.
- Activar créditos bilaterales.
- Activar detección de fantasma no rescatado si está verificada.
- Activar/desactivar voz.
- Dispositivo de micrófono.
- Frase de agradecimiento.
- Ventana de agradecimiento (3 s por defecto).
- Fallback manual.
- Activar/desactivar cada resultado de ruleta.
- Peso de cada resultado.
- Duración general (4 s por defecto) y duración por efecto.
- Cooldown y protección contra repeticiones.
- Activar/desactivar Balance Director.
- Intensidad del foco inicial en P2.
- Mostrar/ocultar HUD.

## HUD
Mostrar sin tapar elementos críticos:
- contadores P1/P2;
- créditos de ayuda;
- temporizador de agradecimiento;
- nombre y duración del efecto actual;
- estado del reconocimiento de voz.

## Resumen de nivel
Al terminar el nivel, mostrar muertes, rescates realizados/recibidos, créditos utilizados, agradecimientos reconocidos y penalizaciones aplicadas.

## UX
- Textos claros en español.
- No mostrar micrófono escuchando cuando no lo está.
- Avisar si el modo voz está deshabilitado o no disponible.
- No asumir que F6 es libre: inspeccionar conflictos y permitir cambiar hotkey.
- Evitar overlay que bloquee controles o cubra la acción.

## Entrega
Pruebas de configuración persistente, valores por defecto, invalidación/recuperación de config y capturas o descripción de HUD. PR sin merge.
