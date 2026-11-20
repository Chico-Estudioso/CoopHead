# Prueba de voz por el escritorio (Discord) — Cuphead 1.3.4

- **Fecha**: 20/11/2026.
- **Tipo**: manual, prueba de voz de 5 s (F6 → Ajustes → «Probar la voz»), repetida varias veces con el juego sonando. La pareja habla por Discord; el jugador local usa su micrófono.
- **Build**: Cuphead 1.3.4 x64, BepInEx 5.4.23.5, Coophead con la tubería de audio corregida (PR #16).
- **Configuración relevante**: `FuenteVoz = Ambos`, `DispositivoEscritorio = todos`, `ConfianzaMinimaEscritorio = 0.6`.

## Resultado

| Comprobación | Resultado |
|---|---|
| Salidas detectadas | 7 salidas activas (auriculares, dos cables de audio virtuales, monitor, S/PDIF, interfaz de audio USB) |
| Salida por la que suena Discord | La **salida predeterminada** de Windows (auriculares): volumen máximo 78–96 %. El resto, sin audio o en silencio |
| Voz de la pareja por el escritorio | **Reconocida**: «gracias cari» con confianza 0,67, 0,68 y 0,60 (una por prueba) |
| Voz local por el micrófono | Reconocida: «gracias amor», «gracias cari», «gracias mi amor» con confianza 0,64–0,71 |
| Salida virtual de 16 canales | Falla al empezar a escuchar en cada prueba («No implementado») |

## Conclusiones
1. La corrección de la tubería (lecturas que el reconocedor tomaba como fin del audio) funciona: es la primera vez que el escritorio reconoce algo.
2. Discord no usaba otra salida: sonaba por la predeterminada. Escuchar todas las salidas no hacía falta y cuesta un reconocedor por salida.
3. La confianza de la voz por Discord (0,60–0,68) queda justo en el umbral de 0,6: una de las tres pasó por 0,00.
4. El jugador pulsó varias veces la prueba seguida porque no veía que estaba en marcha, y la línea «fuera de ventana» parecía un fallo cuando era un reconocimiento correcto sin rescate pendiente.

## Cambios derivados
- `DispositivoEscritorio` pasa a `predeterminado` por defecto (salidas predeterminadas multimedia y de comunicaciones); `todos` queda para diagnosticar.
- `ConfianzaMinimaEscritorio` pasa a 0,5 por defecto, y las dos confianzas se aplican al momento al moverlas en F6, sin reiniciar el juego.
- Una salida que no se puede escuchar se avisa una vez y se ignora hasta reiniciar.
- Durante la prueba, F6 muestra «Escuchando…» y no admite otra prueba a la vez; un reconocimiento en la prueba aparece como «RECONOCIDO (prueba: en partida contaría como gracias)». Las líneas repetidas se agrupan con «(×N)».

## Pendiente
- Comprobarlo en una partida real, con rescates, y medir cuántos «gracias» se pierden con la música del juego.
