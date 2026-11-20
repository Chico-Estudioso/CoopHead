using System;
using System.Collections.Generic;
using System.Globalization;
using BepInEx.Configuration;
using Coophead.Core;
using Coophead.Core.Configuration;
using Coophead.Core.Presentation;
using Coophead.Core.Roulette;
using Coophead.Core.Rules;
using Coophead.Core.Stats;
using Coophead.Core.Thanks;
using UnityEngine;

namespace Coophead.Plugin
{
    /// <summary>
    /// Ventana de Coophead dentro del juego (IMGUI): partida en curso, libro de recuerdos, totales, ruleta, ajustes
    /// principales y registro. No captura los mandos: se maneja con el ratón.
    /// </summary>
    internal sealed class CoopheadWindow
    {
        private const int WindowId = 0x0C00D;
        private const string Gold = "#ffcf40";
        private static readonly string[] Tabs = { "Partida", "Recuerdos", "Totales", "Ruleta", "Ajustes", "Registro" };
        private static readonly string[] AttemptViews = { "Totales del jefe", "Try final (última victoria)", "Último intento" };
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        private readonly ConfigFile config;
        private Rect rect = new Rect(0f, 0f, 860f, 600f);
        private bool positioned;
        private int tab;
        private int attemptView;
        private string selectedLevel;
        private Vector2 scroll;
        private Vector2 listScroll;
        private GUIStyle rich;
        private GUIStyle title;
        private GUIStyle small;

        // Datos que se rellenan antes de dibujar.
        private CoopheadSession session;
        private CoopheadConfig coreConfig;
        private StatsBook book;
        private string statsPath;

        public CoopheadWindow(ConfigFile config)
        {
            this.config = config;
        }

        public bool Visible { get; set; }

        /// <summary>Lanza una prueba de voz de 5 s (la conecta el plugin).</summary>
        public Action RequestVoiceTest { get; set; }

        /// <summary>Solo desde OnGUI.</summary>
        public void Draw(CoopheadSession currentSession, CoopheadConfig currentConfig, StatsBook currentBook, string currentStatsPath, float userScale)
        {
            if (!Visible)
            {
                return;
            }

            session = currentSession;
            coreConfig = currentConfig;
            book = currentBook;
            statsPath = currentStatsPath;
            EnsureStyles();

            float scale = Mathf.Clamp(userScale * Screen.height / 1080f, 0.5f, 3f);
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            if (!positioned)
            {
                rect.x = Mathf.Max(0f, ((Screen.width / scale) - rect.width) / 2f);
                rect.y = Mathf.Max(0f, ((Screen.height / scale) - rect.height) / 2f);
                positioned = true;
            }

            rect = GUILayout.Window(WindowId, rect, DrawContents, "Coophead · Libro de recuerdos");
            GUI.matrix = previous;
        }

        private void EnsureStyles()
        {
            if (rich != null)
            {
                return;
            }

            rich = new GUIStyle(GUI.skin.label) { richText = true, wordWrap = false };
            small = new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true, fontSize = 12 };
            title = new GUIStyle(GUI.skin.label) { richText = true, fontStyle = FontStyle.Bold, fontSize = 16 };
        }

