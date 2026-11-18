# Configuración

BepInEx genera el archivo `BepInEx/config/com.coophead.rules.cfg` la primera vez que arranca el juego con Coophead. Se puede editar con el juego cerrado o, en caliente, con un gestor de configuración de BepInEx. Los cambios se aplican al momento salvo `SemillaDepuracion`, que se lee al iniciar.

Los valores fuera de rango se corrigen y se avisa en `BepInEx/LogOutput.log`.

## 1 - General
| Clave | Por defecto | Descripción |
|---|---|---|
| `Activado` | `true` | Interruptor general. Al desactivarlo se restauran todos los efectos. |
| `Diagnostico` | `true` | Registra en el log cada evento y decisión. Sin audio ni datos personales. |
| `Hooks` | `true` | Instala los hooks de solo lectura. Requiere reiniciar el juego. |
| `AplicarReglas` | `false` | Envía muertes y rescates a las reglas. Actívalo solo tras verificar los hooks (`docs/GAME_HOOKS.md`). |

## 2 - Reglas
| Clave | Por defecto | Rango |
|---|---|---|
| `ReglasActivadas` | `true` | |
| `LimiteReanimacionesP1` / `LimiteReanimacionesP2` | `3` | 0–20 (0 = sin límite) |
| `LimiteReanimacionesEquipo` | `6` | 0–50 (0 = sin límite) |
| `CreditosDeAyuda` | `true` | |
| `CreditosMaximos` | `1` | 0–5 |
| `ReinicioAutomatico` | `false` | Solo debe activarse cuando el hook de reinicio esté verificado. Desactivado = solo aviso. |

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

## 7 - Voz (experimental, solo Windows)
| Clave | Por defecto | Descripción |
|---|---|---|
| `VozActivada` | `false` | Reconocimiento local con el reconocedor de Windows. Requiere reiniciar el juego. |
| `Idioma` | `es-ES` | Debe estar instalado con «Reconocimiento de voz» en Windows. |
| `ConfianzaMinima` | `0.6` | 0,1–1. Súbela si hay falsos positivos; bájala si no te reconoce. |
| `TeclaPruebaVoz` | `F9` | Escucha 5 s y escribe en el log lo reconocido. |

Privacidad y limitaciones en `docs/VOZ_Y_PRIVACIDAD.md`.

## 5 - Agradecimiento
| Clave | Por defecto | Descripción |
|---|---|---|
| `ExigirAgradecimiento` | `true` | Sin agradecimiento no hay ventana ni ruleta. |
| `VentanaSegundos` | `3` | 1–15 |
| `PalabraObligatoria` | `gracias` | Se ignoran mayúsculas, tildes y signos. |
| `Apelativos` | `cari, kari, amor` | Separados por comas; valen para ambos jugadores. |
| `ExigirApelativo` | `false` | Si es `true`, hace falta «gracias» + apelativo. |
| `RespaldoManual` | `true` | Confirmar con tecla. Sin teclas asignadas cuenta como desactivado. |
| `TeclaGraciasP1` / `TeclaGraciasP2` | `F7` / `F8` | No verificado que estén libres en tu configuración: cámbialas si chocan. |
