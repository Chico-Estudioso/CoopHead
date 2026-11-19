# ADR-001 — Plugin BepInEx

Estado: **Aceptado** (19/11/2026). Confirmado en la instalación del usuario: Cuphead 1.3.4 x64, Unity 2017.4.9f1, Mono CLR 2.0, BepInEx 5.4.23.5; el plugin `net35` carga y resuelve sus hooks.

## Contexto
Cuphead es un juego Unity y la comunidad utiliza plugins BepInEx. La instalación local del usuario puede diferir de una instalación estándar de Steam/Epic.

## Decisión provisional
Investigar BepInEx 5 como mecanismo principal. Verificar primero el runtime/arquitectura real de la copia local y la build compatible.

## Consecuencias
- El plugin será C#.
- Los ensamblados del juego se referencian localmente y no se suben a Git.
- Mortar será una opción de gestión; no una dependencia de ejecución.
- Si BepInEx no puede cargar en la build concreta, detenerse y documentar la causa antes de cambiar de framework.