        private void DrawContents(int id)
        {
            GUILayout.BeginHorizontal();
            tab = GUILayout.Toolbar(tab, Tabs);
            if (GUILayout.Button("Cerrar", GUILayout.Width(80f)))
            {
                Visible = false;
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(6f);

            switch (tab)
            {
                case 0:
                    Scrolled(DrawCurrent);
                    break;
                case 1:
                    DrawBook();
                    break;
                case 2:
                    Scrolled(DrawTotals);
                    break;
                case 3:
                    Scrolled(DrawRoulette);
                    break;
                case 4:
                    Scrolled(DrawSettings);
                    break;
                default:
                    Scrolled(DrawLog);
                    break;
            }

            GUI.DragWindow(new Rect(0f, 0f, 10000f, 22f));
        }

        private void Scrolled(Action body)
        {
            scroll = GUILayout.BeginScrollView(scroll);
            body();
            GUILayout.EndScrollView();
        }

        // ---------------- Partida ----------------

        private void DrawCurrent()
        {
            LevelSessionState state = session.Rules.Session;
            IList<ComparisonRow> rows = BookViews.CurrentAttempt(session);
            if (state == null || rows == null)
            {
                GUILayout.Label("No hay ningún nivel en curso. Entra en un nivel para ver la partida en directo.", small);
                return;
            }

            GUILayout.Label(LevelNames.Display(state.LevelKey) + " · intento en curso · " + StatsComparison.FormatDuration(Time.time - state.StartTime), title);
            GUILayout.Label("Equipo: " + state.TeamRevives + " reanimaciones" + (coreConfig.Rules.TeamReviveLimit > 0 ? " de " + coreConfig.Rules.TeamReviveLimit : string.Empty), rich);
            foreach (ThanksWindow window in session.Thanks.Pending)
            {
                GUILayout.Label(Colored(window.Rescued.Label() + ": ¡da las gracias! " + window.Remaining(Time.time).ToString("0.0", Invariant) + " s"), rich);
            }

            foreach (PlayerSlot waiting in session.PunishmentsWaitingForRevive)
            {
                GUILayout.Label(waiting.Label() + ": sin gracias · castigo al reaparecer", rich);
            }

            foreach (ActivePenalty penalty in session.Penalties.Active)
            {
                GUILayout.Label(penalty.Player.Label() + ": " + penalty.Effect.DisplayName() + " " + penalty.Remaining(Time.time).ToString("0.0", Invariant) + " s", rich);
            }

            GUILayout.Space(6f);
            DrawComparison(rows);
        }

        // ---------------- Recuerdos ----------------

        private void DrawBook()
        {
            IList<BossRecord> records = book.Records();
            if (records.Count == 0)
            {
                GUILayout.Label("El libro está vacío. Cada nivel que juguéis con las reglas activadas se apuntará aquí.", small);
                return;
            }

            if (selectedLevel == null || book.Get(selectedLevel) == null)
            {
                selectedLevel = records[0].LevelKey;
            }

            GUILayout.BeginHorizontal();
            listScroll = GUILayout.BeginScrollView(listScroll, GUILayout.Width(250f));
            foreach (BossRecord record in records)
            {
                string label = record.DisplayName + "\n" + record.Attempts + " intentos · " + record.Wins + " victorias";
                bool selected = record.LevelKey == selectedLevel;
                if (GUILayout.Toggle(selected, label, GUI.skin.button) && !selected)
                {
                    selectedLevel = record.LevelKey;
                }
            }

            GUILayout.EndScrollView();

            scroll = GUILayout.BeginScrollView(scroll);
            BossRecord current = book.Get(selectedLevel);
            GUILayout.Label(current.DisplayName, title);
            foreach (string line in BookViews.RecordHeader(current))
            {
                GUILayout.Label(line, small);
            }

            GUILayout.Space(6f);
            attemptView = GUILayout.Toolbar(attemptView, AttemptViews);
            switch (attemptView)
            {
                case 0:
                    DrawComparison(StatsComparison.Build(current.PlayerOne, current.PlayerTwo));
                    break;
                case 1:
                    DrawAttempt(current.LastWin);
                    break;
                default:
                    DrawAttempt(current.LastAttempt);
                    break;
            }

            GUILayout.EndScrollView();
            GUILayout.EndHorizontal();
        }

        private void DrawAttempt(AttemptRecord attempt)
        {
            GUILayout.Label(BookViews.AttemptHeader(attempt), small);
            if (attempt != null)
            {
                DrawComparison(StatsComparison.Build(attempt.PlayerOne, attempt.PlayerTwo));
            }
        }

        // ---------------- Totales ----------------

        private void DrawTotals()
        {
            GUILayout.Label("Todo lo jugado juntos", title);
            foreach (string line in BookViews.BookHeader(book))
            {
                GUILayout.Label(line, small);
            }

            GUILayout.Space(6f);
            DrawComparison(StatsComparison.Build(book.GrandTotal(PlayerSlot.PlayerOne), book.GrandTotal(PlayerSlot.PlayerTwo)));

            GUILayout.Space(10f);
            GUILayout.Label("Niveles más intentados", title);
            var records = new List<BossRecord>(book.Records());
            records.Sort((a, b) => b.Attempts.CompareTo(a.Attempts));
            for (int i = 0; i < records.Count && i < 10; i++)
            {
                BossRecord record = records[i];
                GUILayout.Label(record.DisplayName + " — " + record.Attempts + " intentos, " + record.Wins + " victorias, mejor " + StatsComparison.FormatDuration(record.BestWinSeconds), small);
            }
        }

        // ---------------- Ruleta ----------------

        private void DrawRoulette()
        {
            GUILayout.Label("Ruleta de castigos", title);
            Toggle(CoopheadSettings.RouletteSection, "RuletaActivada", "Ruleta activada");
            Toggle("8 - Efectos", "AplicarEfectos", "Aplicar los castigos de verdad (si no, se simulan)");
            Toggle(CoopheadSettings.BalanceSection, "DirectorActivado", "Director de equilibrio (protege a quien lo pasa peor)");
            Slider(CoopheadSettings.RouletteSection, "DuracionGeneral", "Duración de los castigos (s)", 1f, 10f, "0.0");
            Slider(CoopheadSettings.RouletteSection, "Cooldown", "Tiempo mínimo entre castigos al mismo jugador (s)", 0f, 30f, "0");
            Slider(CoopheadSettings.RouletteSection, "FocoP2", "Foco en P2 (0 = igual para los dos)", 0f, 1f, "0.00");

            GUILayout.Space(8f);
            GUILayout.BeginHorizontal();
            GUILayout.Label("<b>Resultado</b>", rich, GUILayout.Width(250f));
            GUILayout.Label("<b>Peso</b>", rich, GUILayout.Width(230f));
            GUILayout.Label("<b>P1 ahora</b>", rich, GUILayout.Width(90f));
            GUILayout.Label("<b>P2 ahora</b>", rich, GUILayout.Width(90f));
            GUILayout.EndHorizontal();

            foreach (RouletteRow row in BookViews.Roulette(session, coreConfig, Time.time))
            {
                string key = CoopheadSettings.OutcomeKey(row.Outcome);
                GUILayout.BeginHorizontal();
                ToggleInline(CoopheadSettings.RouletteSection, key + "Activado", row.Outcome.DisplayName(), 250f);
                SliderInline(CoopheadSettings.RouletteSection, key + "Peso", 0f, 100f, 230f);
                GUILayout.Label(BookViews.Percent(row.ProbabilityPlayerOne), rich, GUILayout.Width(90f));
                GUILayout.Label(BookViews.Percent(row.ProbabilityPlayerTwo), rich, GUILayout.Width(90f));
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(6f);
            GUILayout.Label(BookViews.Pressure(session, PlayerSlot.PlayerOne, Time.time), small);
            GUILayout.Label(BookViews.Pressure(session, PlayerSlot.PlayerTwo, Time.time), small);
            GUILayout.Label("Las probabilidades no tienen en cuenta el tiempo mínimo entre castigos ni la protección tras dos castigos seguidos.", small);
        }

        // ---------------- Ajustes ----------------

        private void DrawSettings()
        {
            GUILayout.Label("Reglas", title);
            Toggle("1 - General", "AplicarReglas", "Aplicar reglas (contadores, ventana de gracias, ruleta y estadísticas)");
            Toggle(CoopheadSettings.RulesSection, "ReinicioAutomatico", "Reiniciar el nivel al llegar a un límite");
            Toggle(CoopheadSettings.RulesSection, "CreditosDeAyuda", "Créditos de ayuda por rescatar a la pareja");
            Stepper(CoopheadSettings.RulesSection, "LimiteReanimacionesP1", "Límite de reanimaciones de P1 (0 = sin límite)", 0, 20);
            Stepper(CoopheadSettings.RulesSection, "LimiteReanimacionesP2", "Límite de reanimaciones de P2 (0 = sin límite)", 0, 20);
            Stepper(CoopheadSettings.RulesSection, "LimiteReanimacionesEquipo", "Límite del equipo (0 = sin límite)", 0, 50);

            GUILayout.Space(8f);
            GUILayout.Label("Agradecimiento", title);
            Toggle(CoopheadSettings.ThanksSection, "ExigirAgradecimiento", "Exigir el «gracias» tras cada rescate");
            Slider(CoopheadSettings.ThanksSection, "VentanaSegundos", "Segundos para dar las gracias (desde el parry)", 1f, 8f, "0.0");
            Toggle(CoopheadSettings.ThanksSection, "RespaldoManual", "Permitir dar las gracias con tecla (F7 / F8)");
            Toggle(CoopheadSettings.ThanksSection, "ExigirApelativo", "Exigir apelativo («gracias, cari»)");

            GUILayout.Space(8f);
            GUILayout.Label("Voz", title);
            Toggle("7 - Voz", "VozActivada", "Reconocer el «gracias» por voz (requiere reiniciar el juego)");
            EnumCycle("7 - Voz", "FuenteVoz", "Fuente (requiere reiniciar el juego)");
            Slider("7 - Voz", "ConfianzaMinima", "Confianza mínima del micrófono", 0.1f, 1f, "0.00");
            Slider("7 - Voz", "ConfianzaMinimaEscritorio", "Confianza mínima del escritorio", 0.1f, 1f, "0.00");
            Slider("7 - Voz", "RetrasoEscritorio", "Escritorio ignorado al hacer el parry (s, requiere reiniciar)", 0f, 2f, "0.0");
            Slider(CoopheadSettings.ThanksSection, "MargenVoz", "Margen para que la voz termine de procesar (s)", 0f, 5f, "0.0");
            if (GUILayout.Button("Probar la voz 5 s (también F9)", GUILayout.Width(300f)) && RequestVoiceTest != null)
            {
                RequestVoiceTest();
            }

            GUILayout.Label("Últimas frases oídas (para ajustar la confianza):", small);
            string[] heard = VoiceLog.Snapshot();
            if (heard.Length == 0)
            {
                GUILayout.Label("  Nada todavía.", small);
            }

            foreach (string line in heard)
            {
                GUILayout.Label("  " + line, small);
            }

            GUILayout.Space(8f);
            GUILayout.Label("Castigos y HUD", title);
            Slider("8 - Efectos", "FactorMovimientoLento", "Velocidad con «Movimiento lento»", 0.2f, 1f, "0.00");
            Slider("8 - Efectos", "FactorDash", "Dash con «Dash limitado»", 0.2f, 1f, "0.00");
            Toggle("6 - HUD", "MostrarHUD", "Mostrar el HUD durante los niveles");
            EnumCycle("6 - HUD", "Esquina", "Esquina del HUD");
            Stepper("6 - HUD", "TamanoFuente", "Tamaño del texto del HUD", 8, 32);
            Slider("9 - Ventana", "EscalaVentana", "Tamaño de esta ventana", 0.6f, 2f, "0.00");

            GUILayout.Space(8f);
            GUILayout.Label("Los cambios se guardan al momento en BepInEx/config/com.coophead.rules.cfg. Estadísticas: " + statsPath, small);
        }

        // ---------------- Registro ----------------

        private void DrawLog()
        {
            GUILayout.Label("Últimos mensajes de Coophead (el más reciente arriba)", title);
            foreach (string line in RecentLog.Snapshot())
            {
                GUILayout.Label(line, small);
            }
        }

        // ---------------- Utilidades ----------------

        private void DrawComparison(IList<ComparisonRow> rows)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Empty, rich, GUILayout.Width(240f));
            GUILayout.Label("<b>P1</b>", rich, GUILayout.Width(140f));
            GUILayout.Label("<b>P2</b>", rich, GUILayout.Width(140f));
            GUILayout.EndHorizontal();
            foreach (ComparisonRow row in rows)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(row.Label, rich, GUILayout.Width(240f));
                GUILayout.Label(Cell(row.PlayerOne, row.Leader == PlayerSlot.PlayerOne), rich, GUILayout.Width(140f));
                GUILayout.Label(Cell(row.PlayerTwo, row.Leader == PlayerSlot.PlayerTwo), rich, GUILayout.Width(140f));
                GUILayout.EndHorizontal();
            }

