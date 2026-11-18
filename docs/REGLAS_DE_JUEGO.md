# Reglas de juego

Reglas implementadas en `Coophead.Core`. Las decisiones que cambian el kit original están en `docs/decisions/ADR-004-REVISION-DEL-KIT.md`. Todos los valores son configurables.

## Contadores por nivel y jugador
- `Deaths`: muertes detectadas.
- `RevivesReceived`: reanimaciones recibidas **netas**, tras consumir créditos propios y descontar recompensas por rescatar. Es lo que se compara con el límite individual.
- `TotalRevivesReceived`: reanimaciones recibidas en bruto.
- `RevivesGiven`: rescates realizados.
- `HelpCredits`: créditos de ayuda guardados (0 o 1 por defecto).
- `PenaltiesReceived`: castigos de ruleta recibidos en el nivel.
- `ConsecutivePenalties`: castigos seguidos sin un «Sin penalización» entre medias.
- `ThanksRecognized` / `ThanksMissed`: agradecimientos reconocidos y ventanas falladas.

Estos contadores pertenecen al mod: no sustituyen ni modifican las vidas internas de Cuphead. Se reinician al empezar o reintentar un nivel.

## Crédito bilateral de ayuda
Cuando A rescata a B:
1. Se registra una reanimación recibida por B. Si B tenía un crédito, se consume y su contador neto no sube.
2. Se registra un rescate realizado por A y A recibe su recompensa: si `RevivesReceived[A] > 0`, se resta uno; si es 0, guarda un crédito, con un máximo de uno guardado a la vez.
3. Solo cambian contadores del mod.

La regla es idéntica para P1 y P2.

## Límites y reinicio
- **Límite individual**: 3 reanimaciones recibidas netas por jugador y nivel (configurable por jugador; 0 lo desactiva).
- **Límite de equipo**: 6 reanimaciones totales del equipo en el nivel, contadas en bruto (los créditos no las descuentan; 0 lo desactiva). Existe porque, con los créditos, dos jugadores que se rescatan por turnos compensan siempre sus contadores individuales.
- Al alcanzar cualquiera de los dos, el mod pide **una sola vez** reiniciar el nivel y deja de procesar eventos hasta el siguiente inicio. No se abre ventana de agradecimiento ni se tira la ruleta.
- El reinicio solo se ejecuta por una vía verificada en el juego. Mientras no lo esté, `AutoRestartEnabled` debe estar a `false` y el mod solo avisa.
- Si los dos jugadores están muertos a la vez es la derrota normal de Cuphead: el mod no añade nada.

## Eventos fiables
- Cada muerte y cada reanimación llegan con una clave única (p. ej. la instancia del fantasma). Un duplicado no se cuenta dos veces.
- Se ignora y se registra en el log cualquier evento incoherente: rescatar a alguien que no está muerto, rescatar estando muerto, rescatarse a sí mismo, morir estando ya muerto o eventos sin nivel activo.

## Fantasma fuera de pantalla y robo de corazón
En Cuphead el fantasma no caduca. Si sube fuera de cámara, el jugador muerto puede robar un corazón a su pareja y revivir solo. **Fuera de alcance por ahora** (ADR-004): no se cuenta ni se castiga, y la integración no debe registrarlo como rescate.

## Agradecimiento
- Ventana: 3 s desde una reanimación confirmada.
- Vale para los dos jugadores. Por defecto basta con que se reconozca **«gracias»** («Gracias, Cari», «Gracias, amor», «muchas gracias»…). Con `RequireName` hace falta además uno de los apelativos configurados (`cari`, `kari`, `amor` por defecto).
- Se normaliza el texto: sin mayúsculas, tildes ni signos.
- Con un solo micrófono no se distingue quién habla: una frase reconocida cierra la ventana pendiente más antigua.
- Respaldo manual: una tecla o botón confirma el agradecimiento.
- Sin castigo cuando no se puede evaluar con justicia: no hay voz ni respaldo manual, hay un fallo técnico, el rescatado vuelve a morir durante la ventana, llega otra reanimación suya o el nivel termina.
- Si se desactiva «exigir agradecimiento», no hay ventana ni ruleta.

## Ruleta
Todos los resultados empiezan activados con estos pesos orientativos:

| Resultado | Peso | Probabilidad base |
|---|---|---|
| Sin penalización | 20 | 20 % |
| Movimiento lento | 20 | 20 % |
| Controles invertidos | 15 | 15 % |
| Disparo lento | 15 | 15 % |
| Dash limitado | 15 | 15 % |
| Castigo doble | 15 | 15 % |

- Duración general: 4 s, con duración específica opcional por efecto (de 0,5 a 30 s).
- Objetivo por defecto: el jugador rescatado que no dio las gracias (configurable para castigar al rescatador). Si está muerto, no hay ruleta.
- **No repetición**: no sale el mismo castigo dos veces seguidas al mismo jugador si hay alternativas. «Sin penalización» sí puede repetirse.
- **Cooldown**: 8 s desde el último castigo del jugador; dentro de ese margen la tirada es «Sin penalización» protegida.
- **Castigos seguidos**: tras 2 castigos seguidos, la siguiente tirada es «Sin penalización» protegida.
- **Castigo doble**: dos efectos distintos, activados y compatibles, elegidos por peso. Nunca incluye «Sin penalización». Si no existen dos efectos compatibles, «Castigo doble» queda fuera del sorteo.
- El mismo efecto no se apila: si ya está activo, solo se amplía su final. Un efecto incompatible con otro activo del mismo jugador no se aplica.
- Todo efecto se restaura al expirar, al morir el jugador, al terminar o reiniciar el nivel, al desactivar el mod o si falla su aplicación.
- Semilla de depuración opcional para reproducir incidencias.
- En el juego: «Controles invertidos» usa el mecanismo propio de Cuphead (solo el eje horizontal), «Movimiento lento» reduce la velocidad al 60 % y «Dash limitado» acorta el dash a la mitad. «Disparo lento» aún no es compatible y se quita de la ruleta cuando los efectos se aplican de verdad. Ver `docs/GAME_HOOKS.md`.

## Foco opcional en P2
`FocusIntensity` (0 a 1, **0 por defecto**) multiplica el peso de «Sin penalización» del jugador con foco por `1 − 0,5 · intensidad`. Con 0, P1 y P2 tienen exactamente las mismas probabilidades.

## Director de equilibrio
Usa solo los contadores del nivel del propio jugador; nunca aumenta el castigo de la pareja para compensar.

```
presión    = muertes·1 + reanimacionesRecibidasNetas·1,5 + castigosÚltimos60s·1 + castigosSeguidos·1
             − rescatesRealizados·0,5 − créditosGuardados·0,5          (mínimo 0)
protección = min(presión / 8, 1)
```

Con la protección `p`:
- Peso de «Sin penalización» × (1 + 2·p) → hasta el triple.
- Peso de «Castigo doble» × (1 − 1·p) → desaparece con protección máxima.
- Duración × (1 − 0,5·p), con un mínimo de 1 s.

Todos los coeficientes son configurables y el director se puede desactivar para volver a las probabilidades base.

**Ejemplo**: P2 con 3 muertes, 2 reanimaciones netas y 2 rescates realizados → presión 5, protección 62,5 %: «Sin penalización» pasa de 20 a 45, «Castigo doble» de 15 a 5,6 y los efectos duran 2,75 s en lugar de 4 s.
