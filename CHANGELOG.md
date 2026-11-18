# Changelog

Los cambios se documentan en español y se agrupan por versión.

## Sin publicar
### Añadido
- `Coophead.Core`: motor de reglas bilaterales con créditos de ayuda, límite individual y límite de equipo, deduplicación de eventos y resumen de nivel.
- Ruleta de penalizaciones con pesos configurables, no repetición, cooldown, protección tras castigos seguidos, castigo doble compatible y foco opcional en P2.
- Director de equilibrio con fórmula de presión documentada.
- Programador de efectos que garantiza la restauración.
- Ventanas de agradecimiento y reconocimiento de la frase «gracias» para ambos jugadores, con respaldo manual.
- 113 pruebas unitarias y CI que compila para `net35` y `netstandard2.0`.
- Informe M0 de escritorio, hooks candidatos (`docs/GAME_HOOKS.md`) y ADR-004 con la revisión del kit.

### Cambiado
- El proyecto pasa a llamarse Coophead.
- La frase de agradecimiento deja de ser un nombre fijo: basta con «gracias».
- Reglas simétricas por defecto; el foco en P2 queda como ajuste opcional a 0.

### Inicial
- Creación inicial de la documentación y planificación del proyecto.
