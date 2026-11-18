# PROMPT 07 — INTEGRACIÓN, QA Y RELEASE CANDIDATE

## Objetivo
Integrar los módulos aprobados y preparar una versión candidata para el usuario. No publicar automáticamente.

## Revisión previa
- Verifica PRs y ramas pendientes.
- Comprueba que interfaces entre módulos están documentadas.
- Revisa duplicación de hooks y orden de inicialización.
- Comprueba dependencias y licencias.

## Matriz de pruebas
1. Cuphead inicia sin errores con el plugin.
2. Cuphead inicia sin el plugin.
3. Partida local de dos jugadores.
4. P1 muere y P2 lo rescata.
5. P2 muere y P1 lo rescata.
6. Créditos de ayuda en ambos sentidos.
7. Umbral de reanimaciones y reinicio.
8. Fantasma no rescatado, solo si el evento está verificado.
9. Cada penalización individual.
10. Castigo doble con combinación compatible.
11. Sin penalización.
12. Protección adaptativa para P1 y P2.
13. Agradecimiento de voz y fallback manual.
14. Micrófono no disponible.
15. Cambio de escena/retry mientras hay un efecto activo.
16. Salida del juego durante la penalización.
17. Menús y controles no quedan afectados por controles invertidos.

## Reglas de QA
- No inventar pruebas manuales.
- Separar compilación, pruebas unitarias, integración y juego real.
- Guardar logs redactados, sin rutas personales ni audio.
- No considerar aprobado el reinicio hasta comprobarlo en varios niveles.
- Si el reconocimiento de voz no es fiable, desactivarlo por defecto o dejarlo experimental.

## Empaquetado
- Publicar solo DLLs propias y dependencias con redistribución aprobada.
- No incluir Cuphead.exe, Assembly-CSharp.dll, UnityEngine u otros ensamblados del juego.
- Incluir README de instalación y desinstalación.
- Documentar loader/runtime requeridos.
- Añadir versión del plugin y compatibilidad probada.

## Git
Crear rama `release/0.1.0-rc1` (ajusta versión según proyecto). No hacer merge a main ni publicar sin aprobación expresa.

## Informe
Entregar matriz de pruebas, fallos conocidos, logs de prueba, versión exacta del juego, BepInEx y runtime. Solicitar aprobación del usuario para cerrar la release.
