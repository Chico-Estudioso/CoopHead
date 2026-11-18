# Hooks del juego

> **Ningún hook de esta lista está verificado contra la instalación local del usuario.** Los nombres proceden de leer código comunitario (ver «Origen»). M2 debe confirmarlos uno a uno con los ensamblados exactos de la copia de pruebas antes de activar reglas que reinicien o castiguen.

Estados: `Candidato` (visto en código comunitario), `Verificado` (comprobado en la build del usuario con log), `Descartado`.

## Origen de la evidencia
- **Cuphead 3-Player Co-op** (Meloon33, licencia MIT): plugin BepInEx 5 que parchea estos métodos con Harmony. Es la referencia principal.
- **Cuphead.DebugMod** (DemoJameson, sin licencia publicada): plugin BepInEx 5 con target `net35`; configuraciones distintas para Cuphead 1.0–1.1, 1.2 y 1.3+.
- **CupHeads** (Germanized, sin licencia publicada): incluye una descompilación de `Assembly-CSharp`. Solo se ha leído para entender el ciclo de vida del fantasma. **No se copia código de este repositorio.**

Los proyectos sin licencia se usan solo como lectura; nada de su código entra en Coophead.

## Identificación de jugadores
| Elemento | Detalle | Estado |
|---|---|---|
| `PlayerId` (enum) | `PlayerOne`, `PlayerTwo`, `Any`, `None` | Candidato |
| `PlayerManager.GetPlayer(PlayerId)` | Devuelve `AbstractPlayerController` | Candidato |
| `AbstractPlayerController.id` | `PlayerId` del jugador | Candidato |
| `PlayerStatsManager.basePlayer` | Controlador dueño de las estadísticas | Candidato |

Mapeo previsto: `PlayerId.PlayerOne` → `PlayerSlot.PlayerOne`, `PlayerId.PlayerTwo` → `PlayerSlot.PlayerTwo`; cualquier otro valor se descarta. Hay que comprobar en M2 que el índice no cambia al reconectar mandos.

## Ciclo de nivel
| Hook | Uso en Coophead | Estado |
|---|---|---|
| `Level.Awake` (postfix) | `StartLevel`: nuevo intento, contadores a cero | Candidato |
| `Level.OnWin` (postfix) | `EndLevel(Won)` | Candidato |
| `Level._OnLose` | `EndLevel(Lost)`; ambos muertos | Candidato |
| `SceneLoader.ReloadLevel()` | Vía de reinicio del nivel | Candidato |
| `LevelGameOverGUI.Retry` | Reintento desde la pantalla de derrota | Candidato |
| `Level.LevelType`, `Level.IsDicePalace`, `Level.IsChessBoss`, `PlatformingLevel` | Distinguir jefes, plataformas y tutorial | Candidato |

## Muerte, fantasma y reanimación
| Hook | Firma observada | Uso en Coophead | Estado |
|---|---|---|---|
| `Level.OnPlayerDeath` (privado) | `(PlayerStatsManager.DeathEvent e)`, `e.playerId` | `OnPlayerDied` | Candidato |
| `PlayerDeathEffect.Start` / `OnDestroy` | Ciclo de vida del fantasma; campo privado `playerId` | Clave de deduplicación (instancia) | Candidato |
| `PlayerDeathParrySwitch.OnParryPrePause` | `(AbstractPlayerController player)`: quien hace parry | `OnPlayerRevived` con rescatador = `player.id` y rescatado = `playerId` del `PlayerDeathEffect` padre | Candidato |
| `PlayerDeathEffect.OnParrySwitch` | Marca `exiting = true` y lanza la animación de rescate | Alternativa para confirmar el rescate por parry | Candidato |
| `PlayerDeathEffect.ReviveOutOfFrame` | Robo de corazón fuera de cámara | **No contar como rescate** (ADR-004) | Candidato |
| `PlayerStatsManager.OnRevive` | Se dispara en ambos tipos de reanimación | No usar sola: no distingue parry de robo | Candidato |
| `PlayerStatsManager.PartnerCanSteal` | `Health > 1` | Diagnóstico | Candidato |

### Lo que dice la evidencia sobre el fantasma
- El fantasma sube a una velocidad que depende del número de muertes y **no tiene temporizador de caducidad**.
- Fuera de cámara, el jugador muerto puede pulsar un botón para robar un corazón a su pareja si esta tiene más de 1.
- Si ambos están muertos, el nivel se pierde.
- Por tanto, «fantasma no rescatado» no es un evento del juego: como mucho, «fantasma fuera de cámara». Queda fuera de alcance por ahora.

## Efectos de la ruleta
Pendiente de auditoría en M4: velocidad de movimiento, inversión del eje horizontal sin afectar a menús ni pausa, cadencia de disparo y dash. Ningún efecto se activará sin hook verificado que permita restaurar el valor original.

## Plan de verificación (M2)
1. Parches de solo registro (postfix sin modificar valores ni retornos), con número de sesión y clave por evento.
2. Matriz manual: muere P1 y le rescata P2; al revés; ambos alternan; robo de corazón; ambos muertos; retry; salir al mapa; cutscene.
3. Para cada hook: confirmar que se aplica, cuántas veces se dispara y con qué jugador.
4. Solo entonces marcar como `Verificado` y activar las reglas dependientes.
