# Coophead

Plugin de reglas personalizadas para el cooperativo local de dos jugadores de Cuphead, pensado para BepInEx 5.

## Qué hace
- Contadores por jugador y nivel: muertes, rescates dados y recibidos.
- Créditos de ayuda bilaterales: rescatar a tu pareja compensa tus propias reanimaciones.
- Límite individual (3) y límite de equipo (6) de reanimaciones por nivel, con reinicio configurable.
- Ventana de agradecimiento de 3 s tras cada rescate: basta con decir «gracias» («Gracias, Cari», «Gracias, amor»…), por voz local o con una tecla de respaldo.
- Ruleta de penalizaciones temporales de 4 s: movimiento lento, controles invertidos, disparo lento, dash limitado, castigo doble o sin penalización.
- Director de equilibrio transparente que protege a quien acumula más presión.

Reglas completas en [`docs/REGLAS_DE_JUEGO.md`](docs/REGLAS_DE_JUEGO.md).

## Estado
| Fase | Estado |
|---|---|
| M0 Investigación | Hecha y confirmada en Cuphead 1.3.4 x64 ([informe](docs/INFORME_M0.md)) |
| Lógica de reglas, ruleta, equilibrio y agradecimiento | Implementada en `Coophead.Core` con pruebas unitarias |
| M1 Plugin BepInEx mínimo | **Carga en Cuphead 1.3.4** con BepInEx 5.4.23.5 |
| M2 Hooks del juego | Los 9 hooks se instalan en 1.3.4; **falta comprobar en partida que se disparan bien** |
| HUD y resumen de nivel | Listo; **no probado en el juego** |
| Voz local | Experimental, desactivada por defecto; **no probada en un equipo real** |
| Efectos de la ruleta y reinicio | Experimentales y desactivados por defecto; **no probados en el juego** |

Hasta ahora solo se ha comprobado la carga en Cuphead 1.3.4, no el comportamiento en partida. No se debe asumir compatibilidad hasta verificar los hooks en la copia local (ver [`docs/GAME_HOOKS.md`](docs/GAME_HOOKS.md)).

## Estructura
```
src/Coophead.Core/         Lógica pura (net35 + netstandard2.0), sin Unity ni Cuphead
src/Coophead.Plugin/       Plugin BepInEx 5 (net35): configuración, hooks, HUD y voz
src/Coophead.Voice/        Ayudante de voz local para Windows (net48)
tests/Coophead.Core.Tests/ Pruebas unitarias (xUnit, net8.0)
docs/                      Reglas, arquitectura, compatibilidad, hooks y decisiones (ADR)
prompts/                   Guías de trabajo por fase
```

## Compilar y probar
Requiere el SDK de .NET 8.

```bash
dotnet build Coophead.sln -c Release
```

```bash
dotnet test Coophead.sln -c Release
```

## Instalación en el juego
Solo en una **copia de pruebas** de Cuphead con BepInEx 5.4.x: copia `Coophead.dll` y `Coophead.Core.dll` en `BepInEx/plugins/Coophead/`. La CI publica ambas DLL como artefacto (`Coophead-plugin-sin-probar`) en cada ejecución. Detalles en [`docs/INSTALACION_LOCAL.md`](docs/INSTALACION_LOCAL.md) y ajustes en [`docs/CONFIGURACION.md`](docs/CONFIGURACION.md).

En esta versión el plugin no modifica el juego: observa inicio y fin de nivel, muertes y rescates y los escribe en el log. Las reglas solo se aplican si activas `AplicarReglas`, y los efectos de la ruleta siguen desactivados. El repositorio nunca incluye archivos del juego.

## Desarrollo
Instrucciones para Claude Code en [`CLAUDE.md`](CLAUDE.md) y [`AGENTS.md`](AGENTS.md); fases en [`ROADMAP.md`](ROADMAP.md).
