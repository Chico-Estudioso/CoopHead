# Informe M0 — Investigación de escritorio

Fecha: 18 de noviembre de 2026. Alcance: lo que se puede investigar **sin acceso al equipo del usuario**. La inspección de la instalación local sigue pendiente.

## 1. Entorno detectado
No inspeccionado: la sesión de trabajo se ejecuta en la nube y no tiene acceso al equipo ni a la carpeta de Cuphead. Pendiente: ruta de `Cuphead.exe`, versión, arquitectura, Unity, backend, DLC y ensamblados.

## 2. Mortar
No comprobado. Su web indica que permite elegir una carpeta de juego y que, para Cuphead, gestiona BepInEx 5. Hay que probarlo con la copia de pruebas; si no la detecta, se usa la instalación manual.

## 3. BepInEx recomendado
BepInEx **5.4.x**, build **x64** si la versión es 1.3 o posterior (x86 para versiones antiguas). Evidencia: los dos plugins comunitarios activos usan BepInEx 5 con target `net35`, y 3-Player Co-op exige 5.4.x x64. DebugMod incluye un `Cuphead_Vanilla.bat` para arrancar sin plugins y recomienda un `steam_appid.txt` si el plugin no carga en Windows.

## 4. Hooks de jugador y reanimación
Ver `docs/GAME_HOOKS.md`. Hay candidatos claros para inicio y fin de nivel, muerte, rescate por parry y reinicio. Hallazgo importante: el fantasma no caduca; fuera de cámara permite un **robo de corazón** que también reanima. Queda fuera de alcance por decisión del usuario, pero la integración debe distinguirlo para no contarlo como rescate.

## 5. Plan para probar eventos sin alterar el juego
Parches Harmony de solo registro (postfix), activados por configuración, con clave por evento y número de sesión. Matriz manual descrita en `docs/GAME_HOOKS.md`. Las reglas que reinician o castigan permanecen desactivadas hasta marcar los hooks como verificados.

## 6. Voz
Sin decidir motor. Candidatos: reconocedor de Windows (SAPI, `System.Speech`, gramática cerrada) o Vosk con modelo pequeño en español. Ambos usan modelos estadísticos o de aprendizaje automático: no se describirán como «sin IA». Ninguno se puede cargar de forma fiable dentro de un plugin `net35` con dependencias nativas, así que lo previsto es un **proceso auxiliar local** que solo devuelva texto. La lógica de coincidencia de la frase ya está en `Coophead.Core`.

## 7. Arquitectura y riesgos
Arquitectura en `docs/ARQUITECTURA_PROPUESTA.md`. La lógica pura ya está implementada y probada.

| Riesgo | Impacto | Probabilidad | Mitigación |
|---|---|---|---|
| La versión local no coincide con la de los mods de referencia | Alto | Media | Resolver hooks por nombre en tiempo de ejecución y desactivar la función si no existen. |
| Un hook se dispara dos veces o en escenas no jugables | Alto | Media | Claves de deduplicación y comprobación de estado en el Core. |
| Confundir robo de corazón con rescate | Medio | Alta | Usar el hook del parry, no `OnRevive` a secas. |
| Reinicio por una vía no fiable | Alto | Baja | `AutoRestartEnabled = false` hasta verificar `SceneLoader.ReloadLevel`. |
| Efecto que no se restaura | Alto | Media | `PenaltyScheduler` restaura en todos los cierres; cada efecto guarda el valor original. |
| Controles invertidos afectan a menús | Medio | Media | Aplicar solo al motor del jugador en nivel, nunca al input global. |
| Voz poco precisa con ruido del juego | Medio | Alta | Gramática cerrada, respaldo manual y sin castigo por fallo técnico. |
| Licencias de referencias | Medio | Baja | Solo se reutiliza código con licencia compatible; las referencias sin licencia solo se leen. |

## 8. Pasos de M1
1. El usuario indica la versión de Cuphead y prepara una copia de pruebas.
2. Instala BepInEx 5.4.x (x64 si es 1.3+) en esa copia y arranca una vez.
3. Copia `Coophead.dll` y `Coophead.Core.dll` en `BepInEx/plugins/Coophead/`.
4. Comprueba en `BepInEx/LogOutput.log` el mensaje de carga.

## 9. Decisión
**GO condicionado** para M1 y la instrumentación de M2: el plugin se puede compilar sin ensamblados del juego y los hooks se resuelven en tiempo de ejecución con todas las reglas desactivadas por defecto. **NO-GO** para activar reinicios o castigos en el juego hasta completar la verificación de M2 en la copia local.
