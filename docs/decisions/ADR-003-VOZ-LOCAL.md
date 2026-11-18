# ADR-003 — Reconocimiento de voz local

Estado: Propuesto, pendiente de pruebas y aclaración tecnológica.

Requisitos: sin nube, sin envío de audio, sin grabaciones persistentes. La frase inicial era «Gracias, Kari»; desde ADR-004 basta con «gracias» (con «Cari», «amor» o cualquier apelativo) y vale para ambos jugadores. La ventana dura tres segundos. Se documentará si la solución usa ML local. Si el usuario exige ausencia absoluta de modelos de ML, se evaluará detección de palabra clave y se explicarán sus límites. Siempre debe poder desactivarse y disponer de fallback.
