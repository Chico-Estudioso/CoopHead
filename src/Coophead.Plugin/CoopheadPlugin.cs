using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using Coophead.Core;
using Coophead.Core.Configuration;
using Coophead.Core.Integration;
using Coophead.Core.Roulette;
using Coophead.Core.Thanks;
using Coophead.Plugin.GameIntegration;
using HarmonyLib;
using UnityEngine;

namespace Coophead.Plugin
{
    /// <summary>
    /// Punto de entrada de BepInEx. En M1 solo carga la configuración, crea la sesión del Core y registra el entorno.
    /// No parchea el juego: la integración llegará con hooks verificados (M2).
    /// </summary>
    [BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
    public sealed class CoopheadPlugin : BaseUnityPlugin
    {
        private const string GeneralSection = "1 - General";

        private ConfigEntry<bool> modEnabled;
        private ConfigEntry<bool> diagnostics;
        private ConfigEntry<bool> installHooks;
        private ConfigEntry<bool> applyRules;
        private Harmony harmony;
        private ConfigEntry<KeyboardShortcut> thanksKeyP1;
        private ConfigEntry<KeyboardShortcut> thanksKeyP2;
        private CoopheadSettings settings;
        private CoopheadConfig coreConfig;
        private CoopheadSession session;
        private BepInExLog log;
        private bool active;
        private bool failed;

        internal static CoopheadPlugin Instance { get; private set; }

        internal CoopheadSession Session
        {
            get { return session; }
        }

        internal bool DiagnosticsEnabled
        {
            get { return diagnostics != null && diagnostics.Value; }
        }

        private void Awake()
        {
            Instance = this;
            log = new BepInExLog(Logger);
            try
            {
                BindPluginSettings();
                settings = CoopheadSettings.Bind(new BepInExConfigBinder(Config));
                coreConfig = new CoopheadConfig();
                ApplySettings();
                Config.SettingChanged += OnSettingChanged;

                session = new CoopheadSession(coreConfig, new DisabledPenaltyApplier(log), log);
                SubscribeDiagnostics();
                active = modEnabled.Value;
                LogEnvironment();
                InstallGameHooks();
            }
            catch (Exception e)
            {
                failed = true;
                Logger.LogError("Coophead no se ha podido iniciar y queda desactivado: " + e);
            }
        }

        private void Update()
        {
            if (failed || session == null)
            {
                return;
            }

            try
            {
                if (!modEnabled.Value)
                {
                    if (active)
                    {
                        session.Shutdown();
                        active = false;
                        log.Info("Coophead desactivado desde la configuración.");
                    }

                    return;
                }

                active = true;
                float now = Time.time;
                if (thanksKeyP1.Value.IsDown())
                {
                    session.OnManualThanks(PlayerSlot.PlayerOne, now);
                }

                if (thanksKeyP2.Value.IsDown())
                {
                    session.OnManualThanks(PlayerSlot.PlayerTwo, now);
                }

                session.Tick(now);
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        private void OnDestroy()
        {
            if (Config != null)
            {
                Config.SettingChanged -= OnSettingChanged;
            }

            GameHooks.Uninstall();
            if (harmony != null)
            {
                harmony.UnpatchSelf();
            }

            SafeShutdown();
        }

        private void BindPluginSettings()
        {
            modEnabled = Config.Bind(GeneralSection, "Activado", true,
                "Interruptor general. Al desactivarlo se restauran todos los efectos y no se castiga a nadie.");
            diagnostics = Config.Bind(GeneralSection, "Diagnostico", true,
                "Escribe en el log de BepInEx cada evento y decisión del mod. No registra audio ni datos personales.");
            installHooks = Config.Bind(GeneralSection, "Hooks", true,
                "Instala los hooks de solo lectura que registran inicio y fin de nivel, muertes y rescates (requiere reiniciar el juego). Si un hook no existe en tu versión, se desactiva solo.");
            applyRules = Config.Bind(GeneralSection, "AplicarReglas", false,
                "Envía las muertes y rescates detectados a las reglas (contadores, agradecimiento y ruleta). Actívalo solo cuando hayas comprobado en el log que los eventos son correctos (docs/GAME_HOOKS.md).");
            thanksKeyP1 = Config.Bind(CoopheadSettings.ThanksSection, "TeclaGraciasP1", new KeyboardShortcut(KeyCode.F7),
                "Tecla de respaldo para que P1 dé las gracias. Comprueba que no choca con tus controles ni con otros mods.");
            thanksKeyP2 = Config.Bind(CoopheadSettings.ThanksSection, "TeclaGraciasP2", new KeyboardShortcut(KeyCode.F8),
                "Tecla de respaldo para que P2 dé las gracias. Comprueba que no choca con tus controles ni con otros mods.");
        }

        private void InstallGameHooks()
        {
            if (!installHooks.Value)
            {
                Logger.LogInfo("Hooks desactivados en la configuración: Coophead no observa el juego.");
                return;
            }

            var translator = new GameEventTranslator(
                session,
                log,
                () => modEnabled.Value && applyRules.Value && !failed,
                () => DiagnosticsEnabled);
            harmony = new Harmony(PluginInfo.Guid);
            IList<GameHooks.HookStatus> statuses = GameHooks.Install(harmony, translator, log);
            int installed = 0;
            foreach (GameHooks.HookStatus status in statuses)
            {
                if (status.Installed)
                {
                    installed++;
                }

                Logger.LogInfo("Hook " + status);
            }

            Logger.LogInfo("Hooks instalados: " + installed + " de " + statuses.Count + ". Reglas "
                + (applyRules.Value ? "APLICADAS" : "en modo solo registro") + ".");
        }

        private void OnSettingChanged(object sender, SettingChangedEventArgs e)
        {
            try
            {
                ApplySettings();
            }
            catch (Exception exception)
            {
                Fail(exception);
            }
        }

        private void ApplySettings()
        {
            IList<string> warnings = settings.ApplyTo(coreConfig);

            // Sin teclas asignadas no hay respaldo manual real: no se puede castigar por no pulsarlas.
            bool hasKeys = thanksKeyP1.Value.MainKey != KeyCode.None || thanksKeyP2.Value.MainKey != KeyCode.None;
            if (coreConfig.Thanks.ManualFallbackEnabled && !hasKeys)
            {
                coreConfig.Thanks.ManualFallbackEnabled = false;
                warnings.Add("Respaldo manual activado sin teclas asignadas: se trata como desactivado.");
            }

            foreach (string warning in warnings)
            {
                log.Warning("Configuración: " + warning);
            }
        }

        private void SubscribeDiagnostics()
        {
            session.ThanksWindowOpened += window =>
            {
                if (DiagnosticsEnabled)
                {
                    log.Info("Ventana de agradecimiento abierta para " + window.Rescued.Label() + " (" + coreConfig.Thanks.WindowSeconds + " s).");
                }
            };
            session.ThanksWindowClosed += window =>
            {
                if (DiagnosticsEnabled)
                {
                    string note = string.IsNullOrEmpty(window.Note) ? string.Empty : " — " + window.Note;
                    log.Info("Ventana de " + window.Rescued.Label() + " cerrada: " + window.Resolution + note);
                }
            };
            session.RouletteSpun += result =>
            {
                if (DiagnosticsEnabled)
                {
                    log.Info("Ruleta: " + result.Target.Label() + " → " + result.Outcome.DisplayName() + " (" + result.Decision + ").");
                }
            };
            session.RestartRequested += request =>
                log.Warning("Límite alcanzado (" + request.Reason + ") por " + request.TriggeredBy.Label()
                    + ". El reinicio del nivel aún no está conectado al juego.");
            session.LevelEnded += summary =>
            {
                if (DiagnosticsEnabled)
                {
                    log.Info("Resumen " + summary.LevelKey + ": P1 " + Describe(summary.PlayerOne) + " | P2 " + Describe(summary.PlayerTwo));
                }
            };
        }

        private static string Describe(Core.Rules.PlayerRoundStats stats)
        {
            return "muertes " + stats.Deaths
                + ", rescates dados " + stats.RevivesGiven
                + ", recibidos " + stats.TotalRevivesReceived
                + ", créditos usados " + stats.CreditsConsumed
                + ", gracias " + stats.ThanksRecognized
                + ", castigos " + stats.PenaltiesReceived;
        }

        private void LogEnvironment()
        {
            Logger.LogInfo(PluginInfo.Name + " " + PluginInfo.Version + " cargado.");
            Logger.LogInfo("Juego: versión " + Application.version + ", Unity " + Application.unityVersion
                + ", plataforma " + Application.platform + ", proceso de " + (IntPtr.Size * 8) + " bits, runtime " + Environment.Version + ".");
            Logger.LogInfo("Efectos de la ruleta: deshabilitados por seguridad hasta M4. Reinicio de nivel: no conectado.");
            Logger.LogInfo("Reglas: límite P1 " + coreConfig.Rules.PlayerOneReviveLimit
                + ", P2 " + coreConfig.Rules.PlayerTwoReviveLimit
                + ", equipo " + coreConfig.Rules.TeamReviveLimit
                + ", reinicio automático " + (coreConfig.Rules.AutoRestartEnabled ? "sí" : "no")
                + ". Agradecimiento: «" + coreConfig.Thanks.RequiredWord + "», " + coreConfig.Thanks.WindowSeconds + " s.");
        }

        private void Fail(Exception e)
        {
            failed = true;
            log.Error("Error inesperado; Coophead se desactiva hasta reiniciar el juego: " + e);
            SafeShutdown();
        }

        private void SafeShutdown()
        {
            if (session == null)
            {
                return;
            }

            try
            {
                session.Shutdown();
            }
            catch (Exception e)
            {
                Logger.LogError("Error al restaurar el estado al apagar Coophead: " + e.Message);
            }
        }
    }
}
