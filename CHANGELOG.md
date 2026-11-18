# Changelog

Los cambios se documentan en español y se agrupan por versión.

## Sin publicar
### Añadido
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
- Pruebas unitarias (135) y CI que compila para `net35` y `netstandard2.0`.
- Informe M0 de escritorio, hooks candidatos (`docs/GAME_HOOKS.md`) y ADR-004 con la revisión del kit.

### Cambiado
- El proyecto pasa a llamarse Coophead.
- La frase de agradecimiento deja de ser un nombre fijo: basta con «gracias».
- Reglas simétricas por defecto; el foco en P2 queda como ajuste opcional a 0.

### Inicial
- Creación inicial de la documentación y planificación del proyecto.