            GUILayout.Label("En dorado, quién lleva más en cada fila.", small);
        }

        private static string Cell(string value, bool leader)
        {
            return leader ? "<color=" + Gold + "><b>" + value + "</b></color>" : value;
        }

        private static string Colored(string text)
        {
            return "<color=" + Gold + ">" + text + "</color>";
        }

        private ConfigEntryBase Entry(string section, string key)
        {
            try
            {
                return config[section, key];
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void Toggle(string section, string key, string label)
        {
            ToggleInline(section, key, label, 0f);
        }

        private void ToggleInline(string section, string key, string label, float width)
        {
            ConfigEntryBase entry = Entry(section, key);
            if (entry == null || !(entry.BoxedValue is bool))
            {
                return;
            }

            bool value = (bool)entry.BoxedValue;
            bool changed = width > 0f ? GUILayout.Toggle(value, label, GUILayout.Width(width)) : GUILayout.Toggle(value, label);
            if (changed != value)
            {
                entry.BoxedValue = changed;
            }
        }

        private void Slider(string section, string key, string label, float min, float max, string format)
        {
            ConfigEntryBase entry = Entry(section, key);
            if (entry == null || !(entry.BoxedValue is float))
            {
                return;
            }

            float value = (float)entry.BoxedValue;
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, rich, GUILayout.Width(380f));
            float changed = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(220f));
            GUILayout.Label(changed.ToString(format, Invariant), rich, GUILayout.Width(60f));
            GUILayout.EndHorizontal();
            if (Mathf.Abs(changed - value) > 0.0001f)
            {
                entry.BoxedValue = (float)Math.Round(changed, 2);
            }
        }

        private void SliderInline(string section, string key, float min, float max, float width)
        {
            ConfigEntryBase entry = Entry(section, key);
            if (entry == null || !(entry.BoxedValue is float))
            {
                return;
            }

            float value = (float)entry.BoxedValue;
            GUILayout.BeginHorizontal(GUILayout.Width(width));
            float changed = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(width - 50f));
            GUILayout.Label(changed.ToString("0", Invariant), rich, GUILayout.Width(40f));
            GUILayout.EndHorizontal();
            if (Mathf.Abs(changed - value) > 0.0001f)
            {
                entry.BoxedValue = (float)Math.Round(changed);
            }
        }

        private void Stepper(string section, string key, string label, int min, int max)
        {
            ConfigEntryBase entry = Entry(section, key);
            if (entry == null || !(entry.BoxedValue is int))
            {
                return;
            }

            int value = (int)entry.BoxedValue;
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, rich, GUILayout.Width(380f));
            if (GUILayout.Button("-", GUILayout.Width(30f)) && value > min)
            {
                entry.BoxedValue = value - 1;
            }

            GUILayout.Label(value.ToString(Invariant), rich, GUILayout.Width(40f));
            if (GUILayout.Button("+", GUILayout.Width(30f)) && value < max)
            {
                entry.BoxedValue = value + 1;
            }

            GUILayout.EndHorizontal();
        }

        private void EnumCycle(string section, string key, string label)
        {
            ConfigEntryBase entry = Entry(section, key);
            if (entry == null || entry.BoxedValue == null || !entry.SettingType.IsEnum)
            {
                return;
            }

            Array values = Enum.GetValues(entry.SettingType);
            int index = Array.IndexOf(values, entry.BoxedValue);
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, rich, GUILayout.Width(380f));
            if (GUILayout.Button(entry.BoxedValue.ToString(), GUILayout.Width(200f)))
            {
                entry.BoxedValue = values.GetValue((index + 1) % values.Length);
            }

            GUILayout.EndHorizontal();
        }
    }
}
