namespace Coophead.Core
{
    /// <summary>
    /// Identificador estable que usa el mod para cada jugador.
    /// La traducción desde el identificador interno del juego se hace en la capa de integración
    /// (GameIntegration), nunca aquí: el Core no conoce ningún tipo de Cuphead.
    /// </summary>
    public enum PlayerSlot
    {
        PlayerOne = 0,
        PlayerTwo = 1,
    }

    public static class PlayerSlots
    {
        /// <summary>Los dos jugadores, en orden P1, P2.</summary>
        public static PlayerSlot[] All()
        {
            return new[] { PlayerSlot.PlayerOne, PlayerSlot.PlayerTwo };
        }

        /// <summary>Devuelve la pareja del jugador indicado.</summary>
        public static PlayerSlot Partner(this PlayerSlot slot)
        {
            return slot == PlayerSlot.PlayerOne ? PlayerSlot.PlayerTwo : PlayerSlot.PlayerOne;
        }

        public static bool IsDefined(PlayerSlot slot)
        {
            return slot == PlayerSlot.PlayerOne || slot == PlayerSlot.PlayerTwo;
        }

        /// <summary>Etiqueta corta para logs y HUD: «P1» o «P2».</summary>
        public static string Label(this PlayerSlot slot)
        {
            return slot == PlayerSlot.PlayerOne ? "P1" : "P2";
        }
    }
}
