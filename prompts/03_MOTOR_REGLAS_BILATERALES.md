# PROMPT 03 — MOTOR DE REGLAS BILATERALES Y CRÉDITOS DE AYUDA

## Objetivo
Implementar reglas internas desacopladas de los hooks del juego. El motor recibe eventos verificados y actualiza estadísticas por jugador y nivel.

## Reglas acordadas
- Cooperativo local de dos jugadores.
- Contadores separados por jugador y nivel.
- Registrar muertes, reanimaciones recibidas y reanimaciones realizadas.
- Si A rescata a B, A recibe una recompensa: reducir en uno su contador propio de reanimaciones recibidas. Si su contador está en cero, guardar un crédito de ayuda.
- Máximo un crédito almacenado por jugador y nivel.
- Cuando B recibe una reanimación, consumir primero un crédito de ayuda suyo; si no tiene, incrementar `revivesReceived`.
- Créditos y contadores del nivel se reinician al comenzar otro nivel.
- La misma regla se aplica a P1 y P2. El foco predeterminado puede dar algo más de protagonismo a P2, pero no puede convertirlo en objetivo fijo.
- Límite inicial sugerido: tres reanimaciones recibidas por nivel, configurable.
- Al alcanzar el límite, solicitar reinicio del nivel únicamente mediante el hook verificado.

## Diseño
Implementa servicios puros cuando sea posible:
- `PlayerRoundStats`;
- `HelpCreditService`;
- `RuleEngine`;
- `LevelSessionState`.

Los objetos deben poder probarse sin cargar Unity ni Cuphead.

## Casos que deben probarse
1. P2 recibe 2 reanimaciones; P2 rescata a P1: contador P2 baja a 1.
2. P2 está a 0; rescata a P1: guarda 1 crédito.
3. P2 recibe después una reanimación: consume el crédito y permanece en 0.
4. P2 no puede acumular un segundo crédito si ya tiene uno.
5. P1 y P2 pueden rescatarse mutuamente y reciben el mismo trato.
6. Un evento duplicado no aplica dos recompensas ni dos reanimaciones.
7. Cambio de nivel limpia contadores/créditos.
8. Llegar al umbral activa una sola solicitud de reinicio.
9. Reinicio manual y final de combate limpian el estado correctamente.

## Seguridad
No alterar vidas internas de Cuphead. No guardar contadores en el save game sin una decisión aparte. Si un hook no es fiable, no activar reinicio automático.

## Entrega
Pruebas unitarias completas, documentación de reglas actualizada y PR sin merge.
