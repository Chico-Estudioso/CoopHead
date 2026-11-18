# ADR-001 — Plugin BepInEx

Estado: Propuesto. M0 de escritorio favorable (BepInEx 5.4.x, `net35`); pendiente de confirmar en la copia local.

## Contexto
Cuphead es un juego Unity y la comunidad utiliza plugins BepInEx. La instalación local del usuario puede diferir de una instalación estándar de Steam/Epic.

## Decisión provisional
Investigar BepInEx 5 como mecanismo principal. Verificar primero el runtime/arquitectura real de la copia local y la build compatible.

## Consecuencias
- El plugin será C#.
- Los ensamblados del juego se referencian localmente y no se suben a Git.
- Mortar será una opción de gestión; no una dependencia de ejecución.
- Si BepInEx no puede cargar en la build concreta, detenerse y documentar la causa antes de cambiar de framework.
