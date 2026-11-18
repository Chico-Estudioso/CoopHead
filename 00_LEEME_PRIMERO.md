# Coophead — Léeme primero

Estructura de trabajo y guías para desarrollar Coophead, un plugin de reglas cooperativas para Cuphead, con Claude Code.

## Objetivo
Un plugin local para el cooperativo de dos jugadores de Cuphead que añade reglas configurables de reanimaciones, reciprocidad, agradecimiento, ruleta de penalizaciones y estadísticas.

Se plantea para Windows y una instalación local de Cuphead. No se presupone que el juego esté instalado desde Steam o Epic.

## Copia local del juego
- Se trabaja con una copia de pruebas separada de la instalación principal.
- No se sobrescribe ni modifica permanentemente el ejecutable original.
- Claude debe pedir la ruta de `Cuphead.exe` si no puede identificarla de forma inequívoca.
- Antes de instalar BepInEx hay que verificar arquitectura (x64/x86), versión, runtime y compatibilidad real.
- Mortar es la primera opción a probar si permite seleccionar la carpeta; la instalación manual de BepInEx es el plan alternativo.
- No se intenta eludir DRM, licencias ni protecciones del juego. El proyecto solo añade un plugin a una instalación que ya se ejecuta legítimamente.

## Decisiones de producto
Las vigentes están en `docs/REGLAS_DE_JUEGO.md`. Resumen:

- Dos jugadores en cooperativo local, con reglas idénticas para P1 y P2. Existe un foco opcional en P2, desactivado por defecto.
- Rescatar a la pareja da un crédito de ayuda que compensa una reanimación recibida propia (máximo uno guardado por jugador y nivel). Solo afecta a contadores del mod, nunca a las vidas reales.
- Límite individual de 3 reanimaciones netas y límite de equipo de 6 reanimaciones totales por nivel. Al alcanzar cualquiera se reinicia el nivel (solo por una vía verificada).
- Si los dos jugadores mueren a la vez, es la derrota normal del juego.
- El robo de corazón del fantasma fuera de pantalla queda fuera de alcance por ahora.
- Tras un rescate hay 3 s para dar las gracias. Basta con decir «gracias» («Gracias, Cari», «Gracias, amor»…), y vale para los dos jugadores. Respaldo con tecla.
- Voz: sin conexión, sin nube, sin envío ni grabación de audio. Si el motor local usa modelos de aprendizaje automático, se dice claramente.
- Ruleta inicial: Movimiento lento, Controles invertidos, Disparo lento, Sin penalización, Dash limitado y Castigo doble; efectos de 4 s ajustables por efecto; pesos configurables, sin repeticiones injustas y respetando la compatibilidad entre efectos.
- El director de equilibrio reduce la presión sobre quien acumula más muertes, reanimaciones y castigos recientes.

Cambios respecto al kit original: `docs/decisions/ADR-004-REVISION-DEL-KIT.md`.

## Orden de trabajo
- Prompt 00: investigación del entorno y viabilidad. *Hecha la parte de escritorio; falta la inspección local.*
- Prompt 01: plugin BepInEx mínimo y compilación.
- Prompt 02: auditoría de hooks y ciclo de vida de jugadores.
- Prompt 03: motor de reglas bilaterales y créditos de ayuda. *Lógica hecha en `Coophead.Core`.*
- Prompt 04: ruleta y director de equilibrio adaptativo. *Lógica hecha; faltan los efectos en el juego.*
- Prompt 05: reconocimiento de voz local. *Coincidencia de frase y ventanas hechas; falta la captura.*
- Prompt 06: configuración, avisos y estadísticas.
- Prompt 07: integración, pruebas y empaquetado.
- Prompt 08: mantenimiento tras actualizaciones de Cuphead.

Cada fase cierra con informe, pruebas y Pull Request.
