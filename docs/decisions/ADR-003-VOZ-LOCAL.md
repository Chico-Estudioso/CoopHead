# ADR-003 — Reconocimiento de voz local

Estado: Aceptado como experimental (18/11/2026), pendiente de pruebas en un equipo real.

Decisión: reconocedor de voz de escritorio de Windows (`System.Speech`) en un proceso auxiliar local con gramática cerrada, desactivado por defecto y con respaldo por tecla. Detalles y aclaración sobre ML en `docs/VOZ_Y_PRIVACIDAD.md`.

Requisitos: sin nube, sin envío de audio, sin grabaciones persistentes. La frase inicial era «Gracias, Kari»; desde ADR-004 basta con «gracias» (con «Cari», «amor» o cualquier apelativo) y vale para ambos jugadores. La ventana dura tres segundos. Se documentará si la solución usa ML local. Si el usuario exige ausencia absoluta de modelos de ML, se evaluará detección de palabra clave y se explicarán sus límites. Siempre debe poder desactivarse y disponer de fallback.
