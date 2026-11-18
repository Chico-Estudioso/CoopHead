# Reconocimiento de voz y privacidad

## Requisitos obligatorios
- Procesamiento local, sin Internet.
- Sin APIs remotas, nube ni servicios externos.
- No guardar grabaciones ni transcripciones persistentes.
- Capturar audio solo durante la ventana configurada de agradecimiento, salvo una prueba explícita del micrófono iniciada por el usuario.
- Mostrar qué micrófono se está utilizando.
- Permitir desactivar la función completamente.

## Aclaración tecnológica
El reconocimiento de habla local moderno suele utilizar modelos estadísticos o de aprendizaje automático. Eso no implica IA generativa ni conexión a la nube, pero no se debe describir como «sin IA» en sentido técnico si existe un modelo de ML.

M0 debe comparar:
- Reconocedor de Windows/offline disponible en el equipo.
- Vosk u otro motor local, comprobando arquitectura, licencia, dependencias nativas y calidad en español.
- Alternativa de detección de palabra clave si el usuario exige no utilizar ningún modelo de ML, dejando constancia de sus limitaciones.

No incluir modelos grandes dentro del repositorio. El modelo se instalará localmente, se descargará desde su fuente oficial por el usuario o se empaquetará solo después de auditar su licencia y tamaño.

## Frase reconocida (implementado en Coophead.Core)
- Vale para los dos jugadores (ADR-004). Por defecto basta con «gracias»; los apelativos `cari`, `kari` y `amor` se añaden a la gramática y pueden exigirse con `RequireName`.
- El texto se normaliza (minúsculas, sin tildes ni signos) antes de comparar.
- `ThanksPhraseMatcher.BuildGrammar()` devuelve las frases para un reconocedor con gramática cerrada, lo que reduce falsos positivos.
- El Core solo recibe texto ya reconocido, en memoria; nunca audio.
- Con un solo micrófono no se distingue quién habla: cualquiera de los dos puede decir la frase.

## Fallback
Debe existir una opción para que el jugador confirme el agradecimiento mediante una tecla o botón, si así se configura. Si el reconocedor no está disponible, no debe penalizarse automáticamente a alguien por un fallo técnico.

## Pruebas
Medir falsos positivos y falsos negativos con micrófonos reales, ruido del juego, distancia y variantes de pronunciación. No afirmar precisión sin resultados medidos.
