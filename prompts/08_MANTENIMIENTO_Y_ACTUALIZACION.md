# PROMPT 08 — MANTENIMIENTO TRAS ACTUALIZACIÓN DE CUPHEAD

## Objetivo
Adaptar el plugin a una nueva versión de Cuphead o BepInEx sin romper funciones estables.

## Datos
Versión anterior: [DETECTAR]
Nueva versión: [DETECTAR]
Notas de actualización: [INVESTIGAR]

## Procedimiento
1. No modificar producción ni instalación principal.
2. Crear rama `fix/compatibilidad-<version>`.
3. Verificar cambios en ensamblados y runtime.
4. Reauditar cada hook Harmony: tipo, método, firma, parámetros y comportamiento.
5. Identificar hooks removidos/cambiados.
6. Revisar el ciclo de vida de jugadores, fantasmas y reinicios.
7. Ejecutar pruebas unitarias y estáticas.
8. Ejecutar pruebas manuales con los dos jugadores.
9. Comprobar restauración de efectos temporales y reglas adaptativas.
10. Revisar compatibilidad de voz y dependencias nativas.

## Principios
- No declarar compatible una versión no probada.
- Mantener aislados los cambios específicos de juego.
- Si falla un hook crítico, desactivar la función dependiente con aviso, no ejecutar comportamiento incierto.
- No alterar reglas de balance durante una reparación técnica sin documentarlo.
- No añadir dependencias sin revisar licencias.

## Entregables
Actualizar COMPATIBILIDAD, CHANGELOG, ROADMAP, GAME_HOOKS y guía de instalación. Crear release candidate y PR; no fusionar ni publicar sin aprobación.
