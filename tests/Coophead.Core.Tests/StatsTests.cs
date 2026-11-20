using System;
using System.Collections.Generic;
using System.Linq;
using Coophead.Core.Integration;
using Coophead.Core.Roulette;
using Coophead.Core.Rules;
using Coophead.Core.Stats;
using Xunit;

namespace Coophead.Core.Tests
{
    public class StatsTests
    {
        private const PlayerSlot P1 = PlayerSlot.PlayerOne;
        private const PlayerSlot P2 = PlayerSlot.PlayerTwo;
        private static readonly DateTime Day = new DateTime(2026, 11, 19, 20, 0, 0, DateTimeKind.Utc);

        private static CoopheadSession NewSession()
        {
            var config = new CoopheadConfig();
            foreach (RouletteOutcome outcome in RouletteOutcomes.All())
            {
                config.Roulette.Get(outcome).Enabled = outcome == RouletteOutcome.DoublePunishment
                    || outcome == RouletteOutcome.SlowFire || outcome == RouletteOutcome.SlowMovement;
            }

            config.Roulette.Get(RouletteOutcome.DoublePunishment).Weight = 1000f;
            return new CoopheadSession(config, new FakeApplier(), null, new SequenceRandom(0.99, 0.1, 0.9));
        }

        /// <summary>Intento con daño, parrys, un rescate agradecido por voz y otro olvidado.</summary>
        private static LevelSummary PlayAttempt(CoopheadSession session, LevelEndReason end, float duration)
        {
            session.SetVoiceAvailable(true);
            session.StartLevel("FlowerLevel", 100f);
            session.RecordDamageDealt(P1, 120.5f);
            session.RecordDamageDealt(P2, 80f);
            session.RecordParry(P2);
            session.RecordParry(P2);
            session.RecordSuper(P1);
            session.RecordEx(P1);
            session.RecordHitTaken(P2);

            session.OnPlayerDied(P2, "a", 101f);
            session.OnPlayerRevived(P2, P1, "a", 102f);
            session.OnTranscript("gracias cari", 103f);

            session.OnPlayerDied(P1, "b", 110f);
            session.OnPlayerRevived(P1, P2, "b", 111f);
            session.Tick(116f); // se olvida de dar las gracias (plazo 3 s + margen de voz 1,5 s): castigo doble para P1

            return session.EndLevel(end, 100f + duration);
        }

        [Fact]
        public void LaSesionRegistraCombateGraciasYCastigos()
        {
            CoopheadSession session = NewSession();
            LevelSummary summary = PlayAttempt(session, LevelEndReason.Won, 90f);

            Assert.Equal(90f, summary.DurationSeconds, 3);
            Assert.Equal(120.5f, summary.PlayerOne.DamageDealt, 3);
            Assert.Equal(2, summary.PlayerTwo.Parries);
            Assert.Equal(1, summary.PlayerOne.Supers);
            Assert.Equal(1, summary.PlayerOne.ExMoves);
            Assert.Equal(1, summary.PlayerTwo.HitsTaken);
            Assert.Equal(1, summary.PlayerTwo.ThanksByVoice);
            Assert.Equal(0, summary.PlayerTwo.ThanksManual);
            Assert.Equal(1, summary.PlayerOne.ThanksMissed);
            Assert.Equal(1, summary.PlayerOne.DoublePunishments);
            Assert.Equal(2, RouletteOutcomes.AllEffects().Sum(e => summary.PlayerOne.GetEffectCount(e)));
        }

        [Fact]
        public void SinIntentoActivoNoSeCuentaCombate()
        {
            CoopheadSession session = NewSession();
            session.RecordParry(P1);
            session.StartLevel("FlowerLevel", 0f);

            Assert.Equal(0, session.Rules.Session.Get(P1).Parries);
        }

        [Fact]
        public void ElLibroAcumulaPorJefeYGuardaElTryFinal()
        {
            var book = new StatsBook();
            CoopheadSession session = NewSession();

            book.Record(PlayAttempt(session, LevelEndReason.Lost, 60f), Day);
            book.Record(PlayAttempt(session, LevelEndReason.Restarted, 30f), Day.AddMinutes(2));
            BossRecord record = book.Record(PlayAttempt(session, LevelEndReason.Won, 95.5f), Day.AddMinutes(5));

            Assert.Equal("Flower", record.DisplayName);
            Assert.Equal(3, record.Attempts);
            Assert.Equal(1, record.Wins);
            Assert.Equal(1, record.Losses);
            Assert.Equal(1, record.Restarts);
            Assert.Equal(95.5f, record.BestWinSeconds, 3);
            Assert.Equal(185.5f, record.TotalPlaySeconds, 3);
            Assert.Equal(6, record.PlayerTwo.Parries);
            Assert.Equal(3, record.PlayerTwo.ThanksVoice);
            Assert.Equal(361.5d, record.PlayerOne.DamageDealt, 3);
            Assert.NotNull(record.LastWin);
            Assert.Equal(120.5d, record.LastWin.PlayerOne.DamageDealt, 3);
            Assert.Equal("2026-11-19T20:05:00Z", record.LastWin.WhenUtc);
            Assert.Equal("2026-11-19T20:00:00Z", record.FirstPlayedUtc);
            Assert.Equal(3, book.TotalAttempts);
            Assert.Equal(1, book.TotalWins);
            Assert.Equal(6, book.GrandTotal(P2).Parries);
        }

        [Fact]
        public void SalirSinJugarNoSeApunta()
        {
            var book = new StatsBook();
            CoopheadSession session = NewSession();
            session.StartLevel("HouseLevel", 0f);

            Assert.Null(book.Record(session.EndLevel(LevelEndReason.Exited, 3f), Day));
            Assert.Equal(0, book.Count);
        }

