# Hooks del juego

> **Ningún hook de esta lista está verificado contra la instalación local del usuario.** Los nombres proceden de leer código comunitario (ver «Origen»). M2 debe confirmarlos uno a uno con los ensamblados exactos de la copia de pruebas antes de activar reglas que reinicien o castiguen.

Estados: `Candidato` (visto en código comunitario), `Presente (1.3.4)` (el tipo y el método existen con esa firma en la build del usuario; falta comprobar cuándo y cómo se disparan), `Verificado` (comportamiento comprobado en partida con log), `Descartado`.

Prueba de carga del 19/11/2026 en Cuphead 1.3.4: los 9 hooks instrumentados se resolvieron e instalaron (`docs/pruebas/2026-11-19-carga-1.3.4.md`).

Partida real del 20/11/2026 (Flower, 9 intentos): los hooks marcados `Verificado (1.3.4)` se dispararon una vez por evento y con el jugador correcto (`docs/pruebas/2026-11-20-partida-flower.md`). Sin ver todavía: `Level.OnWin` (no hubo victoria), `ReviveOutOfFrame` (no hubo robo de corazón) y `OnEx`.

**Orden observado**: el parry (`OnParryPrePause`) llega más de 2 s antes de que el juego confirme la reaparición (`PlayerStatsManager.OnRevive`). El plazo para dar las gracias cuenta desde el parry, pero si falta el agradecimiento el castigo se aplica al confirmarse la reaparición.

## Origen de la evidencia
- **Cuphead 3-Player Co-op** (Meloon33, licencia MIT): plugin BepInEx 5 que parchea estos métodos con Harmony. Es la referencia principal.
- **Cuphead.DebugMod** (DemoJameson, sin licencia publicada): plugin BepInEx 5 con target `net35`; configuraciones distintas para Cuphead 1.0–1.1, 1.2 y 1.3+.
- **CupHeads** (Germanized, sin licencia publicada): incluye una descompilación de `Assembly-CSharp`. Solo se ha leído para entender el ciclo de vida del fantasma. **No se copia código de este repositorio.**

Los proyectos sin licencia se usan solo como lectura; nada de su código entra en Coophead.

## Identificación de jugadores
| Elemento | Detalle | Estado |
|---|---|---|
| `PlayerId` (enum) | `PlayerOne`, `PlayerTwo`, `Any`, `None` | `Verificado (1.3.4)` |
| `PlayerManager.GetPlayer(PlayerId)` | Devuelve `AbstractPlayerController` | `Verificado (1.3.4)` |
| `AbstractPlayerController.id` | `PlayerId` del jugador | `Verificado (1.3.4)` |
| `PlayerStatsManager.basePlayer` | Controlador dueño de las estadísticas | `Verificado (1.3.4)` |

Mapeo previsto: `PlayerId.PlayerOne` → `PlayerSlot.PlayerOne`, `PlayerId.PlayerTwo` → `PlayerSlot.PlayerTwo`; cualquier otro valor se descarta. Hay que comprobar en M2 que el índice no cambia al reconectar mandos.

## Ciclo de nivel
| Hook | Uso en Coophead | Estado |
|---|---|---|
| `Level.Awake` (postfix) | `StartLevel`: nuevo intento, contadores a cero | `Verificado (1.3.4)` |
| `Level.OnWin` (postfix) | `EndLevel(Won)` | `Presente (1.3.4)` |
| `Level._OnLose` | `EndLevel(Lost)`; ambos muertos | `Verificado (1.3.4)` |
| `Level.OnDestroy` | `EndLevel(Exited)` | `Verificado (1.3.4)` |
| `SceneLoader.ReloadLevel()` | Vía de reinicio del nivel | `Verificado (1.3.4)` |
| `LevelGameOverGUI.Retry` | Reintento desde la pantalla de derrota | Candidato |
| `Level.LevelType`, `Level.IsDicePalace`, `Level.IsChessBoss`, `PlatformingLevel` | Distinguir jefes, plataformas y tutorial | Candidato |

