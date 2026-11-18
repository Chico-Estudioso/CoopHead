# PROMPT 00 — INVESTIGACIÓN, COPIA LOCAL Y VIABILIDAD

Trabaja inicialmente en Plan Mode. Tu misión es investigar y proponer, no instalar ni modificar archivos del juego todavía.

## Rol
Actúa como arquitecto senior de plugins Unity, desarrollador C# especializado en BepInEx/Harmony y responsable técnico de Coophead.

## Contexto cerrado del producto
Queremos un plugin para el cooperativo local de dos jugadores de Cuphead. Debe registrar muertes, reanimaciones recibidas y realizadas; premiar rescatar a la pareja con créditos de ayuda; reiniciar el nivel al alcanzar un límite; comprobar si un fantasma no fue rescatado; reconocer opcionalmente un agradecimiento («gracias», «Gracias, Cari», «Gracias, amor»…) en una ventana de tres segundos mediante procesamiento local; y aplicar una ruleta de penalizaciones temporal y adaptativa.

El sistema tiene que ser bilateral. El Jugador 2 tendrá algo más de protagonismo inicial, pero no debe ser siempre el castigado.

Penalizaciones iniciales activadas: Movimiento lento, Controles invertidos, Disparo lento, Sin penalización, Dash limitado y Castigo doble. Duración inicial de los efectos: cuatro segundos.

## 1. Inspección del entorno
Pide la ruta exacta de `Cuphead.exe` si no está disponible en el contexto. No la adivines. Inspecciona, sin modificar:
- versión/build del juego;
- arquitectura del ejecutable;
- runtime Unity y Mono/IL2CPP;
- ensamblados disponibles y sus versiones;
- presencia de BepInEx u otro loader;
- DLC y versión, si se puede detectar;
- estructura de carpetas y logs.

No copies ensamblados ni assets del juego al repositorio. Usa una ruta externa/local ignorada por Git para referencias de compilación.

## 2. Mortar frente a instalación manual
Investiga si Mortar permite seleccionar esta carpeta concreta de Cuphead y configurar BepInEx 5 sin Steam/Epic. No asumas que el botón Play funcionará con cualquier distribución.
Compara:
- Mortar con selección de carpeta;
- instalación manual de BepInEx 5 compatible;
- requisitos para ejecutar Cuphead fuera del launcher de una tienda.

No instales nada. Recomienda un procedimiento reversible para una copia de pruebas separada. La instalación principal debe permanecer intacta.

## 3. Investigación técnica del juego
Identifica el método de carga del plugin y los ensamblados que necesita. Averigua si la build es compatible con BepInEx 5 y qué target framework de C# corresponde.
Investiga los sistemas/clases relacionados con:
- PlayerOne / PlayerTwo;
- entrada y asignación de controles;
- vida, daño y muerte;
- estado de fantasma;
- reanimación por parry u otro método;
- expiración o desaparición del fantasma;
- fin de nivel, retry, transición de escena y cutscenes.

Para cada hook potencial documenta nombre exacto, ensamblado, firma, momento, parámetros, retorno, cuándo se invoca y cómo se verificará. No inventes nombres ni uses reflexión especulativa como si fuera evidencia.

## 4. Referencias comunitarias
Analiza, sin copiar código automáticamente:
- https://github.com/Meloon33/Cuphead-3-Player-Coop
- https://github.com/Germanized/CupHeads
- https://github.com/DemoJameson/Cuphead.DebugMod
- https://github.com/BepInEx/BepInEx
- https://docs.bepinex.dev/

Comprueba actividad, licencia, versiones y relevancia. No deduzcas que una feature del mod de referencia utiliza el mismo hook que necesitamos.

## 5. Reconocimiento de voz
Compara reconocimiento local en español. Requisitos:
- sin conexión;
- sin API de nube;
- sin envío de audio;
- sin grabación persistente;
- captura solo durante la ventana de agradecimiento o al probar micrófono;
- frase configurable; por defecto basta con «gracias» (ADR-004).

Explica honestamente si Vosk u otra opción utiliza modelos de ML. Investiga compatibilidad entre el runtime de Cuphead y la biblioteca elegida. Considera un proceso auxiliar local si el plugin Mono no puede cargar sus dependencias nativas de forma fiable.
Incluye fallback manual. Si no es posible garantizar el reconocimiento, no se debe castigar al usuario por un error técnico.

## 6. Arquitectura propuesta
Propón componentes separados:
- PluginBootstrap;
- GameIntegration (Harmony hooks);
- PlayerStateTracker;
- RuleEngine;
- HelpCreditService;
- BalanceDirector;
- RouletteService;
- PenaltyController;
- VoiceService;
- ConfigurationService;
- HUD/Stats.

Justifica qué partes son necesarias y qué puede mantenerse sencillo.

## 7. Entregables de investigación
Actualiza con contenido real:
- `docs/COMPATIBILIDAD.md`;
- `docs/INSTALACION_LOCAL.md`;
- `docs/ARQUITECTURA_PROPUESTA.md`;
- `docs/TERCEROS_Y_LICENCIAS.md`;
- `docs/REGLAS_DE_JUEGO.md`;
- `docs/VOZ_Y_PRIVACIDAD.md`;
- `ROADMAP.md`.

Crea una tabla de riesgos y una lista de hooks verificados/no verificados.

## 8. Restricciones
- No instalar BepInEx sin autorización.
- No alterar Cuphead.exe ni ensamblados originales.
- No usar cracks ni saltarse DRM/licencias.
- No subir DLLs del juego a GitHub.
- No habilitar acciones destructivas.
- No marcar pruebas no ejecutadas como superadas.
- No crear agentes en paralelo todavía.

## 9. Informe final
Entrega:
1. Entorno detectado.
2. Mortar: compatible/no comprobado/no recomendable y por qué.
3. BepInEx recomendado y evidencia.
4. Hooks de jugador/reanimación encontrados.
5. Plan para probar eventos sin alterar el juego.
6. Motor de voz recomendado y aclaración sobre ML.
7. Arquitectura y riesgos.
8. Pasos de M1.
9. Decisión GO/NO-GO.

Detente después del informe y espera revisión. No implementes el plugin todavía.
