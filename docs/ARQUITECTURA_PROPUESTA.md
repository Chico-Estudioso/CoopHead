# Arquitectura propuesta

Esta arquitectura es una hipótesis inicial. M0 debe confirmar framework, API, referencias y hooks contra la versión real instalada.

## Componentes

### PluginBootstrap
Carga BepInEx, inicializa servicios y registra la versión del plugin.

### GameIntegration
Contiene todos los hooks de Harmony y traduce llamadas del juego a eventos internos. No contiene reglas de negocio.

### PlayerStateTracker
Mantiene estado por jugador y nivel: muertes, reanimaciones recibidas, rescates realizados, créditos y penalizaciones recientes.

### RuleEngine
Procesa eventos verificados (muerte, rescate, fin de ventana de rescate, fin de nivel) y decide las consecuencias.

### HelpCreditService
Cuando un jugador rescata a la pareja, compensa una reanimación recibida propia si hay contador positivo; si no, guarda un crédito, hasta un máximo de uno por jugador y nivel. El crédito se consume en la siguiente reanimación recibida. Reinicia al cambiar de nivel.

### BalanceDirector
Calcula una presión transparente por jugador usando contadores del nivel y penalizaciones recientes. Cuando la presión es alta, aumenta la probabilidad de no recibir castigo y reduce intensidad/duración. Su comportamiento debe poder desactivarse.

### RouletteService
Selecciona resultados habilitados por peso, evita repeticiones consecutivas y valida compatibilidad de efectos. El resultado «Castigo doble» selecciona dos efectos distintos que puedan coexistir.

### PenaltyController
Aplica cada efecto de forma temporal y restaura exactamente el valor previo. Debe restaurar al expirar, cambiar de nivel, desactivar el plugin o producirse una excepción.

### VoiceService
Opcional. Captura audio solo durante la ventana de agradecimiento, procesa localmente y descarta el buffer. No guarda audio ni se comunica con servicios externos.

### UI / HUD
Muestra estado, temporizador y contadores. Si no hay un hook UI estable, usar overlay BepInEx de forma independiente.

## Flujo de una reanimación
1. GameIntegration detecta y confirma una reanimación exitosa.
2. PlayerStateTracker registra el rescate del rescatador y la reanimación recibida por el rescatado, consumiendo primero un crédito pendiente si existe.
3. HelpCreditService recompensa al rescatador reduciendo su contador de reanimaciones recibidas o guardando un crédito.
4. RuleEngine comprueba si un contador alcanzó el límite de reinicio.
5. VoiceService abre la ventana de tres segundos si está habilitado.
6. Si se reconoce la frase, se registra agradecimiento y no se lanza la ruleta.
7. Si expira sin reconocimiento, BalanceDirector calcula protección y RouletteService decide resultado.
8. PenaltyController aplica el efecto al jugador rescatado, salvo que el modo de configuración indique otra cosa.

## Seguridad funcional
- Los eventos deben deduplicarse para evitar contar dos veces un mismo rescate.
- No usar polling permanente si existe un evento/hook verificable.
- No depender de nombres de jugador imaginados: mapear PlayerOne/PlayerTwo con evidencia del juego.
- Una API desconocida debe permanecer detrás de una interfaz y quedar deshabilitada hasta verificarse.