        [Fact]
        public void ElLibroSobreviveAGuardarYCargar()
        {
            var book = new StatsBook();
            CoopheadSession session = NewSession();
            book.Record(PlayAttempt(session, LevelEndReason.Won, 95.5f), Day);
            session.StartLevel("DicePalaceMainLevel", 0f);
            session.RecordDamageDealt(P2, 12.25f);
            book.Record(session.EndLevel(LevelEndReason.Lost, 40f), Day.AddHours(1));

            string json = book.ToJson();
            StatsBook loaded = StatsBook.FromJson(json);

            Assert.Equal(json, loaded.ToJson());
            Assert.Equal(2, loaded.Count);
            Assert.Equal("Dice Palace Main", loaded.Records()[0].DisplayName);
            BossRecord flower = loaded.Get("FlowerLevel");
            Assert.Equal(1, flower.Wins);
            Assert.Equal(LevelEndReason.Won, flower.LastWin.Reason);
            Assert.Equal(2, RouletteOutcomes.AllEffects().Sum(e => flower.PlayerOne.EffectCounts[(int)e]));
        }

        [Fact]
        public void UnArchivoRotoSeRechaza()
        {
            Assert.Throws<FormatException>(() => StatsBook.FromJson("{ \"niveles\": [ "));
            Assert.Throws<FormatException>(() => StatsBook.FromJson("[]"));
            Assert.Equal(0, StatsBook.FromJson("{}").Count);
        }

        [Fact]
        public void MiniJsonIdaYVuelta()
        {
            var value = new Dictionary<string, object>
            {
                { "texto", "«Gracias», cari\n\"ok\"" },
                { "numero", 12.5d },
                { "entero", 3 },
                { "si", true },
                { "nada", null },
                { "lista", new List<object> { 1, "dos" } },
                { "vacio", new Dictionary<string, object>() },
            };

            var parsed = (Dictionary<string, object>)MiniJson.Parse(MiniJson.Serialize(value));

            Assert.Equal("«Gracias», cari\n\"ok\"", parsed["texto"]);
            Assert.Equal(12.5d, parsed["numero"]);
            Assert.Equal(3d, parsed["entero"]);
            Assert.Equal(true, parsed["si"]);
            Assert.Null(parsed["nada"]);
            Assert.Equal(2, ((List<object>)parsed["lista"]).Count);
            Assert.Equal("\u00e1", MiniJson.Parse("\"\\u00e1\""));
            Assert.Throws<FormatException>(() => MiniJson.Parse("{\"a\" 1}"));
            Assert.Throws<FormatException>(() => MiniJson.Parse("[1,2] x"));
        }

        [Theory]
        [InlineData("FlowerLevel", "Flower")]
        [InlineData("DicePalaceMainLevel", "Dice Palace Main")]
        [InlineData("RetroArcadeLevel", "Retro Arcade")]
        [InlineData("Level", "Level")]
        [InlineData("", "Nivel desconocido")]
        public void NombresLegibles(string key, string expected)
        {
            Assert.Equal(expected, LevelNames.Display(key));
        }

        [Fact]
        public void LaComparativaMarcaQuienTieneMas()
        {
            var p1 = new PlayerTotals { DamageDealt = 500.5d, Parries = 3, Deaths = 2 };
            var p2 = new PlayerTotals { DamageDealt = 300d, Parries = 5, Deaths = 2 };

            IList<ComparisonRow> rows = StatsComparison.Build(p1, p2);

            ComparisonRow damage = rows.First(r => r.Label == "Daño infligido");
            Assert.Equal("500.5", damage.PlayerOne);
            Assert.Equal(P1, damage.Leader);
            Assert.Equal(P2, rows.First(r => r.Label == "Parrys").Leader);
            Assert.Null(rows.First(r => r.Label == "Muertes").Leader);
        }

        [Theory]
        [InlineData(0f, "—")]
        [InlineData(95.4f, "1:35")]
        [InlineData(3725f, "1:02:05")]
        public void DuracionesLegibles(float seconds, string expected)
        {
            Assert.Equal(expected, StatsComparison.FormatDuration(seconds));
        }

        [Fact]
        public void ElTraductorEnviaElCombateSoloConReglasAplicadas()
        {
            bool apply = false;
            var session = new CoopheadSession(new CoopheadConfig(), new FakeApplier());
            var translator = new GameEventTranslator(session, null, () => apply, () => false);
            translator.LevelStarted("FlowerLevel", 1, 10f);

            translator.Parry(0);
            translator.DamageDealt(1, 50f);
            Assert.Equal(0, session.Rules.Session.Get(P1).Parries);

            apply = true;
            translator.Parry(0);
            translator.Super(1);
            translator.Ex(1);
            translator.HitTaken(0);
            translator.DamageDealt(1, 50f);
            translator.DamageDealt(1, -5f);
            translator.DamageDealt(7, 50f);

            Assert.Equal(1, session.Rules.Session.Get(P1).Parries);
            Assert.Equal(1, session.Rules.Session.Get(P1).HitsTaken);
            Assert.Equal(1, session.Rules.Session.Get(P2).Supers);
            Assert.Equal(1, session.Rules.Session.Get(P2).ExMoves);
            Assert.Equal(50f, session.Rules.Session.Get(P2).DamageDealt);

            LevelSummary summary = null;
            session.LevelEnded += s => summary = s;
            translator.LevelEnded(LevelEndReason.Won, "Level.OnWin", 1, 70f);
            Assert.Equal(60f, summary.DurationSeconds, 3);
        }
    }
}
