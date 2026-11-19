using System.Collections.Generic;
using Coophead.Core.Presentation;
using Coophead.Core.Roulette;
using Coophead.Core.Rules;
using Xunit;

namespace Coophead.Core.Tests
{
    public class HudFormatterTests
    {
        private const PlayerSlot P1 = PlayerSlot.PlayerOne;
        private const PlayerSlot P2 = PlayerSlot.PlayerTwo;

        private static CoopheadSession NewSession(CoopheadConfig config)
        {
            foreach (RouletteOutcome outcome in RouletteOutcomes.All())
            {
                config.Roulette.Get(outcome).Enabled = outcome == RouletteOutcome.SlowFire;
            }

            return new CoopheadSession(config, new FakeApplier(), null, new SequenceRandom(0.5));
        }

        [Fact]
        public void SinNivelIndicaQueEspera()
        {
            var config = new CoopheadConfig();
            IList<string> lines = HudFormatter.BuildLines(NewSession(config), config, new HudContext(), 0f);

            Assert.Equal("Coophead · solo registro", lines[0]);
            Assert.Equal("Esperando a que empiece un nivel", lines[1]);
        }

        [Fact]
        public void MuestraContadoresVentanaYEfectos()
        {
            var config = new CoopheadConfig();
            CoopheadSession session = NewSession(config);
            session.StartLevel("FlowerLevel");
            session.OnPlayerDied(P2, "g1", 9f);
            session.OnPlayerRevived(P2, P1, "g1", 10f);

            IList<string> during = HudFormatter.BuildLines(session, config, new HudContext { RulesApplied = true }, 11f);
            Assert.Equal("Coophead · reglas activas", during[0]);
            Assert.Equal("P1: recibidas 0/3 · dadas 1 · muertes 0 · crédito 1", during[1]);
            Assert.Equal("P2: recibidas 1/3 · dadas 0 · muertes 1", during[2]);
            Assert.Equal("Equipo: 1/6 reanimaciones", during[3]);
            Assert.Contains("P2: ¡da las gracias! 2.0 s", during);
            Assert.Contains("Agradecimiento: voz no disponible · tecla activa", during);

            session.Tick(13f);
            IList<string> after = HudFormatter.BuildLines(session, config, new HudContext { RulesApplied = true }, 14f);
            Assert.Contains(after, line => line.StartsWith("P2: Disparo lento ") && !line.EndsWith("(simulado)"));

            IList<string> simulated = HudFormatter.BuildLines(session, config, new HudContext { RulesApplied = true, EffectsSimulated = true }, 14f);
            Assert.Contains(simulated, line => line.StartsWith("P2: Disparo lento ") && line.EndsWith("(simulado)"));
        }

        [Fact]
        public void IndicaFantasmaYLimiteAlcanzado()
        {
            var config = new CoopheadConfig();
            config.Rules.TeamReviveLimit = 1;
            CoopheadSession session = NewSession(config);
            session.StartLevel("FlowerLevel");
            session.OnPlayerDied(P1, "a", 1f);
            IList<string> dead = HudFormatter.BuildLines(session, config, new HudContext(), 1f);
            Assert.StartsWith("P1 (fantasma)", dead[1]);

            session.OnPlayerRevived(P1, P2, "a", 2f);
            IList<string> lines = HudFormatter.BuildLines(session, config, new HudContext(), 2f);
            Assert.Contains("¡Límite alcanzado! (equipo) · reiniciando", lines);
        }

        [Fact]
        public void SinReinicioAutomaticoElLimiteSeVeIgualmente()
        {
            var config = new CoopheadConfig();
            config.Rules.AutoRestartEnabled = false;
            CoopheadSession session = NewSession(config);
            session.StartLevel("FlowerLevel");
            for (int i = 0; i < 3; i++)
            {
                session.OnPlayerDied(P2, "g" + i, i * 10f);
                session.OnPlayerRevived(P2, P1, "g" + i, (i * 10f) + 1f);
                session.OnManualThanks(P2, (i * 10f) + 2f);
            }

            IList<string> lines = HudFormatter.BuildLines(session, config, new HudContext(), 30f);

            Assert.Contains("¡Límite alcanzado! (individual) · reinicio automático desactivado", lines);
        }

        [Fact]
        public void MuestraElTotalCuandoLosCreditosLoReducen()
        {
            var config = new CoopheadConfig();
            CoopheadSession session = NewSession(config);
            session.StartLevel("FlowerLevel");
            session.OnPlayerDied(P1, "a", 1f);
            session.OnPlayerRevived(P1, P2, "a", 2f); // P1 recibe 1
            session.OnPlayerDied(P2, "b", 3f);
            session.OnPlayerRevived(P2, P1, "b", 4f); // P1 rescata: su neto baja a 0

            IList<string> lines = HudFormatter.BuildLines(session, config, new HudContext(), 5f);

            Assert.StartsWith("P1: recibidas 0/3 (total 1)", lines[1]);
        }

        [Fact]
        public void ElResumenIncluyeAmbosJugadores()
        {
            var config = new CoopheadConfig();
            CoopheadSession session = NewSession(config);
            session.StartLevel("FlowerLevel");
            session.OnPlayerDied(P1, "a", 1f);
            session.OnPlayerRevived(P1, P2, "a", 2f);
            session.OnManualThanks(P1, 3f);

            IList<string> summary = HudFormatter.BuildSummary(session.EndLevel(LevelEndReason.Won));

            Assert.Equal("Resumen · FlowerLevel (victoria)", summary[0]);
            Assert.Equal("P1: muertes 1 · rescates dados 0 · recibidos 1 · créditos usados 0 · gracias 1 · castigos 0", summary[1]);
            Assert.Equal("P2: muertes 0 · rescates dados 1 · recibidos 0 · créditos usados 0 · gracias 0 · castigos 0", summary[2]);
            Assert.Equal("Equipo: 1 reanimación", summary[3]);
        }

        [Fact]
        public void SinExigirAgradecimientoLoIndica()
        {
            var config = new CoopheadConfig();
            config.Thanks.RequireThanks = false;
            CoopheadSession session = NewSession(config);
            session.StartLevel("FlowerLevel");

            Assert.Contains("Agradecimiento: no se exige", HudFormatter.BuildLines(session, config, new HudContext(), 0f));
        }
    }
}
