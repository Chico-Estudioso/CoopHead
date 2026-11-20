# Configuración

BepInEx genera el archivo `BepInEx/config/com.coophead.rules.cfg` la primera vez que arranca el juego con Coophead. Se puede editar con el juego cerrado o, en caliente, con un gestor de configuración de BepInEx. Los cambios se aplican al momento salvo `SemillaDepuracion`, que se lee al iniciar.

Los valores fuera de rango se corrigen y se avisa en `BepInEx/LogOutput.log`.

**Ojo**: BepInEx no cambia los valores que ya están en tu archivo. Si lo creaste con una versión anterior, `AplicarReglas`, `AplicarEfectos` y `ReinicioAutomatico` pueden seguir en `false`: cámbialos a mano.

## Ventana de Coophead (F6)
Pulsa **F6** dentro del juego para abrir o cerrar la ventana, que se maneja con el ratón. Mejor con el juego en pausa o en el mapa. Pestañas:

- **Partida**: el intento en curso en directo (daño, parrys, muertes, gracias, castigos…), ventanas de agradecimiento abiertas y castigos activos.
- **Recuerdos**: el libro de recuerdos. Elige un nivel a la izquierda y verás intentos, victorias, tiempos y la comparativa P1/P2 de los totales, del **try final** (última victoria) o del último intento. En dorado, quién lleva más.
- **Totales**: todo lo jugado juntos y los niveles más intentados.
- **Ruleta**: activar o desactivar cada castigo, su peso y la probabilidad actual para cada jugador según su presión.
- **Ajustes**: los interruptores principales (reglas, reinicio, límites, agradecimiento, voz, castigos y HUD). Los cambios se guardan al momento. En la parte de voz hay un botón para **probar la voz 5 s** y la lista de las **últimas frases oídas**: aceptadas, por debajo de la confianza mínima o no entendidas, con su fuente y confianza, para ajustar `ConfianzaMinima` y `ConfianzaMinimaEscritorio`.
- **Registro**: los últimos mensajes de Coophead, para ver si se detectan bien muertes, rescates y frases.

El HUD de la esquina sigue funcionando igual durante la partida. Para editar todos los ajustes desde el juego también se puede instalar el mod BepInEx.ConfigurationManager (opcional, se abre con F1).

## 9 - Ventana
| Clave | Por defecto | Descripción |
|---|---|---|
| `TeclaVentana` | `F6` | Abre y cierra la ventana. Antes era F11; se cambió para no confundirla con la pantalla completa de otros programas. Si tu archivo ya tiene F11 guardado, cámbialo aquí. |
| `EscalaVentana` | `1` | 0,6–2. Tamaño de la ventana (se ajusta además a la resolución). |

## 1 - General
| Clave | Por defecto | Descripción |
|---|---|---|
| `Activado` | `true` | Interruptor general. Al desactivarlo se restauran todos los efectos. |
| `Diagnostico` | `true` | Registra en el log cada evento y decisión. Sin audio ni datos personales. |
| `Hooks` | `true` | Instala los hooks de solo lectura. Requiere reiniciar el juego. |
| `AplicarReglas` | `true` | Envía muertes y rescates a las reglas, el HUD y las estadísticas. Desactivado = solo log. |

## 2 - Reglas
| Clave | Por defecto | Rango |
|---|---|---|
| `ReglasActivadas` | `true` | |
| `LimiteReanimacionesP1` / `LimiteReanimacionesP2` | `3` | 0–20 (0 = sin límite) |
| `LimiteReanimacionesEquipo` | `6` | 0–50 (0 = sin límite) |
| `CreditosDeAyuda` | `true` | |
| `CreditosMaximos` | `1` | 0–5 |
| `ReinicioAutomatico` | `true` | Reinicia el nivel con `SceneLoader.ReloadLevel()` al alcanzar un límite. Desactivado = solo aviso en el HUD y en el log. |

## 3 - Ruleta
| Clave | Por defecto | Rango |
|---|---|---|
| `RuletaActivada` | `true` | |
| `DuracionGeneral` | `4` s | 0,5–30 |
| `<Resultado>Activado` | `true` | Para `SinPenalizacion`, `MovimientoLento`, `ControlesInvertidos`, `DisparoLento`, `DashLimitado` y `CastigoDoble`. |
| `<Resultado>Peso` | 20 / 20 / 15 / 15 / 15 / 15 | 0–1000 |
| `<Efecto>Duracion` | `0` (usa la general) | 0–30, solo para los cuatro efectos temporales |
| `Cooldown` | `8` s | 0–120 |
| `EvitarRepeticiones` | `true` | |
| `MaxCastigosSeguidos` | `2` | 0–10 (0 = sin protección) |
| `FocoP2` | `0` | 0–1 (0 = reglas idénticas) |
| `ObjetivoCastigo` | `Rescued` | `Rescued` (rescatado) o `Rescuer` (rescatador) |
| `SemillaDepuracion` | `-1` | -1 = aleatorio normal |

