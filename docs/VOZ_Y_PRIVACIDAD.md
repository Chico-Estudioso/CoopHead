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

### Fuentes de audio (`FuenteVoz`)
- **Microfono**: el micrófono predeterminado de Windows.
- **Escritorio**: lo que suena por la salida de audio predeterminada del PC (WASAPI en bucle, con NAudio). Sirve para jugar por **Parsec**: la voz de tu pareja llega al PC anfitrión por los altavoces o cascos y se reconoce ahí.
- **Ambos** (por defecto): un reconocedor por fuente. Si la misma frase llega por las dos en menos de 1 s (por ejemplo, la voz de los altavoces entra también por el micrófono), cuenta una sola vez.

La fuente del escritorio también oye el juego: por eso tiene su propia `ConfianzaMinimaEscritorio` (0,6) y se ignora durante los primeros `RetrasoEscritorio` segundos (0,3 s), cuando suena el efecto de reanimación del juego. Es corto para que un «gracias» dicho justo al hacer el parry cuente. Con la latencia de Parsec puede convenir subir `VentanaSegundos` a 4.

### Pareja por Discord
Si tu pareja habla por Discord, su voz sale por la salida de audio que Discord tenga elegida (Ajustes de usuario → Voz y vídeo → Dispositivo de salida). No tiene por qué ser la salida predeterminada de Windows: muchos cascos tienen dos salidas («auriculares» y «manos libres/comunicaciones»), y Discord suele usar la de comunicaciones.

- Por eso `DispositivoEscritorio = todos` (por defecto) escucha **todas las salidas activas** a la vez, cada una con su reconocedor y solo durante la ventana. En el log y en F6 cada una aparece numerada («escritorio1 = Auriculares…», «escritorio2 = Manos libres…»). Así se ve por cuál llega la voz de tu pareja, y se puede dejar solo esa escribiendo parte de su nombre.
- El ayudante baja el umbral de rechazo interno del reconocedor de Windows, que descartaba frases antes de aplicar `ConfianzaMinima`.
- Consejos: que el volumen de tu pareja en Discord sea alto; Discord no debe silenciar el audio («atenuación»); y si en F6 → «Últimas frases oídas» no aparece nada de ningún «escritorio» mientras ella habla, es que su audio no llega a ninguna salida capturada.

### Falsos negativos (partida del 20/11/2026)
- **Retraso del reconocedor**: Windows entrega el resultado cuando terminas de hablar, a veces más de 1 s después. Una frase dicha al final del plazo llegaba tarde y se descartaba. Ahora hay un margen (`MargenVoz`, 1,5 s) en el que se sigue aceptando y el castigo espera.
- **Primera orden perdida**: la primera línea que el plugin enviaba al ayudante llegaba con un BOM invisible y se rechazaba («Orden desconocida»). El ayudante lo limpia.
- **Audio del escritorio entrecortado**: la captura rellenaba con ceros cada lectura y metía microcortes en la voz. Ahora se entrega el audio tal como llega y solo se añade silencio cuando no suena nada.
- **Diagnóstico**: el ayudante informa también de las frases oídas por debajo de la confianza mínima y de la voz que no se parece a la frase. Se ven en F6 → Ajustes → «Últimas frases oídas».

### Garantías de privacidad
- Solo escucha entre la apertura y el cierre de una ventana de agradecimiento, o durante la prueba de micrófono (F9, 5 s). Esto vale para las dos fuentes: el audio del escritorio tampoco se captura fuera de esos momentos.
- No guarda audio ni transcripciones; el texto reconocido se usa en memoria y se descarta. Solo se escribe en el log durante la prueba o con `Diagnostico` activado.
- No usa red.
- Si el reconocedor o el micrófono fallan, la voz se marca como no disponible y **nadie es castigado** por ello.

### Limitaciones conocidas
- Usa el **micrófono predeterminado** y la **salida de audio predeterminada** de Windows; para cambiar de dispositivo, cambia el predeterminado en Windows.
- Con un solo micrófono no se distingue quién habla.
- No hay medidas de precisión todavía: hay que probar con el ruido real del juego y ajustar `ConfianzaMinima`.
- Micrófono: probado por el usuario el 19/11/2026 (funciona). Escritorio: no probado todavía.

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
