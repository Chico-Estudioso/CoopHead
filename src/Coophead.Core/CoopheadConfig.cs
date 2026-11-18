using System.Collections.Generic;
using Coophead.Core.Roulette;
using Coophead.Core.Rules;
using Coophead.Core.Thanks;

namespace Coophead.Core
{
    /// <summary>A quién se aplica la ruleta cuando falta el agradecimiento.</summary>
    public enum PenaltyTargetMode
    {
        /// <summary>Al jugador rescatado que no dio las gracias (por defecto).</summary>
        Rescued,

        /// <summary>A quien rescató.</summary>
        Rescuer,
    }

    /// <summary>Configuración completa del Core. La capa BepInEx (M6) la rellena desde su archivo de configuración.</summary>
    public sealed class CoopheadConfig
    {
        public CoopheadConfig()
        {
            Rules = new RulesConfig();
            Roulette = new RouletteConfig();
            Balance = new BalanceConfig();
            Thanks = new ThanksConfig();
        }

        public RulesConfig Rules { get; private set; }

        public RouletteConfig Roulette { get; private set; }

        public BalanceConfig Balance { get; private set; }

        public ThanksConfig Thanks { get; private set; }

        public PenaltyTargetMode PenaltyTarget { get; set; } = PenaltyTargetMode.Rescued;

        /// <summary>Valida y corrige todas las secciones; devuelve los avisos en español.</summary>
        public IList<string> Validate()
        {
            var warnings = new List<string>();
            warnings.AddRange(Rules.Validate());
            warnings.AddRange(Roulette.Validate());
            warnings.AddRange(Balance.Validate());
            warnings.AddRange(Thanks.Validate());
            return warnings;
        }
    }
}
