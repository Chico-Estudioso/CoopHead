# Terceros, dependencias y atribuciones

Registrar cada dependencia con nombre, versión, URL, licencia, archivos redistribuidos, avisos exigidos y motivo de uso.

No asumir que un repositorio público permite redistribuir código o assets sin condiciones. Revisar licencia de cada proyecto y de sus recursos.

Referencias iniciales para investigar (no son dependencias aprobadas automáticamente):
- BepInEx: https://github.com/BepInEx/BepInEx
- Harmony: comprobar qué implementación incluye el BepInEx elegido.
- Cuphead 3-Player Co-op: https://github.com/Meloon33/Cuphead-3-Player-Coop
- CupHeads: https://github.com/Germanized/CupHeads
- Mortar: https://mortar.rethunk.tech/cuphead/
- Vosk: https://alphacephei.com/vosk/

No incluir en la release ensamblados propietarios de Cuphead ni archivos del juego.

## Dependencias actuales
| Nombre | Versión | Licencia | ¿Se redistribuye? | Motivo |
|---|---|---|---|---|
| xUnit | 2.9.2 | Apache 2.0 | No (solo pruebas) | Pruebas unitarias de `Coophead.Core`. |
| xunit.runner.visualstudio | 2.8.2 | Apache 2.0 | No (solo pruebas) | Ejecutar las pruebas con `dotnet test`. |
| Microsoft.NET.Test.Sdk | 17.11.1 | MIT | No (solo pruebas) | Infraestructura de pruebas. |
| Microsoft.NETFramework.ReferenceAssemblies (implícito) | 1.0.x | MIT | No (solo compilación) | Compilar el target `net35` fuera de Windows. |

`Coophead.Core` no tiene dependencias en tiempo de ejecución.

## Referencias leídas sin reutilizar código
- Cuphead.DebugMod y CupHeads no publican licencia: se consultan solo para documentar nombres de API del juego. No se copia su código.
- Cuphead 3-Player Co-op es MIT: se ha usado como referencia de hooks; si en el futuro se adapta código suyo, se conservará su aviso de copyright.
