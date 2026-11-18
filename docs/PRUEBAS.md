# Estrategia de pruebas

## Cómo ejecutarlas
```bash
dotnet test Coophead.sln -c Release
```
La CI (`.github/workflows/build-test.yml`) compila `Coophead.Core` para `net35` y `netstandard2.0` y ejecuta las pruebas en cada PR.

## Unitarias (implementadas en `tests/Coophead.Core.Tests`)
- Contadores y reinicio por nivel.
- Créditos de ayuda y límite máximo de uno.
- Orden de aplicación de créditos al revivir.
- Umbral de reanimaciones.
- Pesos normalizados de la ruleta.
- Doble castigo con dos efectos compatibles distintos.
- Director adaptativo y protección por presión.
- Cooldown y no repetición.
- Restauración de valores originales tras expirar un efecto.
- Límite de equipo con reanimaciones alternas.
- Deduplicación y eventos incoherentes.
- Frase de agradecimiento: normalización, variantes y apelativos.
- Ventanas: expiración, orden de llegada, sustitución, fallo técnico y respaldo manual.
- Flujo completo de `CoopheadSession`.

Las pruebas unitarias no sustituyen a las de integración ni a las manuales en el juego.

## Integración
- Identificación de PlayerOne y PlayerTwo.
- Muerte y creación del fantasma.
- Reanimación por cada método real del juego.
- Fantasma que se pierde sin rescate.
- Cambio de escena, salida a mapa, retry y final de nivel.
- Cambios de mando/configuración si afectan a la identificación del jugador.

## Manuales
- Al menos un nivel de plataformas.
- Al menos un jefe.
- Ambos jugadores mueren y rescatan por turnos.
- Se alcanza el límite y se solicita retry.
- Se cambia de escena antes de que termine un efecto.
- Se cierra el juego con un efecto activo.
- Micrófono desconectado, ruido, frase correcta y frase no reconocida.

Toda prueba manual debe registrar build del juego, configuración, pasos, resultado y log relevante.