## 4 - Equilibrio
| Clave | Por defecto | Rango |
|---|---|---|
| `DirectorActivado` | `true` | |
| `PesoMuertes` | `1` | 0–10 |
| `PesoReanimacionesRecibidas` | `1,5` | 0–10 |
| `PesoCastigosRecientes` | `1` | 0–10 |
| `PesoCastigosSeguidos` | `1` | 0–10 |
| `PesoRescatesRealizados` | `0,5` | 0–10 (resta) |
| `PesoCreditosGuardados` | `0,5` | 0–10 (resta) |
| `VentanaCastigosRecientes` | `60` s | 0–600 (0 = todo el nivel) |
| `PresionMaxima` | `8` | 0,5–100 |
| `RefuerzoSinPenalizacion` | `2` | 0–10 |
| `ReduccionCastigoDoble` | `1` | 0–1 |
| `ReduccionDuracion` | `0,5` | 0–1 |
| `DuracionMinima` | `1` s | 0–10 |

La fórmula está en `docs/REGLAS_DE_JUEGO.md`.

## 6 - HUD
| Clave | Por defecto | Descripción |
|---|---|---|
| `MostrarHUD` | `true` | Contadores, créditos, límite de equipo, ventana de agradecimiento, efectos activos y estado de la voz. Solo durante los niveles. |
| `Esquina` | `SuperiorDerecha` | `SuperiorIzquierda`, `SuperiorDerecha`, `InferiorIzquierda` o `InferiorDerecha`. |
| `TamanoFuente` | `14` | 8–32 |
| `SegundosResumen` | `8` | 0–30. Resumen del nivel al terminar (0 = no mostrarlo). |

El HUD no captura entrada ni bloquea controles. Ejemplo:

```
Coophead · reglas activas
P1: recibidas 0/3 · dadas 1 · muertes 0 · crédito 1
P2 (fantasma): recibidas 1/3 · dadas 0 · muertes 2
Equipo: 1/6 reanimaciones
P2: ¡da las gracias! 2.0 s
Agradecimiento: voz no disponible · tecla activa
```

## 8 - Efectos (experimental)
| Clave | Por defecto | Descripción |
|---|---|---|
| `AplicarEfectos` | `true` | Aplica de verdad los castigos. Desactivado = simulados (se ven en el HUD con «(simulado)»). Los efectos no compatibles con tu versión se quitan de la ruleta. |
| `FactorMovimientoLento` | `0.6` | 0,2–1. Velocidad durante «Movimiento lento». |
| `FactorDash` | `0.5` | 0,2–1. Duración del dash durante «Dash limitado». |

«Disparo lento» todavía no tiene una vía segura en el juego.

## 7 - Voz (experimental, solo Windows)
| Clave | Por defecto | Descripción |
|---|---|---|
| `VozActivada` | `false` | Reconocimiento local con el reconocedor de Windows. Requiere reiniciar el juego. |
| `Idioma` | `es-ES` | Debe estar instalado con «Reconocimiento de voz» en Windows. |
| `ConfianzaMinima` | `0.6` | 0,1–1. Súbela si hay falsos positivos; bájala si no te reconoce. |
| `FuenteVoz` | `Ambos` | `Microfono`, `Escritorio` (lo que suena en el PC, p. ej. Parsec) o `Ambos`. Requiere reiniciar el juego. |
| `ConfianzaMinimaEscritorio` | `0.6` | 0,1–1. Para lo oído por el escritorio, donde también suena el juego. |
| `RetrasoEscritorio` | `0.3` s | 0–2. Tiempo inicial de la ventana en que se ignora el escritorio (sonido de reanimación del juego). Pequeño para que un «gracias» dicho justo al hacer el parry cuente. |
| `TeclaPruebaVoz` | `F9` | Escucha 5 s y escribe en el log lo reconocido y por qué fuente. |

Privacidad y limitaciones en `docs/VOZ_Y_PRIVACIDAD.md`.

## 5 - Agradecimiento
| Clave | Por defecto | Descripción |
|---|---|---|
| `ExigirAgradecimiento` | `true` | Sin agradecimiento no hay ventana ni ruleta. |
| `VentanaSegundos` | `3` | 1–15 |
| `PalabraObligatoria` | `gracias` | Se ignoran mayúsculas, tildes y signos. |
| `Apelativos` | `cari, kari, amor` | Separados por comas; valen para ambos jugadores. |
| `MargenVoz` | `1.5` s | 0–5. Tiempo extra tras el plazo para que el reconocedor entregue una frase dicha a tiempo (tarda en dar el resultado). No se ve en el HUD, que muestra «comprobando la voz…». |
| `ExigirApelativo` | `false` | Si es `true`, hace falta «gracias» + apelativo. |
| `RespaldoManual` | `true` | Confirmar con tecla. Sin teclas asignadas cuenta como desactivado. |
| `TeclaGraciasP1` / `TeclaGraciasP2` | `F7` / `F8` | No verificado que estén libres en tu configuración: cámbialas si chocan. |