## Muerte, fantasma y reanimación
| Hook | Firma observada | Uso en Coophead | Estado |
|---|---|---|---|
| `Level.OnPlayerDeath` (privado) | `(DeathEvent e)`, `e.playerId` | Diagnóstico | `Verificado (1.3.4)` |
| `PlayerDeathEffect.Start` | Aparición del fantasma; campo privado `playerId` | `OnPlayerDied` con clave por instancia | `Verificado (1.3.4)` |
| `PlayerDeathParrySwitch.OnParryPrePause` | `(AbstractPlayerController player)`: quien hace parry | `OnPlayerRevived` con rescatador = `player.id` y rescatado = `playerId` del `PlayerDeathEffect` padre | `Verificado (1.3.4)` |
| `PlayerDeathEffect.OnParrySwitch` | Marca `exiting = true` y lanza la animación de rescate | Alternativa para confirmar el rescate por parry | Candidato |
| `PlayerDeathEffect.ReviveOutOfFrame` | Robo de corazón fuera de cámara | **No contar como rescate** (ADR-004) | `Presente (1.3.4)` |
| `PlayerStatsManager.OnRevive` | Se dispara en ambos tipos de reanimación | Marca vivo sin contar | `Verificado (1.3.4)` |
| `PlayerStatsManager.PartnerCanSteal` | `Health > 1` | Diagnóstico | Candidato |

### Lo que dice la evidencia sobre el fantasma
- El fantasma sube a una velocidad que depende del número de muertes y **no tiene temporizador de caducidad**.
- Fuera de cámara, el jugador muerto puede pulsar un botón para robar un corazón a su pareja si esta tiene más de 1.
- Si ambos están muertos, el nivel se pierde.
- Por tanto, «fantasma no rescatado» no es un evento del juego: como mucho, «fantasma fuera de cámara». Queda fuera de alcance por ahora.

## Estadísticas de combate
| Hook | Lectura | Uso | Estado |
|---|---|---|---|
| `DamageDealer.DealDamage` (postfix) | `__result` (daño) y `PlayerId`, o `DamageDealer.lastPlayer` | Daño infligido | `Verificado (1.3.4)` |
| `PlayerStatsManager.OnParry` | `basePlayer.id` | Parrys | `Verificado (1.3.4)` |
| `PlayerStatsManager.OnSuper` / `OnEx` | `basePlayer.id` | Súper y EX | `Verificado (1.3.4)` OnSuper; OnEx sin ver aún |
| `PlayerStatsManager.TakeDamage` (prefijo + postfix) | `Health` antes y después | Golpes que quitan vida | `Verificado (1.3.4)` |

Origen: Cuphead 3-Player Co-op (MIT). Solo leen datos; el de daño no escribe en el log porque se llama con cada impacto.

## Efectos de la ruleta (experimental, `AplicarEfectos = false` por defecto)
Implementados en `src/Coophead.Plugin/GameIntegration/GameEffectsApplier.cs`, resueltos por nombre:

| Efecto | Miembros usados | Restauración | Estado |
|---|---|---|---|
| Controles invertidos | `PlayerManager.GetPlayer(PlayerId)` → `stats` → `PlayerStatsManager.ReverseTime` | Se pone a 0. Además el propio juego lo descuenta, así que caduca solo aunque fallara la restauración | `Verificado (1.3.4)` |
| Movimiento lento | `LevelPlayerController.motor` → `LevelPlayerMotor.properties.moveSpeed` (490 en la referencia) | Valor original exacto, guardado por objeto y con contador | `Verificado (1.3.4)` |
| Dash limitado | `LevelPlayerMotor.properties.dashTime` (0,3 s en la referencia) | Valor original exacto | `Verificado (1.3.4)` |
| Disparo lento | Sin vía segura identificada (la cadencia depende de cada arma) | — | No compatible: se quita de la ruleta al activar `AplicarEfectos` |

- `ReverseTime` es el mecanismo de inversión que ya usa el juego: en la referencia solo invierte el eje horizontal al caminar dentro del nivel, no menús ni pausa.
- En niveles de avión los efectos no se aplican (no hay `LevelPlayerMotor`) y se registra un aviso.
- Si `AplicarEfectos` está desactivado, la ruleta funciona igual pero los efectos se **simulan**: el HUD los muestra con «(simulado)».

## Reinicio de nivel
Con `ReinicioAutomatico = true`, al alcanzar un límite se llama a `SceneLoader.ReloadLevel()` en el siguiente `Update` (nunca dentro de un hook). Si el método no existe, solo se avisa. Estado: `Verificado (1.3.4)` (partida del 20/11/2026: al llegar P2 a 3 reanimaciones el nivel se reinició).

