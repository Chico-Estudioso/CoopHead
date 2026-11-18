# PROMPT 04 — RULETA DE PENALIZACIONES Y BALANCE ADAPTATIVO

## Objetivo
Diseñar e implementar un sistema temporal de penalizaciones divertido y equilibrado para ambos jugadores.

## Resultados habilitados inicialmente
1. Movimiento lento.
2. Controles invertidos.
3. Disparo lento.
4. Sin penalización.
5. Dash limitado.
6. Castigo doble.

Todos empiezan activados. Duración inicial de cada efecto temporal: cuatro segundos. La duración debe poder cambiarse globalmente y por efecto.

## Ruleta
- Cada resultado tiene peso configurable.
- Normaliza pesos para calcular probabilidades.
- Permite activar/desactivar resultados.
- Evita el mismo resultado dos veces seguidas para el mismo jugador cuando haya alternativas.
- Incluye cooldown configurable para que no se encadenen efectos en un intervalo absurdo.
- `Sin penalización` cuenta como resultado válido.
- `Castigo doble` elige dos efectos temporales distintos y compatibles; nunca elige `Sin penalización` como segundo efecto.
- Mantén semilla/resultado de debug opcional para reproducir incidencias, sin hacer la ruleta predecible en uso normal.

Pesos orientativos iniciales: Movimiento lento 20; Controles invertidos 15; Disparo lento 15; Sin penalización 20; Dash limitado 15; Castigo doble 15. Los pesos son defaults editables, no una promesa de balance final.

## Aplicación de castigos
- El objetivo predeterminado es el jugador que acaba de ser rescatado y no ha agradecido.
- Toda penalización tiene inicio, duración y restauración.
- Restaurar valores originales, no valores hardcodeados, al terminar.
- Limpiar efectos al cambiar de nivel, reiniciar, salir de la partida o desactivar el mod.
- No combinar efectos incompatibles.
- No deshabilitar todos los controles ni bloquear el juego.
- Si no se puede modificar un efecto de forma segura, dejarlo desactivado y explicar por qué.

## Balance Director
Implementa un director adaptativo transparente y bilateral.

Entradas permitidas:
- muertes del jugador en el nivel;
- reanimaciones recibidas después de créditos;
- reanimaciones realizadas;
- penalizaciones recientes y consecutivas;
- créditos de ayuda disponibles.

No uses IA ni perfiles ocultos para inferir habilidad. La fórmula debe ser simple, documentada y visible en configuración.

Comportamiento:
- Si un jugador acumula presión alta, aumenta la probabilidad de `Sin penalización` para él.
- Reduce peso de `Castigo doble` cuando su presión es alta.
- Reduce duración de los efectos dentro de límites configurables.
- Evita más de dos penalizaciones consecutivas; después aplica una tirada protegida o `Sin penalización`.
- Nunca incrementa automáticamente la presión del otro jugador para compensar.
- Los créditos por rescatar a la pareja reducen la presión según la regla aprobada.
- El sistema se puede desactivar y vuelve a probabilidades base.

## Pruebas
- Pesos cero y pesos inválidos.
- Solo un resultado habilitado.
- Castigo doble sin dos efectos compatibles.
- Presión alta y baja para ambos jugadores.
- No repetición y cooldown.
- Restauración después de nivel/retry/excepción.
- Controles invertidos no afectan menús ni pausas.
- Dash limitado no rompe estados de dash existentes.

## Entrega
Documenta fórmula, probabilidades base, reglas de protección y limitaciones de hooks. PR sin merge.
