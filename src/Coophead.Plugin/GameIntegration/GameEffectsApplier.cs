using System;
using System.Collections.Generic;
using System.Reflection;
using Coophead.Core;
using Coophead.Core.Diagnostics;
using Coophead.Core.Roulette;
using HarmonyLib;

namespace Coophead.Plugin.GameIntegration
{
    /// <summary>
    /// Aplica los efectos de la ruleta sobre el juego. EXPERIMENTAL: los miembros vienen de código comunitario y se
    /// resuelven por nombre; si alguno no existe, el efecto se marca como no compatible y no se aplica.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>Controles invertidos: usa el mecanismo propio del juego (<c>PlayerStatsManager.ReverseTime</c>), que
    /// invierte solo el movimiento horizontal en el nivel y caduca solo aunque fallara la restauración.</item>
    /// <item>Movimiento lento: multiplica <c>LevelPlayerMotor.properties.moveSpeed</c> y restaura el valor exacto.</item>
    /// <item>Dash limitado: multiplica <c>LevelPlayerMotor.properties.dashTime</c> (dash más corto) y restaura el valor exacto.</item>
    /// <item>Disparo lento: sin vía segura identificada; no compatible por ahora.</item>
    /// </list>
    /// Solo actúa en niveles de plataformas y jefes a pie; en niveles de avión el efecto no se aplica.
    /// </remarks>
    internal sealed class GameEffectsApplier : IPenaltyApplier
    {
        private readonly ICoreLog log;
        private readonly Func<float> slowMovementFactor;
        private readonly Func<float> dashFactor;
        private readonly List<SavedValue> savedValues = new List<SavedValue>();

        private readonly Type playerIdType;
        private readonly MethodInfo getPlayer;
        private readonly Type levelPlayerControllerType;
        private readonly MethodInfo reverseTimeSetter;

        public GameEffectsApplier(ICoreLog log, Func<float> slowMovementFactor, Func<float> dashFactor)
        {
            this.log = log;
            this.slowMovementFactor = slowMovementFactor;
            this.dashFactor = dashFactor;

            try
            {
                playerIdType = AccessTools.TypeByName("PlayerId");
                Type playerManager = AccessTools.TypeByName("PlayerManager");
                if (playerIdType != null && playerManager != null)
                {
                    getPlayer = AccessTools.Method(playerManager, "GetPlayer", new[] { playerIdType });
                }

                levelPlayerControllerType = AccessTools.TypeByName("LevelPlayerController");
                Type stats = AccessTools.TypeByName("PlayerStatsManager");
                if (stats != null)
                {
                    reverseTimeSetter = AccessTools.PropertySetter(stats, "ReverseTime");
                }
            }
            catch (Exception e)
            {
                // Si algo no existe en esta versión, los efectos afectados quedan como no compatibles.
                log.Warning("Efectos: no se pudieron resolver los tipos del juego (" + e.Message + ").");
            }
        }

        public bool IsSupported(PenaltyEffect effect)
        {
            if (getPlayer == null || !getPlayer.IsStatic)
            {
                return false;
            }

            switch (effect)
            {
                case PenaltyEffect.InvertedControls:
                    return reverseTimeSetter != null;
                case PenaltyEffect.SlowMovement:
                case PenaltyEffect.LimitedDash:
                    return levelPlayerControllerType != null;
                default:
                    return false;
            }
        }

        /// <summary>Resumen para el log al arrancar.</summary>
        public string Describe()
        {
            var parts = new List<string>();
            foreach (PenaltyEffect effect in RouletteOutcomes.AllEffects())
            {
                parts.Add(effect.DisplayName() + (IsSupported(effect) ? " OK" : " no compatible"));
            }

            return string.Join(", ", parts.ToArray());
        }

