# Cuphead Co-op Rules — Kit de inicio

Este kit contiene la estructura inicial de trabajo y los prompts para desarrollar un plugin de reglas cooperativas para Cuphead con Claude Code.

## Objetivo

Crear un plugin local para el cooperativo de dos jugadores de Cuphead que añada reglas configurables de reanimaciones, reciprocidad, reconocimiento de agradecimiento, ruleta de penalizaciones y estadísticas.

El proyecto se plantea inicialmente para Windows y una instalación local de Cuphead. No se presupone que el juego esté instalado desde Steam o Epic.

## Antes de empezar

1. Crea un repositorio privado de GitHub llamado `Cuphead-Coop-Rules`.
2. Copia el contenido de `repo_files/` a la raíz del repositorio.
3. Mantén `main` protegida y no trabajes directamente sobre ella.
4. Abre Claude Code desde la carpeta local del repositorio.
5. Ejecuta `prompts/00_INVESTIGACION_Y_ENTORNO_LOCAL.md` en Plan Mode.
6. Revisa la investigación antes de permitir cambios en la instalación del juego.

## Importante sobre tu copia local

- El proyecto debe trabajar con una copia de pruebas separada de tu instalación principal.
- No se debe sobrescribir ni modificar permanentemente el ejecutable original.
- Claude debe pedirte la ruta de `Cuphead.exe` si no puede identificarla de forma inequívoca.
- Antes de instalar BepInEx, debe verificar arquitectura (x64/x86), versión, runtime y compatibilidad real.
- Mortar es la primera opción a probar si permite seleccionar tu carpeta; la instalación manual de BepInEx es el plan alternativo.
- No se debe intentar eludir DRM, licencias o protecciones del juego. El proyecto solo añade un plugin a una instalación que ya puede ejecutarse legítimamente.

## Decisiones de producto ya acordadas

- Dos jugadores en cooperativo local.
- El Jugador 2 recibe algo más de protagonismo en la configuración inicial, pero las reglas deben ser bilaterales y justas.
- Si un jugador rescata a su pareja, recibe un crédito de ayuda que compensa una reanimación recibida por ese jugador.
- El crédito se aplica al contador del mod, no altera las vidas reales del juego.
- Máximo un crédito de ayuda almacenado por jugador y nivel.
- Si un contador está en cero, el crédito queda almacenado y se consume en la próxima reanimación recibida.
- Si se alcanza el límite configurable de reanimaciones recibidas, se reinicia el nivel.
- Detectar también, si la API lo permite de forma fiable, que un fantasma desaparece sin ser rescatado.
- Tras una reanimación, se abre una ventana de tres segundos para reconocer una frase de agradecimiento, inicialmente «Gracias, Kari».
- Si no se reconoce, la ruleta aplica una consecuencia al jugador rescatado, salvo que la opción esté desactivada o el sistema adaptativo lo proteja.
- Reconocimiento de voz: sin conexión, sin servicios en la nube, sin envío de audio y sin guardar grabaciones. Hay que explicar con claridad si el motor local utiliza modelos de aprendizaje automático; no presentarlo como «cero IA» si no lo es.
- Penalizaciones iniciales activadas: Movimiento lento, Controles invertidos, Disparo lento, Sin penalización, Dash limitado y Castigo doble.
- Duración inicial de efectos: cuatro segundos, con posibilidad de ajustar la duración por efecto.
- La ruleta debe tener pesos configurables, evitar repeticiones injustas y respetar compatibilidad entre efectos.
- El sistema adaptativo reduce la presión sobre quien acumula más muertes, reanimaciones recibidas y penalizaciones recientes.

## Orden de trabajo

- Prompt 00: investigación del entorno y viabilidad.
- Prompt 01: plugin BepInEx mínimo y compilación.
- Prompt 02: auditoría de hooks y ciclo de vida de jugadores.
- Prompt 03: motor de reglas bilaterales y créditos de ayuda.
- Prompt 04: ruleta y director de equilibrio adaptativo.
- Prompt 05: reconocimiento de voz local.
- Prompt 06: configuración, avisos y estadísticas.
- Prompt 07: integración, pruebas y empaquetado.
- Prompt 08: mantenimiento tras actualizaciones de Cuphead.

No ejecutes todos los prompts en una sola sesión. Cada fase debe cerrar con informe, pruebas y Pull Request.
