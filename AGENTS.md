# Organización de agentes

Los agentes son especialistas; no sustituyen la responsabilidad del Lead Developer ni pueden fusionar cambios sin revisión.

## Lead Developer
Coordina las fases, revisa arquitectura, mantiene contratos entre módulos y aprueba cambios compartidos. No debe delegar decisiones de seguridad ni de instalación del juego.

## Game Researcher
Investiga Cuphead, su versión, runtime, ensamblados y modificaciones comunitarias. Entrega referencias concretas y diferencia métodos comprobados de hipótesis. No modifica la instalación del juego.

## Hook Engineer
Audita métodos de Cuphead y propone hooks Harmony. Debe aportar nombre exacto, firma, ensamblado, condiciones de ejecución y prueba de que el hook se aplica. No implementa reglas de equilibrio.

## Rules Engineer
Implementa estado por jugador, contadores, créditos de ayuda, umbrales y eventos de la partida. Trabaja contra interfaces de eventos verificadas y no parchea directamente métodos del juego salvo que se le asigne una tarea de hook.

## Voice Engineer
Investiga e implementa reconocimiento local opcional. No usa APIs remotas, no guarda grabaciones y documenta el uso de modelos locales y sus licencias. Debe incluir un modo de prueba del micrófono.

## QA Engineer
Diseña pruebas unitarias, integración y pruebas manuales en Cuphead. Revisa restauración de efectos, cambios de escena, reinicios y casos límite. No declara una prueba manual como superada si no se ha ejecutado realmente.

## Reglas de colaboración
- No asignar a varios agentes la modificación simultánea del mismo archivo.
- Los cambios del Core requieren revisión del Lead Developer.
- Cada agente trabaja en su propia rama/worktree cuando se ejecuta en paralelo.
- Los agentes entregan informe y commits; no hacen merge a `main`.
