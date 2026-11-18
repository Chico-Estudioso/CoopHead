namespace Coophead.Core.Rules
{
    /// <summary>Recompensa que recibe quien rescata a su pareja.</summary>
    public enum RescuerReward
    {
        /// <summary>Créditos desactivados: solo se cuenta el rescate.</summary>
        None,

        /// <summary>Tenía reanimaciones recibidas netas: se le resta una.</summary>
        CounterReduced,

        /// <summary>Su contador estaba a cero: guarda un crédito.</summary>
        CreditStored,

        /// <summary>Contador a cero y ya tenía el máximo de créditos: no gana nada más.</summary>
        CreditCapReached,
    }

    /// <summary>
    /// Créditos de ayuda bilaterales (ADR-002). Solo modifica contadores del mod.
    /// </summary>
    public sealed class HelpCreditService
    {
        private readonly RulesConfig config;

        public HelpCreditService(RulesConfig config)
        {
            this.config = config;
        }

        /// <summary>
        /// Registra la reanimación recibida por el jugador rescatado. Si tiene un crédito guardado,
        /// lo consume y su contador neto no sube.
        /// </summary>
        /// <returns>true si se consumió un crédito.</returns>
        public bool RegisterReviveReceived(PlayerRoundStats rescued)
        {
            rescued.TotalRevivesReceived++;
            if (config.HelpCreditsEnabled && rescued.HelpCredits > 0)
            {
                rescued.HelpCredits--;
                rescued.CreditsConsumed++;
                return true;
            }

            rescued.RevivesReceived++;
            return false;
        }

        /// <summary>
        /// Registra el rescate y recompensa al rescatador: resta una reanimación recibida propia o,
        /// si está a cero, guarda un crédito hasta el máximo configurado.
        /// </summary>
        public RescuerReward RewardRescuer(PlayerRoundStats rescuer)
        {
            rescuer.RevivesGiven++;
            if (!config.HelpCreditsEnabled)
            {
                return RescuerReward.None;
            }

            if (rescuer.RevivesReceived > 0)
            {
                rescuer.RevivesReceived--;
                rescuer.CounterReductions++;
                return RescuerReward.CounterReduced;
            }

            if (rescuer.HelpCredits < config.MaxStoredCredits)
            {
                rescuer.HelpCredits++;
                rescuer.CreditsEarned++;
                return RescuerReward.CreditStored;
            }

            return RescuerReward.CreditCapReached;
        }
    }
}
