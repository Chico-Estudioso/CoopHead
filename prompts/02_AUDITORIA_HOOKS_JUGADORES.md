# PROMPT 02 — AUDITORÍA DE JUGADORES, MUERTES Y REANIMACIONES

## Objetivo
Identificar y verificar la secuencia real de eventos del cooperativo local de Cuphead antes de implementar reglas.

## Preparación
Lee arquitectura y compatibilidad. Rama: `feat/auditoria-hooks-coop`. No cambies el comportamiento del juego; añade solo instrumentación temporal y segura.

## Investiga
- Cómo se identifican Player 1 y Player 2.
- Qué objeto representa el estado de cada jugador.
- Cuándo se registra daño y muerte.
- Cuándo se crea/controla el fantasma.
- Qué métodos permiten reanimar y cómo se confirma un éxito.
- Cómo termina la ventana de rescate y qué ocurre si el fantasma se pierde.
- Qué eventos indican inicio, retry, fin o transición del nivel.
- Cómo distinguir juego activo de menús, cutscenes y pantallas de resultado.

## Método
1. Inspecciona ensamblados exactos de la copia local.
2. Documenta clase, método, firma y ensamblado de cada hook candidato.
3. Crea Harmony patches temporales que registren eventos sin alterar valores/retornos.
4. Añade identificador de sesión y secuencia para deduplicar eventos.
5. Ejecuta una matriz manual: muere P1, rescata P1; muere P2, rescata P2; ambos alternan; ghost expira; retry; transición de escena; cutscene.
6. Comprueba cada resultado en logs.

## Requisitos críticos
- No contar la misma muerte o reanimación dos veces.
- No confundir fin de escena con fantasma no rescatado.
- No asumir que el jugador 1 es siempre índice 0 sin probarlo.
- No reiniciar el nivel en esta fase.
- Si no hay hook fiable, documentar el bloqueo; no inventar sustitutos.

## Entregables
`docs/GAME_HOOKS.md` con hooks validados, firma, evidencia, limitaciones y versión. Añade pruebas o herramientas de diagnóstico que puedan mantenerse sin afectar rendimiento.

## Cierre
Informe de qué eventos son fiables y GO/NO-GO para M3. PR sin merge.