        public void Apply(PlayerSlot player, PenaltyEffect effect, float durationSeconds)
        {
            if (!IsSupported(effect))
            {
                throw new InvalidOperationException("efecto no compatible con esta versión del juego");
            }

            object controller = GetPlayer(player);
            if (controller == null)
            {
                throw new InvalidOperationException("no se encuentra el jugador " + player.Label());
            }

            switch (effect)
            {
                case PenaltyEffect.InvertedControls:
                    object stats = GameReflection.ReadMember(controller, "stats");
                    // Caducidad propia del juego como red de seguridad: aunque no se restaurase, acaba sola.
                    reverseTimeSetter.Invoke(stats, new object[] { durationSeconds + 0.1f });
                    log.Info("Castigo aplicado a " + player.Label() + ": controles invertidos durante " + durationSeconds.ToString("0.0") + " s.");
                    break;
                case PenaltyEffect.SlowMovement:
                    ScaleMotorProperty(player, controller, "moveSpeed", slowMovementFactor());
                    log.Info("Castigo aplicado a " + player.Label() + ": movimiento lento durante " + durationSeconds.ToString("0.0") + " s.");
                    break;
                case PenaltyEffect.LimitedDash:
                    ScaleMotorProperty(player, controller, "dashTime", dashFactor());
                    log.Info("Castigo aplicado a " + player.Label() + ": dash limitado durante " + durationSeconds.ToString("0.0") + " s.");
                    break;
            }
        }

        public void Restore(PlayerSlot player, PenaltyEffect effect)
        {
            switch (effect)
            {
                case PenaltyEffect.InvertedControls:
                    object controller = GetPlayer(player);
                    object stats = GameReflection.ReadMember(controller, "stats");
                    if (stats != null && reverseTimeSetter != null)
                    {
                        reverseTimeSetter.Invoke(stats, new object[] { 0f });
                    }

                    break;
                case PenaltyEffect.SlowMovement:
                    RestoreMotorProperty(player, "moveSpeed");
                    break;
                case PenaltyEffect.LimitedDash:
                    RestoreMotorProperty(player, "dashTime");
                    break;
            }
        }

        private object GetPlayer(PlayerSlot player)
        {
            object id = Enum.ToObject(playerIdType, player == PlayerSlot.PlayerOne ? 0 : 1);
            return getPlayer.Invoke(null, new[] { id });
        }

        private void ScaleMotorProperty(PlayerSlot player, object controller, string field, float factor)
        {
            if (!levelPlayerControllerType.IsInstanceOfType(controller))
            {
                throw new InvalidOperationException("solo disponible en niveles a pie (no en avión)");
            }

            object properties = MotorProperties(controller);
            FieldInfo info = properties == null ? null : AccessTools.Field(properties.GetType(), field);
            if (info == null || info.FieldType != typeof(float))
            {
                throw new InvalidOperationException("no se encuentra " + field);
            }

            // Se guarda por objeto modificado (no por jugador) y con contador, para que el valor original sea siempre
            // el auténtico aunque ambos jugadores compartieran el mismo objeto de propiedades.
            SavedValue saved = Find(properties, field);
            if (saved == null)
            {
                saved = new SavedValue(properties, info, (float)info.GetValue(properties));
                saved.Users.Add(player);
                saved.Field.SetValue(properties, saved.Original * Math.Max(0.1f, Math.Min(1f, factor)));
                savedValues.Add(saved);
                return;
            }

            if (!saved.Users.Contains(player))
            {
                saved.Users.Add(player);
            }
        }

        private void RestoreMotorProperty(PlayerSlot player, string field)
        {
            for (int i = savedValues.Count - 1; i >= 0; i--)
            {
                SavedValue saved = savedValues[i];
                if (saved.Field.Name != field || !saved.Users.Remove(player) || saved.Users.Count > 0)
                {
                    continue;
                }

                savedValues.RemoveAt(i);
                saved.Field.SetValue(saved.Target, saved.Original);
            }
        }

        private SavedValue Find(object target, string field)
        {
            foreach (SavedValue saved in savedValues)
            {
                if (ReferenceEquals(saved.Target, target) && saved.Field.Name == field)
                {
                    return saved;
                }
            }

            return null;
        }

        private static object MotorProperties(object controller)
        {
            object motor = GameReflection.ReadMember(controller, "motor");
            return GameReflection.ReadMember(motor, "properties");
        }

        private sealed class SavedValue
        {
            public SavedValue(object target, FieldInfo field, float original)
            {
                Target = target;
                Field = field;
                Original = original;
                Users = new List<PlayerSlot>();
            }

            public object Target { get; private set; }

            public FieldInfo Field { get; private set; }

            public float Original { get; private set; }

            public List<PlayerSlot> Users { get; private set; }
        }
    }
}
