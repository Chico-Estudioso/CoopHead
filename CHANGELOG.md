# Changelog

Los cambios se documentan en español y se agrupan por versión.

## Sin publicar
### Corregido (20/11/2026, voz de la pareja por Discord)
- La fuente Escritorio escucha todas las salidas de audio activas (`DispositivoEscritorio = todos`), numeradas en el log y en F6: Discord puede sacar la voz por una salida distinta de la predeterminada.
- Se baja el umbral de rechazo interno del reconocedor de Windows para que decida `ConfianzaMinima`.

### Corregido (20/11/2026, falsos negativos de la voz)
- Margen de voz (`MargenVoz`, 1,5 s): una frase dicha a tiempo cuyo resultado llega tarde ya no se descarta.
- La primera orden al ayudante de voz llegaba con un BOM invisible y se perdía («Orden desconocida»).
- El audio del escritorio llegaba con microcortes; ahora se entrega continuo.
- Las líneas del ayudante ya no arrastran retornos de carro.

### Añadido (20/11/2026)
- F6 → Ajustes: retraso del escritorio, margen de voz, botón para probar la voz y lista de las últimas frases oídas (aceptadas, dudosas o no entendidas) con fuente y confianza.

### Corregido (20/11/2026, tras la primera partida real)
- La voz del escritorio fallaba siempre («El método especificado no es compatible»): el flujo de audio ya no rechaza `Seek`.
- El castigo por no dar las gracias caía mientras el rescatado aún reaparecía. El plazo sigue contando desde el parry de reanimación, pero el castigo se aplica cuando el jugador vuelve a la partida. El HUD muestra «sin gracias · castigo al reaparecer».
- `RetrasoEscritorio` baja a 0,3 s por defecto para que un «gracias» dicho justo al hacer el parry cuente.

### Verificado (20/11/2026)
- Partida real en Flower: muertes, rescates, golpes, parrys, súper, daño, límite individual con reinicio y castigos reales funcionan (`docs/pruebas/2026-11-20-partida-flower.md`).

### Añadido (19/11/2026)
- Ventana de Coophead dentro del juego (F6, con ratón): partida en directo, libro de recuerdos con el try final y comparativa P1/P2, totales, ruleta editable con probabilidades actuales, ajustes principales y registro.
- Estadísticas acumulativas: daño infligido, parrys, súper, EX, golpes recibidos, gracias por voz y con tecla, castigos por efecto y castigos dobles, además de las reglas. Libro de recuerdos por nivel en `BepInEx/config/Coophead/estadisticas.json` con intentos, victorias, mejor tiempo, totales y el último intento ganado (`docs/ESTADISTICAS.md`).
- El resumen de fin de nivel incluye daño, parrys y duración.
- Voz desde el escritorio para jugar por Parsec: `FuenteVoz` = `Microfono`, `Escritorio` o `Ambos` (por defecto). Captura WASAPI en bucle con NAudio, solo durante las ventanas, con confianza propia, retraso inicial para ignorar el sonido de reanimación del juego y deduplicación entre fuentes.
- El HUD avisa del límite alcanzado también con el reinicio automático desactivado, y muestra el total de reanimaciones cuando los créditos reducen el contador neto.
- El log indica cada castigo aplicado de verdad.

### Cambiado (19/11/2026)
- `AplicarReglas`, `AplicarEfectos` y `ReinicioAutomatico` pasan a estar activados por defecto tras comprobar el plugin en Cuphead 1.3.4.

### Verificado
- 19/11/2026: el plugin carga en Cuphead 1.3.4 (x64, Unity 2017.4.9f1, Mono CLR 2.0) con BepInEx 5.4.23.5 y resuelve 9 de 9 hooks. Falta verificar su comportamiento en partida.

### Añadido
- Efectos en el juego experimentales (desactivados por defecto): controles invertidos con el mecanismo propio del juego, movimiento lento y dash limitado con restauración exacta del valor original. Los efectos no compatibles se quitan de la ruleta; si los efectos están desactivados, se simulan y el HUD lo indica.
- Reinicio de nivel con `SceneLoader.ReloadLevel()`, diferido al siguiente frame y detrás de `ReinicioAutomatico`.
- Voz experimental (desactivada por defecto): ayudante local `Coophead.Voice.exe` con el reconocedor de Windows y gramática cerrada, protocolo de órdenes fijas probado en el Core, escucha solo durante las ventanas, prueba de micrófono con F9 y sin castigo por fallos técnicos.
- HUD discreto (IMGUI) en una esquina configurable con contadores, créditos, límite de equipo, ventana de agradecimiento, efectos activos y estado de la voz; resumen al terminar el nivel. Texto generado en el Core (`HudFormatter`) y probado.
- Instrumentación de M2: 9 hooks Harmony de solo lectura resueltos por nombre en tiempo de ejecución, con informe `OK`/`FALTA` al arrancar.
- `GameEventTranslator`: traduce los eventos del juego al Core con la misma clave para muerte y rescate, ignora el fin de instancias de nivel anteriores y trata el robo de corazón como vuelta a la partida sin contar.
- Ajustes `Hooks` y `AplicarReglas` (este último desactivado por defecto).
- Plugin BepInEx 5 mínimo (`Coophead.dll`, `net35`): carga la configuración, crea la sesión del Core, registra el entorno del juego y no modifica nada todavía.
- Esquema de configuración `CoopheadSettings` con 50 ajustes en español, rangos y validación (`docs/CONFIGURACION.md`).
- Teclas de respaldo para dar las gracias (F7/F8 por defecto, configurables).
- Artefacto de CI con las DLL del plugin.
- `Coophead.Core`: motor de reglas bilaterales con créditos de ayuda, límite individual y límite de equipo, deduplicación de eventos y resumen de nivel.
- Ruleta de penalizaciones con pesos configurables, no repetición, cooldown, protección tras castigos seguidos, castigo doble compatible y foco opcional en P2.
- Director de equilibrio con fórmula de presión documentada.
- Programador de efectos que garantiza la restauración.
- Ventanas de agradecimiento y reconocimiento de la frase «gracias» para ambos jugadores, con respaldo manual.
- Pruebas unitarias (154) y CI que compila para `net35` y `netstandard2.0`.
- Informe M0 de escritorio, hooks candidatos (`docs/GAME_HOOKS.md`) y ADR-004 con la revisión del kit.

### Cambiado
- El proyecto pasa a llamarse Coophead.
- La frase de agradecimiento deja de ser un nombre fijo: basta con «gracias».
- Reglas simétricas por defecto; el foco en P2 queda como ajuste opcional a 0.

### Inicial
- Creación inicial de la documentación y planificación del proyecto.
