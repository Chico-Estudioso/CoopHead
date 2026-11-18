using System;
using System.Collections.Generic;
using System.Reflection;
using Coophead.Core.Diagnostics;
using Coophead.Core.Integration;
using Coophead.Core.Rules;
using HarmonyLib;
using UnityEngine;

namespace Coophead.Plugin.GameIntegration
{
    /// <summary>
    /// Hooks candidatos de docs/GAME_HOOKS.md. Se resuelven por nombre en tiempo de ejecución y se instalan como
    /// postfix de solo lectura: no cambian argumentos, valores de retorno ni estado del juego.
    /// </summary>
    internal static class GameHooks
    {
        private static GameEventTranslator translator;
        private static ICoreLog log;

        private static readonly HookSpec[] Catalog =
        {
            new HookSpec("Level", "Awake", "LevelAwake", "Inicio de nivel"),
            new HookSpec("Level", "OnWin", "LevelWon", "Victoria"),
            new HookSpec("Level", "_OnLose", "LevelLost", "Derrota"),
            new HookSpec("Level", "OnDestroy", "LevelDestroyed", "Salida del nivel"),
            new HookSpec("Level", "OnPlayerDeath", "LevelPlayerDeath", "Muerte notificada por el nivel (diagnóstico)"),
            new HookSpec("PlayerDeathEffect", "Start", "GhostStart", "Aparición del fantasma (muerte)"),
            new HookSpec("PlayerDeathParrySwitch", "OnParryPrePause", "ParryRevive", "Rescate por parry"),
            new HookSpec("PlayerDeathEffect", "ReviveOutOfFrame", "StealRevive", "Robo de corazón (diagnóstico)"),
            new HookSpec("PlayerStatsManager", "OnRevive", "StatsRevive", "Reanimación confirmada por el juego"),
        };

        public static IList<HookStatus> Install(Harmony harmony, GameEventTranslator eventTranslator, ICoreLog coreLog)
        {
            translator = eventTranslator;
            log = coreLog;
            var results = new List<HookStatus>();
            foreach (HookSpec spec in Catalog)
            {
                results.Add(InstallOne(harmony, spec));
            }

            return results;
        }

        public static void Uninstall()
        {
            translator = null;
        }

        private static HookStatus InstallOne(Harmony harmony, HookSpec spec)
        {
            string target = spec.TypeName + "." + spec.MethodName;
            try
            {
                Type type = AccessTools.TypeByName(spec.TypeName);
                if (type == null)
                {
                    return new HookStatus(spec, false, "tipo no encontrado");
                }

                MethodInfo original = AccessTools.DeclaredMethod(type, spec.MethodName);
                if (original == null)
                {
                    return new HookStatus(spec, false, "método no encontrado en " + type.FullName);
                }

                MethodInfo postfix = typeof(GameHooks).GetMethod(spec.PostfixName, BindingFlags.Static | BindingFlags.NonPublic);
                harmony.Patch(original, postfix: new HarmonyMethod(postfix));
                return new HookStatus(spec, true, Signature(original));
            }
            catch (Exception e)
            {
                return new HookStatus(spec, false, "error al parchear " + target + ": " + e.Message);
            }
        }

        private static string Signature(MethodInfo method)
        {
            var parts = new List<string>();
            foreach (ParameterInfo parameter in method.GetParameters())
            {
                parts.Add(parameter.ParameterType.Name + " " + parameter.Name);
            }

            return method.DeclaringType.Name + "." + method.Name + "(" + string.Join(", ", parts.ToArray()) + ") en " + method.DeclaringType.Assembly.GetName().Name;
        }

        /// <summary>Ningún fallo de Coophead debe propagarse al juego.</summary>
        private static void Guard(string hook, Action action)
        {
            if (translator == null)
            {
                return;
            }

            try
            {
                action();
            }
            catch (Exception e)
            {
                if (log != null)
                {
                    log.Error("Error en el hook " + hook + ": " + e.Message);
                }
            }
        }

        private static int InstanceId(object instance)
        {
            var unityObject = instance as UnityEngine.Object;
            return unityObject != null ? unityObject.GetInstanceID() : 0;
        }

