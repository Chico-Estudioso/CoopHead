# Fuentes iniciales para M0

Las siguientes fuentes se localizaron durante la preparación del kit. Son puntos de partida; Claude debe volver a comprobar su vigencia, versiones y licencias.

- Mortar para Cuphead: https://mortar.rethunk.tech/cuphead/
- Mortar general: https://mortar.rethunk.tech/
- Documentación de instalación BepInEx: https://docs.bepinex.dev/articles/user_guide/installation/
- BepInExPack Cuphead / Mono Unity: https://thunderstore.io/c/cuphead/p/BepInEx/BepInExPack/
- BepInEx específico para Cuphead: https://www.nexusmods.com/cuphead/mods/173
- Cuphead 3-Player Co-op (referencia de funciones): https://github.com/Meloon33/Cuphead-3-Player-Coop
- CupHeads (referencia BepInEx/Harmony): https://github.com/Germanized/CupHeads
- Cuphead.DebugMod: https://github.com/DemoJameson/Cuphead.DebugMod
- Vosk: https://alphacephei.com/vosk/

Notas de investigación al 18 de noviembre de 2026:
- Mortar describe soporte para elegir una carpeta de juego y la ficha específica de Cuphead indica BepInEx 5; la detección de una instalación local concreta aún debe probarse.
- BepInEx debe seleccionarse según arquitectura y runtime. Su documentación advierte que los juegos IL2CPP no están cubiertos por las releases estables habituales; verificar si Cuphead local usa Mono.
- El repositorio CupHeads documenta uso de BepInEx y Harmony, pero algunas de sus instrucciones pueden estar dirigidas a Steam; no asumir dependencia cero de Steam en todos los proyectos comunitarios.