## Instrumentación implementada
`src/Coophead.Plugin/GameIntegration/GameHooks.cs` instala estos hooks al arrancar, **resolviéndolos por nombre** (Coophead no compila contra `Assembly-CSharp`):

| Hook instalado | Lectura | Traducción (`GameEventTranslator`) |
|---|---|---|
| `Level.Awake` | tipo e instancia del nivel | `LevelStarted` |
| `Level.OnWin`, `Level._OnLose`, `Level.OnDestroy` | instancia del nivel | `LevelEnded` (se ignora si es de una instancia anterior) |
| `Level.OnPlayerDeath` | `playerId` del evento | Solo diagnóstico |
| `PlayerDeathEffect.Start` | `playerId` e instancia del fantasma | `OnPlayerDied` con clave `ghost:<id>` |
| `PlayerDeathParrySwitch.OnParryPrePause` | `id` del jugador que hace parry; fantasma padre | `OnPlayerRevived` con la misma clave `ghost:<id>` |
| `PlayerDeathEffect.ReviveOutOfFrame` | `playerId` | Solo diagnóstico (robo de corazón, no cuenta) |
| `PlayerStatsManager.OnRevive` | `basePlayer.id` | Marca vivo sin contar si el Core aún lo tenía por muerto |

- Todos son **postfix de solo lectura** envueltos en `try/catch`: no cambian argumentos, retornos ni estado del juego.
- Si un tipo o método no existe en la versión instalada, ese hook queda marcado `FALTA` en el log y el resto sigue funcionando.
- Con `AplicarReglas = false` (por defecto) los eventos solo se escriben en el log con número de secuencia `[#n]`.

## Cómo verificar en la copia de pruebas
1. Instala el plugin (ver `docs/INSTALACION_LOCAL.md`) con `Diagnostico = true` y `AplicarReglas = false`.
2. Arranca el juego y busca en `BepInEx/LogOutput.log` las líneas `Hook OK …` / `Hook FALTA …` y el resumen «Hooks instalados: X de 9».
3. Juega un jefe en cooperativo y ejecuta esta matriz, anotando los números `[#n]` que aparecen:

| Paso | Esperado en el log |
|---|---|
| Entrar al nivel | `Nivel iniciado: <TipoDeNivel>` una sola vez |
| Muere P1 | `Muerte notificada por el nivel: P1` y `Fantasma <id> de P1` |
| P2 hace parry al corazón de P1 | `Rescate por parry del fantasma <id>: P2 rescata a P1` con el mismo `<id>`, seguido de `Reanimación confirmada por el juego: P1` |
| Lo mismo al revés | Igual con P1 y P2 intercambiados |
| El fantasma sube fuera de pantalla y roba un corazón | `Robo de corazón …` y `Reanimación confirmada por el juego` (sin «Rescate por parry») |
| Mueren los dos | Dos fantasmas y `Nivel terminado (Lost)` |
| Retry tras perder | `Nivel terminado (Exited)` del anterior o «instancia anterior… se ignora», y un `Nivel iniciado` nuevo |
| Ganar | `Nivel terminado (Won)` |
| Salir al mapa desde la pausa | `Nivel terminado (Exited)` |
| Repetirlo en un nivel de avión y en uno de plataformas | Mismos eventos, o anotar las diferencias |

4. Si todo coincide, cambia el estado de cada hook a `Verificado` en este documento, indicando la versión del juego, y activa `AplicarReglas`.
5. Con `AplicarReglas` activo, prueba los efectos uno a uno: deja solo uno activado en la ruleta, activa `AplicarEfectos`, deja pasar una ventana sin dar las gracias y comprueba que el efecto se nota, que dura lo indicado y que al terminar todo vuelve a la normalidad. Repite muriendo durante el efecto y saliendo al mapa con el efecto activo.
6. Prueba el reinicio con `LimiteReanimacionesEquipo = 1` y `ReinicioAutomatico = true` en un jefe y en un nivel de plataformas.

## Plan de verificación (M2)
1. Parches de solo registro (postfix sin modificar valores ni retornos), con número de sesión y clave por evento. **Hecho.**
2. Matriz manual: muere P1 y le rescata P2; al revés; ambos alternan; robo de corazón; ambos muertos; retry; salir al mapa; cutscene.
3. Para cada hook: confirmar que se aplica, cuántas veces se dispara y con qué jugador.
4. Solo entonces marcar como `Verificado` y activar las reglas dependientes.