        // ---- Postfix (solo lectura) ----

        private static void LevelAwake(object __instance)
        {
            Guard("Level.Awake", () => translator.LevelStarted(__instance.GetType().Name, InstanceId(__instance)));
        }

        private static void LevelWon(object __instance)
        {
            Guard("Level.OnWin", () => translator.LevelEnded(LevelEndReason.Won, "Level.OnWin", InstanceId(__instance)));
        }

        private static void LevelLost(object __instance)
        {
            Guard("Level._OnLose", () => translator.LevelEnded(LevelEndReason.Lost, "Level._OnLose", InstanceId(__instance)));
        }

        private static void LevelDestroyed(object __instance)
        {
            Guard("Level.OnDestroy", () => translator.LevelEnded(LevelEndReason.Exited, "Level.OnDestroy", InstanceId(__instance)));
        }

        private static void LevelPlayerDeath(object[] __args)
        {
            Guard("Level.OnPlayerDeath", () =>
            {
                object deathEvent = __args != null && __args.Length > 0 ? __args[0] : null;
                translator.DeathReported(GameReflection.ReadPlayerId(deathEvent, "playerId"));
            });
        }

        private static void GhostStart(object __instance)
        {
            Guard("PlayerDeathEffect.Start", () =>
                translator.GhostSpawned(GameReflection.ReadPlayerId(__instance, "playerId"), InstanceId(__instance), Time.time));
        }

        private static void ParryRevive(object __instance, object[] __args)
        {
            Guard("PlayerDeathParrySwitch.OnParryPrePause", () =>
            {
                object rescuer = __args != null && __args.Length > 0 ? __args[0] : null;
                Component ghost = FindGhost(__instance as Component);
                if (ghost == null)
                {
                    log.Warning("Rescate por parry sin fantasma asociado: se ignora.");
                    return;
                }

                translator.ParryRevive(
                    GameReflection.ReadPlayerId(ghost, "playerId"),
                    GameReflection.ReadPlayerId(rescuer, "id"),
                    ghost.GetInstanceID(),
                    Time.time);
            });
        }

        private static void StealRevive(object __instance)
        {
            Guard("PlayerDeathEffect.ReviveOutOfFrame", () =>
                translator.StealRevive(GameReflection.ReadPlayerId(__instance, "playerId"), InstanceId(__instance)));
        }

        private static void StatsRevive(object __instance)
        {
            Guard("PlayerStatsManager.OnRevive", () =>
            {
                object player = GameReflection.ReadMember(__instance, "basePlayer");
                translator.PlayerBackInGame(GameReflection.ReadPlayerId(player, "id"), Time.frameCount, Time.time);
            });
        }

        private static Component FindGhost(Component parrySwitch)
        {
            if (parrySwitch == null)
            {
                return null;
            }

            Type ghostType = AccessTools.TypeByName("PlayerDeathEffect");
            if (ghostType == null)
            {
                return null;
            }

            return parrySwitch.GetComponentInParent(ghostType) ?? parrySwitch.GetComponent(ghostType);
        }

        internal sealed class HookSpec
        {
            public HookSpec(string typeName, string methodName, string postfixName, string purpose)
            {
                TypeName = typeName;
                MethodName = methodName;
                PostfixName = postfixName;
                Purpose = purpose;
            }

            public string TypeName { get; private set; }

            public string MethodName { get; private set; }

            public string PostfixName { get; private set; }

            public string Purpose { get; private set; }
        }

        internal sealed class HookStatus
        {
            public HookStatus(HookSpec spec, bool installed, string detail)
            {
                Spec = spec;
                Installed = installed;
                Detail = detail;
            }

            public HookSpec Spec { get; private set; }

            public bool Installed { get; private set; }

            public string Detail { get; private set; }

            public override string ToString()
            {
                return (Installed ? "OK    " : "FALTA ") + Spec.TypeName + "." + Spec.MethodName + " — " + Spec.Purpose + " — " + Detail;
            }
        }
    }
}
