using System;
using System.Collections.Generic;
using System.Linq;
using Coophead.Core.Presentation;
using Coophead.Core.Roulette;
using Coophead.Core.Rules;
using Coophead.Core.Stats;
using Xunit;

namespace Coophead.Core.Tests
{
    public class BookViewsTests
    {
        [Fact]
        public void LaRuletaMuestraProbabilidadesBaseSinNivel()
        {
            var config = new CoopheadConfig();
            var session = new CoopheadSession(config, new FakeApplier());

            IList<RouletteRow> rows = BookViews.Roulette(session, config, 0f);

            Assert.Equal(6, rows.Count);
            Assert.Equal(1f, rows.Sum(r => r.ProbabilityPlayerOne), 3);
            Assert.Equal(0.2f, rows.First(r => r.Outcome == RouletteOutcome.NoPenalty).ProbabilityPlayerTwo, 3);
            Assert.Equal("20 %", BookViews.Percent(0.2f));
        }

        [Fact]
        public void LaPresionCambiaLasProbabilidadesDeQuienMuere()
        {
            var config = new CoopheadConfig();
            var session = new CoopheadSession(config, new FakeApplier());
            session.StartLevel("FlowerLevel", 0f);
            for (int i = 0; i < 2; i++)
            {
                session.OnPlayerDied(PlayerSlot.PlayerTwo, "g" + i, i);
                session.OnPlayerRevived(PlayerSlot.PlayerTwo, PlayerSlot.PlayerOne, "g" + i, i + 0.5f);
                session.OnManualThanks(PlayerSlot.PlayerTwo, i + 0.6f);
            }

            RouletteRow noPenalty = BookViews.Roulette(session, config, 5f).First(r => r.Outcome == RouletteOutcome.NoPenalty);

            Assert.True(noPenalty.ProbabilityPlayerTwo > noPenalty.ProbabilityPlayerOne);
            Assert.StartsWith("P2: presión", BookViews.Pressure(session, PlayerSlot.PlayerTwo, 5f));
        }

        [Fact]
        public void LaComparativaEnVivoSoloConNivelActivo()
        {
            var session = new CoopheadSession(new CoopheadConfig(), new FakeApplier());
            Assert.Null(BookViews.CurrentAttempt(session));

            session.StartLevel("FlowerLevel", 0f);
            session.RecordParry(PlayerSlot.PlayerOne);

            Assert.Equal(PlayerSlot.PlayerOne, BookViews.CurrentAttempt(session).First(r => r.Label == "Parrys").Leader);
        }

        [Fact]
        public void CabecerasDelLibro()
        {
            var book = new StatsBook();
            var session = new CoopheadSession(new CoopheadConfig(), new FakeApplier());
            session.StartLevel("FlowerLevel", 0f);
            session.RecordDamageDealt(PlayerSlot.PlayerOne, 10f);
            BossRecord record = book.Record(session.EndLevel(LevelEndReason.Won, 75f), new DateTime(2026, 11, 19, 20, 0, 0, DateTimeKind.Utc));

            IList<string> header = BookViews.RecordHeader(record);

            Assert.Equal("Intentos 1 · Victorias 1 · Derrotas 0 · Reinicios 0 · Salidas 0", header[0]);
            Assert.Equal("Tiempo jugado 1:15 · Mejor victoria 1:15", header[1]);
            Assert.Contains("victoria · 1:15", BookViews.AttemptHeader(record.LastWin));
            Assert.Equal("Todavía no hay ninguno.", BookViews.AttemptHeader(null));
            Assert.Equal("Niveles 1 · Intentos 1 · Victorias 1", BookViews.BookHeader(book)[0]);
        }

        [Theory]
        [InlineData("", "—")]
        [InlineData("ayer", "—")]
        public void FechasNoValidas(string iso, string expected)
        {
            Assert.Equal(expected, BookViews.FormatDate(iso));
        }

        [Fact]
        public void FechaValidaEnHoraLocal()
        {
            string expected = new DateTime(2026, 11, 19, 20, 5, 0, DateTimeKind.Utc).ToLocalTime().ToString("dd/MM/yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture);
            Assert.Equal(expected, BookViews.FormatDate("2026-11-19T20:05:00Z"));
        }
    }
}
