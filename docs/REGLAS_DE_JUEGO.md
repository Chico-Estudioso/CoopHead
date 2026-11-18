# Reglas de juego acordadas

## Contadores por nivel y jugador
- `deaths`: muertes detectadas.
- `revivesReceived`: reanimaciones recibidas después de aplicar un crédito pendiente.
- `revivesGiven`: rescates realizados.
- `helpCredits`: créditos de ayuda disponibles (0 o 1).
- `penaltiesReceived`: penalizaciones recientes.
- `consecutivePenalties`: resultados consecutivos distintos de «Sin penalización».

Estos contadores pertenecen al mod, no sustituyen ni modifican las vidas internas de Cuphead.

## Crédito bilateral de ayuda
Cuando A rescata a B:
1. Se registra un rescate realizado por A.
2. Se registra una reanimación recibida por B; si B tenía un crédito disponible, se consume y esa reanimación no aumenta su contador.
3. A recibe una recompensa: si `revivesReceived[A] > 0`, se resta uno. Si es cero, obtiene un crédito, con máximo uno guardado.
4. La operación solo afecta a contadores del mod.
5. Los créditos y contadores se reinician al cambiar de nivel.

Esta interpretación concreta el requisito del usuario: si P2 rescata a P1, baja la carga de reanimaciones recibidas de P2. La misma regla se aplica a P1 para que el sistema sea bilateral.

## Límite
El límite inicial recomendado es 3 reanimaciones recibidas por nivel, configurable por jugador o globalmente. Al alcanzar el umbral, el mod solicita reiniciar el nivel mediante una vía verificada. Si no puede reiniciar con seguridad, muestra aviso y no ejecuta una acción destructiva.

## Fantasma no rescatado
Contar el evento únicamente si se demuestra que el estado del fantasma terminó por expiración/fallo de rescate, no por transición de escena, carga, cutscene o reinicio.

## Agradecimiento
- Ventana: 3 segundos desde una reanimación confirmada.
- Frase inicial: «Gracias, Kari».
- La frase debe ser configurable.
- Un reconocimiento válido evita la ruleta de ese evento.
- Si la voz está deshabilitada, el micrófono no está disponible o el reconocedor falla, la opción de exigir agradecimiento debe poder desactivarse o utilizar un fallback manual.

## Ruleta inicial
Todos estos resultados empiezan habilitados:
- Movimiento lento.
- Controles invertidos.
- Disparo lento.
- Sin penalización.
- Dash limitado.
- Castigo doble.

Duración inicial: 4 segundos para los efectos temporales, editable por efecto.

Pesos orientativos iniciales (sujetos a pruebas): Movimiento lento 20; Controles invertidos 15; Disparo lento 15; Sin penalización 20; Dash limitado 15; Castigo doble 15. Los pesos se normalizan y se muestran en configuración.

Castigo doble selecciona dos efectos distintos entre los efectos temporales habilitados. No puede seleccionar «Sin penalización» como segundo castigo ni combinar efectos que la auditoría declare incompatibles.

## Adaptación
`BalanceDirector` usa una medida sencilla y explicable de presión individual: muertes, reanimaciones recibidas, penalizaciones recientes y rescates realizados. La fórmula debe documentarse, configurarse y poder desactivarse.

Cuando alguien acumula presión alta:
- Aumenta el peso de «Sin penalización» para ese jugador.
- Reduce el peso de «Castigo doble».
- Puede reducir la duración de efectos.
- Evita encadenar castigos.
- Nunca aumenta automáticamente el castigo del compañero como compensación.

El jugador que recibe el efecto tras un agradecimiento fallido es, por defecto, quien acaba de ser rescatado. La configuración podrá cambiar el objetivo, pero el comportamiento predeterminado debe ser coherente y bilateral.
