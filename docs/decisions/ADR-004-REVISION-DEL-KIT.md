# ADR-004 — Revisión del kit de inicio

Estado: Aprobado (18 de noviembre de 2026).

## Contexto
El kit de inicio se redactó como si varias decisiones de producto ya estuvieran tomadas. Al revisarlo aparecieron huecos y contradicciones. Las críticas de diseño se consultaron con el usuario; las cuestiones técnicas u organizativas se decidieron directamente.

## Decisiones consultadas con el usuario

### 1. Límite de equipo además del límite individual
Con los créditos de ayuda tal como estaban, dos jugadores que mueren y se rescatan por turnos compensan siempre sus contadores: podían morir diez veces sin que el nivel se reiniciara nunca.

Decisión: se mantienen los créditos y el límite individual (3 reanimaciones netas) y se añade un **límite de equipo**: reanimaciones totales del nivel, sin descontar créditos. Valor inicial: 6. Ambos son configurables y se desactivan con 0.

Si los dos jugadores están muertos a la vez, es la derrota normal de Cuphead y el mod no hace nada más.

### 2. Frase de agradecimiento para ambos jugadores
«Gracias, Kari» solo encajaba cuando Kari rescataba. El usuario aclaró que ambos jugadores se llaman «Cari», «amor» o lo que sea.

Decisión: la frase vale igual para los dos jugadores. Por defecto basta con decir **«gracias»**; la lista de apelativos («cari», «kari», «amor») sirve para construir la gramática del reconocedor y puede exigirse con `RequireName`. Ya no hay un nombre fijo por jugador.

### 3. Foco en P2
El kit hablaba de «algo más de protagonismo» para P2 sin definirlo, en contradicción con reglas simétricas.

Decisión: reglas **idénticas** para P1 y P2 por defecto. Existe un ajuste `FocusIntensity` (0 por defecto) que, si se sube, reduce hasta la mitad el peso de «Sin penalización» para el jugador con foco (P2 por defecto).

### 4. Robo de corazón al salir de pantalla
El código de referencia muestra que en Cuphead el fantasma no caduca: si sube fuera de cámara, el jugador muerto puede robar un corazón a su pareja (si esta tiene más de 1) y revivir solo. El kit no lo contemplaba.

Decisión: **fuera de alcance por ahora**. No se cuenta ni se castiga. La integración (M2) deberá distinguir este caso del rescate por parry para no contarlo como rescate. Ver `docs/GAME_HOOKS.md`.

## Decisiones técnicas tomadas sin consulta
- Nombre del proyecto: **Coophead** (repositorio `Chico-Estudioso/CoopHead`).
- `main` arranca con el kit original sin cambios, para que la primera PR muestre qué se modificó.
- La lógica de reglas, ruleta, equilibrio y agradecimiento vive en `Coophead.Core`, una biblioteca pura que compila para `net35` (runtime de los plugins de Cuphead según la evidencia comunitaria) y `netstandard2.0` (pruebas). Se implementó antes de M1/M2 porque no depende del juego.
- Cada evento del juego llega con una clave única para deduplicarlo; un evento incoherente (rescatar a alguien vivo, rescatar estando muerto) se ignora y se registra.
- «Sin penalización» no cuenta para la regla de no repetición: así no choca con la protección del director de equilibrio.
- Tras dos castigos seguidos, la siguiente tirada es «Sin penalización» protegida. Cooldown inicial: 8 s.
- «Castigo doble» queda fuera del sorteo si no hay dos efectos activados y compatibles.
- Si el jugador rescatado vuelve a morir durante su ventana, o el nivel termina, la ventana se cierra sin castigo. Morir también restaura sus efectos activos.
- Con un solo micrófono no se distingue quién habla: una frase reconocida cierra la ventana pendiente más antigua.
- Si no se exige agradecimiento, no se abre ventana ni se tira la ruleta.
- Se quita «Plan Mode» y la rama `feat/...` como requisitos rígidos: en sesiones en la nube se trabaja en la rama de sesión `claude/...` y se integra por PR.
- Las PR y los commits se firman a nombre del usuario.
