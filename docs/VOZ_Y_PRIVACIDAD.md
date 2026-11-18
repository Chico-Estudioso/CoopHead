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

## Motor elegido (experimental)
**Reconocedor de voz de escritorio de Windows** (`System.Speech`, SAPI) a través de un proceso auxiliar, `Coophead.Voice.exe` (.NET Framework 4.8):

- **Tecnología**: reconocimiento automático del habla con modelos acústicos estadísticos que trae Windows. Es aprendizaje automático clásico, **no IA generativa**, y funciona **sin conexión**. No se describe como «sin IA».
- **Gramática cerrada**: solo puede reconocer las frases de agradecimiento configuradas («gracias», «gracias cari», «gracias amor»…), lo que reduce falsos positivos y evita transcribir conversaciones.
- **Sin descargas ni modelos en el repositorio**: usa el idioma de voz instalado en Windows (Configuración → Hora e idioma → Idioma → Español → Reconocimiento de voz).
- **Por qué un proceso aparte**: el plugin corre en el Mono de Unity con perfil .NET 3.5 y no puede cargar `System.Speech` de forma fiable. El ayudante solo acepta órdenes fijas (`LISTEN`, `STOP`, `TEST n`, `QUIT`) por su entrada estándar y devuelve texto reconocido por su salida estándar (`VoiceProtocol`). Se cierra si el juego se cierra.
- **Alternativa descartada por ahora**: Vosk (Apache 2.0) exige descargar un modelo de decenas de MB y dependencias nativas; queda como opción si el reconocedor de Windows no da buen resultado en español.

### Garantías de privacidad
- Solo escucha entre la apertura y el cierre de una ventana de agradecimiento, o durante la prueba de micrófono (F9, 5 s).
- No guarda audio ni transcripciones; el texto reconocido se usa en memoria y se descarta. Solo se escribe en el log durante la prueba o con `Diagnostico` activado.
- No usa red.
- Si el reconocedor o el micrófono fallan, la voz se marca como no disponible y **nadie es castigado** por ello.

### Limitaciones conocidas
- Usa el **micrófono predeterminado de Windows**: `System.Speech` no permite elegir otro dispositivo. Para cambiarlo, cambia el predeterminado en Windows.
- Con un solo micrófono no se distingue quién habla.
- No hay medidas de precisión todavía: hay que probar con el ruido real del juego y ajustar `ConfianzaMinima`.
- No probado en un equipo real: el ayudante compila, pero no se ha ejecutado.

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
