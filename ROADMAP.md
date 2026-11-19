# Roadmap — Coophead

Estado a 18/11/2026. Leyenda: ✅ hecho · 🟡 parcial · ⬜ pendiente.

## M0 — Investigación y entorno local ✅
Investigación de escritorio (`docs/INFORME_M0.md`) confirmada con el log del usuario: Cuphead 1.3.4 x64, Unity 2017.4.9f1, Mono CLR 2.0, BepInEx 5.4.23.5. Mortar no evaluado.

- Identificar `Cuphead.exe`, versión, arquitectura, runtime Unity/Mono y ensamblados.
- Evaluar Mortar con instalación desde carpeta.
- Evaluar instalación manual de BepInEx 5.
- Auditar proyecto comunitario de co-op/revive como referencia, sin copiar código automáticamente.
- Identificar métodos reales de muerte, ghost, revive, nivel y jugador.
- Validar opciones de reconocimiento de voz local y requisitos/licencias.
- Resultado: informe técnico y decisión GO/NO-GO.

## M1 — Plugin mínimo ✅
BepInEx carga `Coophead 0.1.0` en Cuphead 1.3.4 y registra versión, Unity y arquitectura (`docs/pruebas/2026-11-19-carga-1.3.4.md`).

- Crear solución C# con target framework verificado.
- Referenciar BepInEx/Harmony y ensamblados de juego de forma local.
- Crear plugin que cargue y escriba logs.
- Crear build reproducible sin incluir DLLs propietarias.
- Resultado: plugin detectado por BepInEx en copia de pruebas.

## M2 — Auditoría de eventos de jugador 🟡
Instrumentación hecha y **los 9 hooks existen en 1.3.4**: resueltos por nombre, postfix de solo lectura, traducción probada en el Core y procedimiento de verificación en `docs/GAME_HOOKS.md`. Falta ejecutar la matriz manual en la copia de pruebas.

- Identificar Player 1 y Player 2 de forma fiable.
- Detectar muerte, aparición del fantasma, reanimación exitosa y fallo de rescate.
- Detectar cambio/fin de nivel y reinicio.
- Implementar telemetría de depuración no invasiva.
- Resultado: eventos comprobados en partidas locales.

## M3 — Motor de reglas bilateral 🟡
Lógica implementada y probada en `Coophead.Core` (incluye el límite de equipo de ADR-004), conectada a los hooks candidatos y con reinicio por `SceneLoader.ReloadLevel()` detrás de `ReinicioAutomatico`. Falta verificarlo en el juego.

- Contadores por jugador y nivel.
- Límite configurable de reanimaciones recibidas.
- Créditos de ayuda: rescatar a la pareja compensa una reanimación recibida propia; máximo un crédito guardado por nivel.
- Aplicar créditos antes de incrementar el contador en la próxima reanimación recibida.
- Reinicio de nivel al alcanzar el umbral configurado.
- Límite de equipo: 6 reanimaciones totales por nivel.
- Resultado: reglas verificadas con ambos jugadores.

## M4 — Ruleta y equilibrio adaptativo 🟡
Ruleta, director de equilibrio y programador de efectos implementados y probados. Efectos en el juego experimentales (controles invertidos, movimiento lento y dash limitado) detrás de `AplicarEfectos`; «Disparo lento» sin vía segura todavía. Falta probarlos en la copia local.

- Movimiento lento, controles invertidos, disparo lento, sin penalización, dash limitado y castigo doble.
- Duración inicial de cuatro segundos.
- Pesos configurables, cooldown, protección contra repetición.
- Balance Director con presión por jugador y protección cuando hay demasiada carga.
- Resultado: ninguna combinación deja al jugador bloqueado; todos los efectos se restauran.

## M5 — Reconocimiento de voz local 🟡
Ventanas, coincidencia de frase y respaldo manual implementados. Motor elegido: reconocedor de Windows en un proceso auxiliar con gramática cerrada (experimental, desactivado por defecto), con prueba de micrófono. Falta probarlo en un equipo real y medir falsos positivos y negativos.

- Confirmar requisitos de privacidad y tolerancia a modelos locales.
- Selección de micrófono y prueba de entrada.
- Frase configurable: basta con «gracias» («Gracias, Cari», «Gracias, amor»…), válida para ambos jugadores.
- Ventana de tres segundos tras una reanimación.
- Sin nube, sin APIs remotas, sin almacenamiento de audio.
- Fallback manual configurable si la voz no está disponible.
- Resultado: reconocimiento probado en el equipo real, falsos positivos/negativos documentados.

## M6 — Configuración y HUD 🟡
Ventana dentro del juego (F11) con libro de recuerdos, ruleta, ajustes y registro. Esquema completo de ajustes en BepInEx (`docs/CONFIGURACION.md`), validado y probado. HUD IMGUI con contadores, créditos, ventana, efectos y estado de la voz, más resumen al terminar el nivel. Falta comprobar en el juego que no tapa nada importante.

- Configuración BepInEx y/o interfaz propia, según viabilidad.
- HUD de contadores, créditos, temporizador y estado de voz.
- Resumen al terminar nivel.
- Resultado: ajustes accesibles y comprensibles.

## Estadísticas y libro de recuerdos 🟡
Estadísticas por intento y libro acumulado por nivel, guardado entre partidas (`docs/ESTADISTICAS.md`). Faltan probar los hooks de combate en el juego.

## M7 — Integración y distribución ⬜
- Pruebas de regresión, niveles diferentes y cambios de escena.
- Empaquetado del plugin y documentación de instalación/desinstalación.
- Revisión de licencias de dependencias.
- Release candidate y revisión del usuario.
- Resultado: primera versión estable.

## M8 — Mantenimiento ⬜
- Auditoría de cambios del juego y BepInEx.
- Revisión de hooks afectados.
- Pruebas de regresión antes de declarar compatibilidad.
