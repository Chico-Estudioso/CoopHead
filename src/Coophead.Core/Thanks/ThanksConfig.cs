using System.Collections.Generic;

namespace Coophead.Core.Thanks
{
    /// <summary>Configuración del agradecimiento tras una reanimación (docs/VOZ_Y_PRIVACIDAD.md).</summary>
    public sealed class ThanksConfig
    {
        public ThanksConfig()
        {
            AcceptedNames = new List<string> { "cari", "kari", "amor" };
        }

        /// <summary>
        /// Exigir agradecimiento. Si es false no se abre ventana y, por tanto, nunca se tira la ruleta.
        /// </summary>
        public bool RequireThanks { get; set; } = true;

        /// <summary>Duración de la ventana desde la reanimación confirmada.</summary>
        public float WindowSeconds { get; set; } = 3f;

        /// <summary>Palabra obligatoria. Por defecto «gracias».</summary>
        public string RequiredWord { get; set; } = "gracias";

        /// <summary>
        /// Apelativos reconocidos junto a «gracias» («Gracias, Cari», «Gracias, amor»…). Valen para los dos
        /// jugadores. Con <see cref="RequireName"/> a false solo sirven para construir la gramática del reconocedor.
        /// </summary>
        public List<string> AcceptedNames { get; set; }

        /// <summary>Si es true, hace falta «gracias» más uno de los apelativos; si es false, basta con «gracias».</summary>
        public bool RequireName { get; set; }

        /// <summary>Permite confirmar el agradecimiento con una tecla o botón.</summary>
        public bool ManualFallbackEnabled { get; set; } = true;

        public IList<string> Validate()
        {
            var warnings = new List<string>();
            if (float.IsNaN(WindowSeconds) || WindowSeconds < 1f || WindowSeconds > 15f)
            {
                warnings.Add("Ventana de agradecimiento fuera de [1, 15] s; se usan 3 s.");
                WindowSeconds = 3f;
            }

            if (ThanksPhraseMatcher.Normalize(RequiredWord).Length == 0)
            {
                warnings.Add("La palabra de agradecimiento está vacía; se usa «gracias».");
                RequiredWord = "gracias";
            }

            if (AcceptedNames == null)
            {
                AcceptedNames = new List<string>();
            }

            AcceptedNames.RemoveAll(name => ThanksPhraseMatcher.Normalize(name).Length == 0);
            if (RequireName && AcceptedNames.Count == 0)
            {
                warnings.Add("Se exige apelativo pero la lista está vacía; basta con «" + RequiredWord + "».");
                RequireName = false;
            }

            return warnings;
        }
    }
}
