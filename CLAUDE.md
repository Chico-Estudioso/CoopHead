# Instrucciones del proyecto — Coophead

Lee `AGENTS.md`, `ROADMAP.md` y los documentos relevantes de `docs/` antes de modificar código.

## Idioma y comunicación
- Responde y documenta en español.
- Mantén en inglés los identificadores de código, namespaces, clases, métodos, propiedades y nombres de APIs cuando sea la convención técnica.
- Explica los términos técnicos cuando sea útil.
- Distingue hechos verificados, hipótesis y cuestiones pendientes.

## Objetivo
Desarrollar un plugin BepInEx para Cuphead que añada reglas configurables al cooperativo local de dos jugadores: seguimiento de muertes y reanimaciones, créditos de ayuda bilaterales, reinicios, agradecimiento por voz local, ruleta de penalizaciones adaptativa y estadísticas.

## Reglas obligatorias
1. No trabajes directamente sobre `main`.
2. Cada tarea se realiza en una rama `feat/...`, `fix/...`, `docs/...` o `chore/...`. En sesiones de Claude Code en la nube se usa la rama de sesión asignada (`claude/...`).
3. No hagas merge de Pull Requests sin aprobación expresa del usuario.
4. No uses `git push --force`, `reset --hard`, `clean -fd` ni operaciones destructivas sin autorización expresa.
5. No cambies configuración global de Git ni credenciales.
6. No instales, reemplaces ni borres archivos de Cuphead sin autorización expresa y copia de seguridad verificada.
7. No incluyas ejecutables, ensamblados propietarios del juego, archivos de guardado, grabaciones, modelos de voz o rutas privadas en Git.
8. No inventes nombres de clases, métodos, campos, eventos ni rutas de Cuphead. Verifica los hooks contra los ensamblados exactos del usuario.
9. No confundas pruebas unitarias o compilación con pruebas reales dentro del juego.
10. Si un hook no es fiable, no apliques reglas que puedan reiniciar una partida o penalizar a un jugador; desactiva esa función y explica el motivo.
11. No envíes audio a Internet ni uses APIs de voz en la nube. No guardes grabaciones. La voz debe ser opcional y local.
12. No afirmes que el reconocimiento de voz carece de IA/aprendizaje automático si utiliza un modelo de ese tipo. Describe la tecnología con honestidad.
13. No redistribuyas bibliotecas o código de terceros sin comprobar su licencia y condiciones.
14. Mantén las reglas de juego separadas de los hooks y de la interfaz.
15. Todo efecto temporal debe restaurar el estado original incluso si el nivel cambia, el jugador muere o se produce un error.

## Estructura y comandos
- `src/Coophead.Core`: lógica pura (`net35` + `netstandard2.0`). No puede referenciar Unity, BepInEx ni ensamblados de Cuphead, y debe seguir compilando para `net35` (sin `Task`, `Tuple`, `IReadOnlyList`, `string.IsNullOrWhiteSpace`, etc.).
- `tests/Coophead.Core.Tests`: pruebas xUnit (`net8.0`).
- Compilar: `dotnet build Coophead.sln -c Release`.
- Probar: `dotnet test Coophead.sln -c Release`.
- Hooks candidatos y su estado: `docs/GAME_HOOKS.md`. Decisiones: `docs/decisions/`.

## Git y Pull Requests
- `main` es la rama estable y protegida.
- Usa ramas de corta duración y commits pequeños con mensajes claros.
- Antes de crear una PR, ejecuta las pruebas disponibles y revisa `git diff`.
- La PR debe explicar alcance, pruebas realizadas, limitaciones y pasos manuales pendientes.
- No fusiones ni publiques releases sin aprobación.

## Calidad de implementación
- Prefiere componentes pequeños y responsabilidades claras.
- Conserva compatibilidad con el runtime real de Cuphead y BepInEx detectado en M0.
- No elijas un target framework por costumbre: comprueba las dependencias disponibles.
- Evita dependencias innecesarias.
- Usa identificadores de jugador estables según la API verificada; no supongas que los índices o nombres internos son fijos.
- Configura límites, temporizadores y probabilidades mediante configuración validada.
- Añade logs útiles sin registrar datos personales ni audio.
- Actualiza `CHANGELOG.md`, `ROADMAP.md` y documentación afectada.

## Definición de hecho
Una tarea no está terminada solo porque el código compile. Debe cumplir sus criterios de aceptación, incluir pruebas apropiadas, registrar las limitaciones y documentar qué no se ha podido verificar en el juego.
