using System.Text;

namespace Coophead.Core.Stats
{
    /// <summary>
    /// Nombre legible de un nivel a partir del tipo que da el juego («FlowerLevel» → «Flower»,
    /// «DicePalaceMainLevel» → «Dice Palace Main»). No usa nombres inventados: solo reformatea el identificador.
    /// </summary>
    public static class LevelNames
    {
        public static string Display(string levelKey)
        {
            if (string.IsNullOrEmpty(levelKey))
            {
                return "Nivel desconocido";
            }

            string name = levelKey;
            if (name.EndsWith("Level") && name.Length > "Level".Length)
            {
                name = name.Substring(0, name.Length - "Level".Length);
            }

            var builder = new StringBuilder(name.Length + 8);
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                bool boundary = i > 0 && char.IsUpper(c) && (char.IsLower(name[i - 1]) || (i + 1 < name.Length && char.IsLower(name[i + 1]) && char.IsUpper(name[i - 1])));
                if (boundary)
                {
                    builder.Append(' ');
                }

                builder.Append(c);
            }

            return builder.ToString();
        }
    }
}
