using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using Coophead.Core;
using Coophead.Core.Configuration;
using Coophead.Core.Integration;
using Coophead.Core.Presentation;
using Coophead.Core.Roulette;
using Coophead.Core.Stats;
using Coophead.Core.Thanks;
using Coophead.Core.Voice;
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
        private const string HudSection = "6 - HUD";
        private const string VoiceSection = "7 - Voz";
        private const string EffectsSection = "8 - Efectos";
        private const string WindowSection = "9 - Ventana";

        private ConfigEntry<bool> modEnabled;
        private ConfigEntry<bool> diagnostics;
        private ConfigEntry<bool> installHooks;
        private ConfigEntry<bool> applyRules;
        private Harmony harmony;
        private ConfigEntry<bool> showHud;
        private ConfigEntry<HudCorner> hudCorner;
        private ConfigEntry<int> hudFontSize;
        private ConfigEntry<float> summarySeconds;
        private readonly HudOverlay overlay = new HudOverlay();
        private readonly HudContext hudContext = new HudContext();
        private IList<string> summaryLines;
        private StatsStore statsStore;
        private ConfigEntry<KeyboardShortcut> windowKey;
        private ConfigEntry<float> windowScale;
        private CoopheadWindow window;
        private bool savedCursorVisible;
        private CursorLockMode savedCursorLock;
        private StatsBook statsBook;
        private ConfigEntry<bool> voiceEnabled;
        private ConfigEntry<string> voiceCulture;
        private ConfigEntry<float> voiceMinConfidence;
        private ConfigEntry<VoiceSourceMode> voiceSources;
        private ConfigEntry<float> voiceDesktopMinConfidence;
        private ConfigEntry<float> voiceDesktopDelay;
        private ConfigEntry<KeyboardShortcut> voiceTestKey;
        private VoiceService voice;
        private ConfigEntry<bool> applyEffects;
        private ConfigEntry<float> slowMovementFactor;
        private ConfigEntry<float> dashFactor;
        private GameEffectsApplier gameEffects;
        private bool restartPending;
        private bool voiceTesting;
        private float summaryUntil;
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

        internal StatsBook Stats
        {
            get { return statsBook; }
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
                gameEffects = new GameEffectsApplier(log, () => slowMovementFactor.Value, () => dashFactor.Value);
                settings = CoopheadSettings.Bind(new BepInExConfigBinder(Config));
                coreConfig = new CoopheadConfig();
                ApplySettings();
                Config.SettingChanged += OnSettingChanged;

                var applier = new RoutingPenaltyApplier(gameEffects, new DisabledPenaltyApplier(log), () => applyEffects.Value);
                session = new CoopheadSession(coreConfig, applier, log);
                statsStore = new StatsStore(System.IO.Path.Combine(Paths.ConfigPath, "Coophead"), log);
                statsBook = statsStore.Load();
                window = new CoopheadWindow(Config) { RequestVoiceTest = StartVoiceTest };
                SubscribeDiagnostics();
                active = modEnabled.Value;
                LogEnvironment();
                InstallGameHooks();
                StartVoice();
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
                UpdateWindow();
                float now = Time.time;
                if (thanksKeyP1.Value.IsDown())
                {
                    session.OnManualThanks(PlayerSlot.PlayerOne, now);
                }

                if (thanksKeyP2.Value.IsDown())
                {
                    session.OnManualThanks(PlayerSlot.PlayerTwo, now);
                }

                ProcessVoice(now);
                session.Tick(now);
                if (restartPending)
                {
                    restartPending = false;
                    RestartLevel();
                }
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

            if (voice != null)
            {
                voice.Dispose();
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
            applyRules = Config.Bind(GeneralSection, "AplicarReglas", true,
                "Envía las muertes y rescates detectados a las reglas (contadores, agradecimiento, ruleta y estadísticas). Si se desactiva, solo se registran en el log.");
            showHud = Config.Bind(HudSection, "MostrarHUD", true,
                "Muestra contadores, créditos, ventana de agradecimiento y efectos activos durante los niveles.");
            hudCorner = Config.Bind(HudSection, "Esquina", HudCorner.SuperiorDerecha,
                "Esquina de la pantalla donde se dibuja el HUD. Elige una que no tape la acción ni las cartas de vida.");
            hudFontSize = Config.Bind(HudSection, "TamanoFuente", 14,
                new ConfigDescription("Tamaño del texto del HUD.", new AcceptableValueRange<int>(8, 32)));
            summarySeconds = Config.Bind(HudSection, "SegundosResumen", 8f,
                new ConfigDescription("Segundos que se muestra el resumen al terminar un nivel. 0 = no mostrarlo.", new AcceptableValueRange<float>(0f, 30f)));
            applyEffects = Config.Bind(EffectsSection, "AplicarEfectos", true,
                "Aplica de verdad los castigos de la ruleta al juego. Si está desactivado, se simulan: se cuentan y se ven en el HUD con «(simulado)», pero no cambian nada. Los efectos no compatibles con tu versión se quitan de la ruleta.");
            slowMovementFactor = Config.Bind(EffectsSection, "FactorMovimientoLento", 0.6f,
                new ConfigDescription("Velocidad de movimiento durante «Movimiento lento» (0,6 = 60 %).", new AcceptableValueRange<float>(0.2f, 1f)));
            dashFactor = Config.Bind(EffectsSection, "FactorDash", 0.5f,
                new ConfigDescription("Duración del dash durante «Dash limitado» (0,5 = la mitad de distancia).", new AcceptableValueRange<float>(0.2f, 1f)));
            voiceEnabled = Config.Bind(VoiceSection, "VozActivada", false,
                "Experimental. Reconoce «gracias» con el reconocedor de voz de Windows, en local y sin Internet. Solo escucha durante la ventana de agradecimiento o la prueba. No guarda ni envía audio. Requiere reiniciar el juego.");
            voiceCulture = Config.Bind(VoiceSection, "Idioma", "es-ES",
                "Idioma del reconocedor de Windows. Debe estar instalado con «Reconocimiento de voz».");
            voiceMinConfidence = Config.Bind(VoiceSection, "ConfianzaMinima", 0.6f,
                new ConfigDescription("Confianza mínima del reconocedor para aceptar la frase. Súbela si hay falsos positivos.", new AcceptableValueRange<float>(0.1f, 1f)));
            voiceSources = Config.Bind(VoiceSection, "FuenteVoz", VoiceSourceMode.Ambos,
                "De dónde se escucha el «gracias»: Microfono (tu micrófono predeterminado), Escritorio (lo que suena en el PC, p. ej. la voz de tu pareja por Parsec) o Ambos. Requiere reiniciar el juego.");
            voiceDesktopMinConfidence = Config.Bind(VoiceSection, "ConfianzaMinimaEscritorio", 0.6f,
                new ConfigDescription("Confianza mínima para frases oídas por el escritorio. Más alta que la del micrófono porque ahí también suena el juego.", new AcceptableValueRange<float>(0.1f, 1f)));
            voiceDesktopDelay = Config.Bind(VoiceSection, "RetrasoEscritorio", 0.3f,
                new ConfigDescription("Segundos que se ignora el escritorio al abrirse la ventana, para no confundir el sonido de reanimación del juego con un «gracias». Requiere reiniciar el juego.", new AcceptableValueRange<float>(0f, 2f)));
            voiceTestKey = Config.Bind(VoiceSection, "TeclaPruebaVoz", new KeyboardShortcut(KeyCode.F9),
                "Escucha 5 s y escribe en el log lo que se ha reconocido, para probar el micrófono.");
            windowKey = Config.Bind(WindowSection, "TeclaVentana", new KeyboardShortcut(KeyCode.F6),
                "Abre y cierra la ventana de Coophead (libro de recuerdos, ruleta y ajustes). Se maneja con el ratón.");
            windowScale = Config.Bind(WindowSection, "EscalaVentana", 1f,
                new ConfigDescription("Tamaño de la ventana de Coophead.", new AcceptableValueRange<float>(0.6f, 2f)));
            thanksKeyP1 = Config.Bind(CoopheadSettings.ThanksSection, "TeclaGraciasP1", new KeyboardShortcut(KeyCode.F7),
                "Tecla de respaldo para que P1 dé las gracias. Comprueba que no choca con tus controles ni con otros mods.");
            thanksKeyP2 = Config.Bind(CoopheadSettings.ThanksSection, "TeclaGraciasP2", new KeyboardShortcut(KeyCode.F8),
                "Tecla de respaldo para que P2 dé las gracias. Comprueba que no choca con tus controles ni con otros mods.");
        }

        private void StartVoice()
        {
            if (!voiceEnabled.Value)
            {
                Logger.LogInfo("Voz desactivada (experimental). El agradecimiento se confirma con la tecla de respaldo.");
                return;
            }

            if (Application.platform != RuntimePlatform.WindowsPlayer && Application.platform != RuntimePlatform.WindowsEditor)
            {
                Logger.LogWarning("La voz solo está disponible en Windows; queda desactivada.");
                return;
            }

            voice = new VoiceService(log);
            string helper = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location), "Coophead.Voice.exe");
            IList<string> phrases = new ThanksPhraseMatcher(coreConfig.Thanks).BuildSpokenGrammar();
            var options = new VoiceService.Options
            {
                Culture = voiceCulture.Value,
                Phrases = phrases,
                Sources = voiceSources.Value,
                MinConfidence = voiceMinConfidence.Value,
                DesktopMinConfidence = voiceDesktopMinConfidence.Value,
                DesktopDelaySeconds = voiceDesktopDelay.Value,
            };
            if (!voice.Start(helper, options))
            {
                voice = null;
                return;
            }

            session.ThanksWindowOpened += window => voice.Send(VoiceProtocol.Listen);
            session.ThanksWindowClosed += window =>
            {
                if (session.Thanks.Pending.Count == 0 && !voiceTesting)
                {
                    voice.Send(VoiceProtocol.Stop);
                }
            };
        }

        private void ProcessVoice(float now)
        {
            if (voice == null)
            {
                return;
            }

            if (voiceTestKey.Value.IsDown())
            {
                StartVoiceTest();
            }

            foreach (VoiceMessage message in voice.Drain())
            {
                switch (message.Kind)
                {
                    case VoiceMessageKind.Ready:
                        session.SetVoiceAvailable(true);
                        log.Info("Voz lista (" + message.Text + "). Reconocimiento local de Windows con modelos estadísticos; sin Internet, sin guardar audio.");
                        break;
                    case VoiceMessageKind.Unavailable:
                        session.SetVoiceAvailable(false);
                        session.ReportVoiceFailure();
                        log.Warning("Voz no disponible: " + message.Text + " Nadie será castigado por ello.");
                        break;
                    case VoiceMessageKind.Heard:
                    {
                        ThanksWindow closed = session.OnTranscript(message.Text, now);
                        string line = "«" + message.Text + "» por " + SourceLabel(message.Source) + " (" + Confidence(message)
                            + ") → " + (closed != null ? "gracias de " + closed.Rescued.Label() : "fuera de ventana");
                        VoiceLog.Add(line);
                        if (voiceTesting || DiagnosticsEnabled)
                        {
                            log.Info("Voz: " + line);
                        }

                        break;
                    }

                    case VoiceMessageKind.Low:
                    {
                        string line = "«" + message.Text + "» por " + SourceLabel(message.Source) + " (" + Confidence(message)
                            + ") → no cuenta: confianza por debajo del mínimo";
                        VoiceLog.Add(line);
                        if (voiceTesting || DiagnosticsEnabled)
                        {
                            log.Info("Voz: " + line);
                        }

                        break;
                    }

                    case VoiceMessageKind.Rejected:
                        VoiceLog.Add("algo por " + SourceLabel(message.Source) + " → no se parece a «" + coreConfig.Thanks.RequiredWord + "»");
                        break;
                    case VoiceMessageKind.TestEnd:
                        voiceTesting = false;
                        log.Info("Prueba de voz terminada.");
                        break;
                    case VoiceMessageKind.Error:
                        log.Warning("Voz: " + message.Text);
                        break;
                }
            }
        }

        internal void StartVoiceTest()
        {
            if (voice == null || !voice.Available)
            {
                log.Warning("Prueba de voz: la voz no está disponible (actívala con VozActivada y reinicia el juego).");
                return;
            }

            voiceTesting = true;
            VoiceLog.Add("— prueba de 5 s: decid «" + coreConfig.Thanks.RequiredWord + "» —");
            log.Info("Prueba de voz: escuchando 5 s. Di «" + coreConfig.Thanks.RequiredWord + "».");
            voice.Send(VoiceProtocol.Test(5f));
        }

        private static string SourceLabel(string source)
        {
            return string.IsNullOrEmpty(source) ? "voz" : source == VoiceProtocol.SourceMicrophone ? "micrófono" : source;
        }

        private static string Confidence(VoiceMessage message)
        {
            return "confianza " + message.Confidence.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        }

        private void RestartLevel()
        {
            Type sceneLoader = AccessTools.TypeByName("SceneLoader");
            System.Reflection.MethodInfo reload = sceneLoader == null ? null : AccessTools.Method(sceneLoader, "ReloadLevel", Type.EmptyTypes);
            if (reload == null || !reload.IsStatic)
            {
                log.Warning("No se encuentra SceneLoader.ReloadLevel en esta versión: no se reinicia el nivel.");
                return;
            }

            reload.Invoke(null, null);
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

        private void UpdateWindow()
        {
            if (windowKey.Value.IsDown())
            {
                window.Visible = !window.Visible;
                if (window.Visible)
                {
                    savedCursorVisible = Cursor.visible;
                    savedCursorLock = Cursor.lockState;
                }
                else
                {
                    RestoreCursor();
                }
            }

            if (window.Visible)
            {
                // El juego puede ocultar el cursor en cada frame: mientras la ventana está abierta se mantiene visible.
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }

        private void RestoreCursor()
        {
            Cursor.visible = savedCursorVisible;
            Cursor.lockState = savedCursorLock;
        }

        private void OnGUI()
        {
            if (failed || session == null || !modEnabled.Value)
            {
                return;
            }

            if (window != null && window.Visible)
            {
                try
                {
                    window.Draw(session, coreConfig, statsBook, statsStore.FilePath, windowScale.Value);
                    if (!window.Visible)
                    {
                        RestoreCursor();
                    }
                }
                catch (ExitGUIException)
                {
                    // Unity la usa para cortar el dibujo a propósito: no es un error.
                    throw;
                }
                catch (Exception e)
                {
                    window.Visible = false;
                    RestoreCursor();
                    log.Error("Error al dibujar la ventana; se cierra: " + e.Message);
                }
            }

            // El HUD solo se dibuja en Repaint: evita construir el texto varias veces por frame.
            if (!showHud.Value || Event.current.type != EventType.Repaint)
            {
                return;
            }

            try
            {
                float offset = 0f;
                if (session.Rules.Session != null && session.Rules.Session.IsActive)
                {
                    hudContext.RulesApplied = applyRules.Value;
                    hudContext.VoiceAvailable = session.Thanks.VoiceAvailable;
                    hudContext.VoiceListening = voice != null && voice.Listening;
                    hudContext.EffectsSimulated = !applyEffects.Value;
                    IList<string> lines = HudFormatter.BuildLines(session, coreConfig, hudContext, Time.time);
                    overlay.Draw(lines, hudCorner.Value, hudFontSize.Value, 0f);
                    offset = HudOverlay.Height(lines, hudFontSize.Value) + 6f;
                }

                if (summaryLines != null && Time.realtimeSinceStartup < summaryUntil)
                {
                    overlay.Draw(summaryLines, hudCorner.Value, hudFontSize.Value, offset);
                }
            }
            catch (Exception e)
            {
                showHud.Value = false;
                log.Error("Error al dibujar el HUD; se oculta: " + e.Message);
            }
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

            if (applyEffects.Value)
            {
                foreach (PenaltyEffect effect in RouletteOutcomes.AllEffects())
                {
                    OutcomeSettings outcome = coreConfig.Roulette.Get(effect.ToOutcome());
                    if (outcome.Enabled && !gameEffects.IsSupported(effect))
                    {
                        outcome.Enabled = false;
                        warnings.Add("«" + effect.DisplayName() + "» no es compatible con esta versión: se quita de la ruleta.");
                    }
                }
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
            {
                log.Warning("Límite alcanzado (" + request.Reason + ") por " + request.TriggeredBy.Label() + ": se reinicia el nivel.");
                // Se difiere al siguiente Update: nunca se recarga la escena dentro de un hook del juego.
                restartPending = true;
            };
            session.LevelEnded += summary =>
            {
                if (statsBook.Record(summary, DateTime.UtcNow) != null)
                {
                    statsStore.Save(statsBook);
                }

                if (summarySeconds.Value > 0f)
                {
                    summaryLines = HudFormatter.BuildSummary(summary);
                    summaryUntil = Time.realtimeSinceStartup + summarySeconds.Value;
                }

                if (DiagnosticsEnabled)
                {
                    log.Info("Resumen " + summary.LevelKey + ": P1 " + Describe(summary.PlayerOne) + " | P2 " + Describe(summary.PlayerTwo));
                }
            };
        }

        private static string Describe(Core.Rules.PlayerRoundStats stats)
        {
            return "muertes " + stats.Deaths
                + ", daño " + stats.DamageDealt.ToString("0")
                + ", parrys " + stats.Parries
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
            Logger.LogInfo("Efectos de la ruleta: " + (applyEffects.Value ? "se aplican" : "simulados") + " (" + gameEffects.Describe() + ").");
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
