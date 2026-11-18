# Roadmap — Cuphead Co-op Rules

## M0 — Investigación y entorno local
- Identificar `Cuphead.exe`, versión, arquitectura, runtime Unity/Mono y ensamblados.
- Evaluar Mortar con instalación desde carpeta.
- Evaluar instalación manual de BepInEx 5.
- Auditar proyecto comunitario de co-op/revive como referencia, sin copiar código automáticamente.
- Identificar métodos reales de muerte, ghost, revive, nivel y jugador.
- Validar opciones de reconocimiento de voz local y requisitos/licencias.
- Resultado: informe técnico y decisión GO/NO-GO.

## M1 — Plugin mínimo
- Crear solución C# con target framework verificado.
- Referenciar BepInEx/Harmony y ensamblados de juego de forma local.
- Crear plugin que cargue y escriba logs.
- Crear build reproducible sin incluir DLLs propietarias.
- Resultado: plugin detectado por BepInEx en copia de pruebas.

## M2 — Auditoría de eventos de jugador
- Identificar Player 1 y Player 2 de forma fiable.
- Detectar muerte, aparición del fantasma, reanimación exitosa y fallo de rescate.
- Detectar cambio/fin de nivel y reinicio.
- Implementar telemetría de depuración no invasiva.
- Resultado: eventos comprobados en partidas locales.

## M3 — Motor de reglas bilateral
- Contadores por jugador y nivel.
- Límite configurable de reanimaciones recibidas.
- Créditos de ayuda: rescatar a la pareja compensa una reanimación recibida propia; máximo un crédito guardado por nivel.
- Aplicar créditos antes de incrementar el contador en la próxima reanimación recibida.
- Reinicio de nivel al alcanzar el umbral configurado.
- Resultado: reglas verificadas con ambos jugadores.

## M4 — Ruleta y equilibrio adaptativo
- Movimiento lento, controles invertidos, disparo lento, sin penalización, dash limitado y castigo doble.
- Duración inicial de cuatro segundos.
- Pesos configurables, cooldown, protección contra repetición.
- Balance Director con presión por jugador y protección cuando hay demasiada carga.
- Resultado: ninguna combinación deja al jugador bloqueado; todos los efectos se restauran.

## M5 — Reconocimiento de voz local
- Confirmar requisitos de privacidad y tolerancia a modelos locales.
- Selección de micrófono y prueba de entrada.
- Frase inicial configurable: «Gracias, Kari».
- Ventana de tres segundos tras una reanimación.
- Sin nube, sin APIs remotas, sin almacenamiento de audio.
- Fallback manual configurable si la voz no está disponible.
- Resultado: reconocimiento probado en el equipo real, falsos positivos/negativos documentados.

## M6 — Configuración y HUD
- Configuración BepInEx y/o interfaz propia, según viabilidad.
- HUD de contadores, créditos, temporizador y estado de voz.
- Resumen al terminar nivel.
- Resultado: ajustes accesibles y comprensibles.

## M7 — Integración y distribución
- Pruebas de regresión, niveles diferentes y cambios de escena.
- Empaquetado del plugin y documentación de instalación/desinstalación.
- Revisión de licencias de dependencias.
- Release candidate y revisión del usuario.
- Resultado: primera versión estable.

## M8 — Mantenimiento
- Auditoría de cambios del juego y BepInEx.
- Revisión de hooks afectados.
- Pruebas de regresión antes de declarar compatibilidad.
