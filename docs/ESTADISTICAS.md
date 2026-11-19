# Estadísticas y libro de recuerdos

Coophead apunta lo que pasa en cada intento de cada nivel y lo acumula en un **libro de recuerdos** que se conserva entre partidas.

## Dónde se guarda
`BepInEx/config/Coophead/estadisticas.json`, junto a una copia `estadisticas.json.bak` del guardado anterior. Es un archivo de texto (JSON) que no toca el guardado del juego. Para empezar de cero, borra el archivo con el juego cerrado. Si el archivo no se puede leer, se aparta como `estadisticas.json.ilegible-<fecha>` y se empieza uno nuevo, sin borrar nada.

Solo se registra con `AplicarReglas` activado. Entrar y salir de un nivel sin que pase nada (por ejemplo, la casa del principio) no cuenta como intento.

## Qué se cuenta por jugador
| Estadística | Origen |
|---|---|
| Daño infligido | Hook `DamageDealer.DealDamage` (daño hecho a enemigos) |
| Parrys | Hook `PlayerStatsManager.OnParry` |
| Súper y EX | Hooks `PlayerStatsManager.OnSuper` y `OnEx` |
| Golpes recibidos | Hook `PlayerStatsManager.TakeDamage`, solo si la vida baja de verdad |
| Muertes, rescates dados y reanimaciones recibidas (totales) | Reglas de Coophead |
| Gracias dadas (por voz y con tecla) y gracias olvidadas | Ventanas de agradecimiento |
| Castigos recibidos, por efecto, y castigos dobles | Ruleta |
| Créditos de ayuda ganados y usados | Reglas de Coophead |

## Qué se guarda por nivel
- Intentos, victorias, derrotas, reinicios del mod y salidas.
- Tiempo jugado total y **mejor tiempo de victoria**.
- Fechas de la primera vez, la última vez y la primera victoria.
- Totales acumulados de P1 y P2.
- El **último intento** y el **último intento ganado** («el try final»), con sus estadísticas por jugador.

El nombre del nivel se obtiene del tipo que usa el juego («FlowerLevel» → «Flower»).

## Dónde se ve
- **Durante la partida**: el HUD de siempre y, al terminar el nivel, un resumen con daño, parrys, muertes, rescates, gracias y castigos de cada uno.
- **En la ventana de Coophead** (ver `docs/CONFIGURACION.md`): el libro completo, los totales y la comparativa P1 contra P2, con quién lleva más en cada estadística.

## Limitaciones
- Los hooks de estadísticas son candidatos sacados de código comunitario; si alguno no existe en tu versión, aparece como `FALTA` en el log y esa estadística se queda a cero.
- El daño se mide en las unidades internas del juego.
- Con el fantasma fuera de pantalla, el robo de corazón no cuenta como rescate (ADR-004).
