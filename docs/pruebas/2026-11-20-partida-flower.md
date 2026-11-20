# Partida real — Flower, Cuphead 1.3.4

- **Fecha**: 20/11/2026.
- **Tipo**: manual, dos jugadores (P2 conectado por Parsec).
- **Build**: Cuphead 1.3.4 x64, BepInEx 5.4.23.5, Coophead con estadísticas y ventana (PR #8–#11).
- **Configuración relevante**: `AplicarReglas`, `AplicarEfectos` y `ReinicioAutomatico` activados; `VozActivada = true`, `FuenteVoz = Ambos`; `VentanaSegundos` ≈ 2 (1,99).

## Resultado

| Comprobación | Resultado |
|---|---|
| Inicio de nivel, derrota y salida | Correcto en los 9 intentos; un evento por intento |
| Muerte → fantasma → rescate por parry → reaparición | Correcto, misma clave de fantasma, jugador correcto |
| Golpes, parrys, súper y daño | Se cuentan |
| Límite individual y reinicio | P2 llegó a 3 reanimaciones netas → nivel reiniciado («Restarted») |
| Castigos reales | Movimiento lento, controles invertidos, dash limitado y castigo doble aplicados sin errores |
| Voz por el escritorio | **Falla**: «El método especificado no es compatible» en cada ventana |
| Ventana de agradecimiento | **Mal sincronizada**: caducaba y castigaba antes de que el rescatado reapareciera |

## Fallos encontrados y corrección
1. **Voz del escritorio**: `AudioPipeStream.Seek` lanzaba `NotSupportedException` y `System.Speech` no aceptaba el flujo. Ahora `Seek` y `SetLength` no hacen nada y, si el reconocedor rechaza el flujo, se apaga la captura.
2. **Momento de la ventana**: el parry llega más de 2 s antes de que el juego confirme la reaparición. Ahora la ventana se abre con el parry (se puede dar las gracias desde ese momento), pero el plazo empieza al confirmarse la reaparición (`PlayerStatsManager.OnRevive`), con una espera máxima de 6 s. El castigo nunca llega mientras el jugador reaparece.
3. El `Level.OnDestroy` que llega después de ganar o perder ya no escribe un «Nivel terminado (Exited)» confuso.

## Observación sin cambiar
Con los valores por defecto, el director de equilibrio protege mucho a quien muere más: tras 2 muertes de P2 su protección pasaba del 80 % y la ruleta casi siempre daba «Sin penalización». Se consulta al usuario si quiere suavizarlo.

## Log relevante (extracto)
```
[#34] Fantasma -177400 de P2
[#35] Parry de P1
[#36] Rescate por parry del fantasma -177400: P1 rescata a P2
Ventana de agradecimiento abierta para P2 (1.99 s).
[Warning:  Coophead] Voz: escritorio: El método especificado no es compatible.
Ventana de P2 cerrada: Missed
Castigo aplicado a P2: movimiento lento durante 3.4 s.
Ruleta para P2: Movimiento lento (Rolled). P2: presión 2.5, protección 31%, sin penalización ×1.63, castigo doble ×0.69, duración ×0.84
[#37] Reanimación confirmada por el juego: P2
...
[#76] Rescate por parry del fantasma -256118: P1 rescata a P2
Límite alcanzado (PlayerLimit): se solicita reiniciar el nivel.
[#77] Nivel terminado (Restarted) vía Level.OnDestroy
[#78] Nivel iniciado: FlowerLevel (instancia 78750)
```
