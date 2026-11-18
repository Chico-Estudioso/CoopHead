using System.Collections.Generic;
using UnityEngine;

namespace Coophead.Plugin
{
    public enum HudCorner
    {
        SuperiorIzquierda,
        SuperiorDerecha,
        InferiorIzquierda,
        InferiorDerecha,
    }

    /// <summary>
    /// Caja de texto IMGUI en una esquina. No captura entrada ni bloquea controles: solo dibuja.
    /// </summary>
    internal sealed class HudOverlay
    {
        private const float Margin = 10f;
        private GUIStyle style;
        private int styleFontSize;

        /// <summary>Solo se puede llamar desde OnGUI.</summary>
        public void Draw(IList<string> lines, HudCorner corner, int fontSize, float verticalOffset)
        {
            if (lines == null || lines.Count == 0)
            {
                return;
            }

            if (style == null || styleFontSize != fontSize)
            {
                style = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.UpperLeft,
                    fontSize = fontSize,
                    wordWrap = false,
                    richText = false,
                    padding = new RectOffset(8, 8, 6, 6),
                };
                styleFontSize = fontSize;
            }

            var text = new string[lines.Count];
            lines.CopyTo(text, 0);
            var content = new GUIContent(string.Join("\n", text));
            Vector2 size = style.CalcSize(content);

            bool left = corner == HudCorner.SuperiorIzquierda || corner == HudCorner.InferiorIzquierda;
            bool top = corner == HudCorner.SuperiorIzquierda || corner == HudCorner.SuperiorDerecha;
            float x = left ? Margin : Screen.width - size.x - Margin;
            float y = top ? Margin + verticalOffset : Screen.height - size.y - Margin - verticalOffset;
            GUI.Box(new Rect(x, y, size.x, size.y), content, style);
        }

        public static float Height(IList<string> lines, int fontSize)
        {
            return lines == null ? 0f : (lines.Count * (fontSize + 4f)) + 16f;
        }
    }
}
