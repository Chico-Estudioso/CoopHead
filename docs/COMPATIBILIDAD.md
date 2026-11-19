# Matriz de compatibilidad

| Componente | Versión | Estado | Evidencia | Notas |
|---|---|---|---|---|
| Cuphead | 1.3.4 | Compatible probado (carga) | Log del 19/11/2026 | Falta comprobar el comportamiento de los hooks en partida. DLC instalado: sin comprobar. |
| Arquitectura | x64 | Compatible probado | Log: «System platform: Bits64, Windows» | |
| Unity | 2017.4.9f1 | Compatible probado | Log: «Running under Unity v2017.4.9» | Coincide con `UnityEngine.Modules` 2017.4.9 usado para compilar. |
| Backend de scripting | Mono, CLR 2.0.50727 | Compatible probado | Log: «CLR runtime version: 2.0.50727.1433» | Perfil .NET 3.5. |
| BepInEx | 5.4.23.5 | Compatible probado (carga) | Log del 19/11/2026 | El plugin compila contra 5.4.21; la API es compatible. |
| Harmony | HarmonyX incluido en BepInEx 5.4.23.5 | Compatible probado | 9 parches instalados sin error | No se añade copia propia. |
| Target .NET del plugin | `net35` | Compatible probado | El plugin y `Coophead.Core` cargan en CLR 2.0 | |
| Coophead.Core | 0.1.0 | Compatible probado (solo pruebas unitarias) | Pruebas en CI | No probado dentro del juego. |
| Coophead (plugin) | 0.1.0 | Compatible probado (carga) | Log: «Hooks instalados: 9 de 9» | Reglas en modo solo registro; comportamiento en partida pendiente. |
| Efectos en el juego | Controles invertidos, movimiento lento, dash limitado | No probado | Miembros de código comunitario | Disparo lento: no compatible por ahora. |
| Reconocedor local | Reconocedor de escritorio de Windows (`System.Speech`) | No probado | El ayudante compila para .NET Framework 4.8 | Experimental y desactivado por defecto. Requiere el paquete de reconocimiento de voz en español de Windows. |

Estados permitidos: `No probado`, `Investigando`, `Compatible probado`, `Incompatible`, `Bloqueado`, `Deshabilitado por seguridad`.

No se declara compatibilidad por compilar: hace falta probar en la copia local de Cuphead.
