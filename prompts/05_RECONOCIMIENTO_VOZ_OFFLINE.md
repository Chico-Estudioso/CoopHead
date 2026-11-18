# PROMPT 05 — RECONOCIMIENTO DE AGRADECIMIENTO LOCAL

## Objetivo
Añadir reconocimiento opcional de una frase pronunciada después de una reanimación.

## Comportamiento
- Tras una reanimación confirmada, inicia una ventana de tres segundos.
- Frase: basta con «gracias» («Gracias, Cari», «Gracias, amor»…), válida para ambos jugadores (ADR-004). La coincidencia ya está en `Coophead.Core` (`ThanksPhraseMatcher`).
- Frase configurable por el usuario.
- Si se reconoce con confianza suficiente, se registra agradecimiento y se cancela la ruleta asociada a ese rescate.
- Si no se reconoce, se permite continuar con la regla configurada.
- El objetivo normal de la ruleta es quien fue rescatado.

## Privacidad no negociable
- Sin servicios de Internet, cloud o APIs remotas.
- No transmitir audio.
- No guardar grabaciones ni transcripciones persistentes.
- Capturar solo durante la ventana de agradecimiento y durante pruebas explícitas del micrófono.
- Permitir seleccionar el dispositivo de entrada.
- Permitir desactivar voz por completo.
- No penalizar por fallo del micrófono, ausencia de dispositivo, error del motor o baja confianza técnica.

## Investigación antes de elegir biblioteca
Compara opciones locales para español y el runtime disponible. Comprueba licencias, arquitectura, tamaño, dependencias nativas y distribución.
Vosk u otros motores pueden utilizar modelos de aprendizaje automático. No describas un motor como «sin IA» si usa ML. Aclara la diferencia entre IA generativa, reconocimiento automático local y detección de palabra clave.
Si el usuario exige cero modelos de ML, estudia keyword spotting/procesamiento de señal y explica sus limitaciones de precisión.

## Arquitectura
No bloquees el hilo Unity. Si la biblioteca de reconocimiento no es compatible con el runtime del plugin, evalúa un pequeño proceso auxiliar local. Su comunicación debe ser local, mínima y controlada. No debe aceptar órdenes arbitrarias del juego ni mantener el micrófono abierto indefinidamente.

## Fallback
Implementa una confirmación manual por tecla/botón configurable. Si el reconocedor falla, la opción debe ser neutral: mostrar un aviso y no aplicar una penalización por fallo técnico.

## Pruebas
- frase correcta;
- frase similar pero incorrecta;
- ruido de juego;
- dos personas hablando;
- micrófono desconectado;
- permiso de micrófono denegado;
- voz fuera de ventana;
- frase durante ventana;
- ausencia de almacenamiento de audio.

## Entrega
Informe de precisión medida, recursos utilizados, licencias, consumo aproximado, privacidad y limitaciones. PR sin merge.
