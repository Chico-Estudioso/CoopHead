# Matriz de compatibilidad

| Componente | Versión | Estado | Evidencia | Notas |
|---|---|---|---|---|
| Cuphead | Pendiente | No probado | — | Hay que detectarla en el equipo del usuario. La comunidad distingue 1.0–1.1, 1.2 y 1.3+ (DLC). |
| Arquitectura | x64 en 1.3+, x86 en versiones anteriores | Investigando | DebugMod empaqueta BepInEx x64 para 1.3+ y x86 para el resto | Confirmar con el ejecutable local. |
| Unity | 2017.4.9 (1.2+), 5.6.2 (1.0–1.1) | Investigando | Referencias `UnityEngine.Modules` de DebugMod | Confirmar en `Cuphead_Data`. |
| Backend de scripting | Mono | Investigando | Existen plugins BepInEx 5 (Mono) funcionando | IL2CPP no está soportado por BepInEx 5 estable. |
| BepInEx | 5.4.x | Investigando | 3-Player Co-op exige BepInEx 5.4.x x64; DebugMod usa `BepInEx.Core` 5.* | No instalado aún. |
| Harmony | HarmonyX incluido en BepInEx 5.4 | Investigando | `0Harmony.dll` en `BepInEx/core` | No añadir una copia propia. |
| Target .NET del plugin | `net35` | Investigando | DebugMod y 3-Player Co-op compilan para `net35` | `Coophead.Core` ya compila para `net35`. |
| Coophead.Core | 0.1.0 | Compatible probado (solo pruebas unitarias) | Pruebas en CI | No probado dentro del juego. |
| Coophead (plugin) | 0.1.0 | No probado | Compila contra BepInEx.Core 5.4.21 y UnityEngine.Modules 2017.4.9 | Falta comprobar la carga en la copia de pruebas. |
| Reconocedor local | Reconocedor de escritorio de Windows (`System.Speech`) | No probado | El ayudante compila para .NET Framework 4.8 | Experimental y desactivado por defecto. Requiere el paquete de reconocimiento de voz en español de Windows. |

Estados permitidos: `No probado`, `Investigando`, `Compatible probado`, `Incompatible`, `Bloqueado`, `Deshabilitado por seguridad`.

No se declara compatibilidad por compilar: hace falta probar en la copia local de Cuphead.
