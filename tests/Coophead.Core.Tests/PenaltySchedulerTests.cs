using Coophead.Core.Roulette;
using Xunit;

namespace Coophead.Core.Tests
{
    public class PenaltySchedulerTests
    {
        private const PlayerSlot P1 = PlayerSlot.PlayerOne;
        private const PlayerSlot P2 = PlayerSlot.PlayerTwo;

        [Fact]
        public void ElEfectoSeAplicaYSeRestauraAlExpirar()
        {
            var applier = new FakeApplier();
            var scheduler = new PenaltyScheduler(applier, null);

            Assert.True(scheduler.Start(P1, PenaltyEffect.SlowFire, 4f, 10f));
            Assert.Contains("P1 SlowFire", applier.Active);

            scheduler.Tick(13.9f);
            Assert.Contains("P1 SlowFire", applier.Active);

            scheduler.Tick(14f);
            Assert.Empty(applier.Active);
            Assert.Empty(scheduler.Active);
        }

        [Fact]
        public void ElMismoEfectoNoSeApilaSoloSeAmplia()
        {
            var applier = new FakeApplier();
            var scheduler = new PenaltyScheduler(applier, null);
            scheduler.Start(P1, PenaltyEffect.SlowMovement, 4f, 0f);

            scheduler.Start(P1, PenaltyEffect.SlowMovement, 4f, 2f);

            Assert.Single(applier.Calls);
            Assert.Equal(6f, scheduler.Active[0].EndTime);
        }

        [Fact]
        public void LimpiarTodoRestauraTodosLosEfectos()
        {
            var applier = new FakeApplier();
            var scheduler = new PenaltyScheduler(applier, null);
            scheduler.Start(P1, PenaltyEffect.SlowFire, 4f, 0f);
            scheduler.Start(P1, PenaltyEffect.InvertedControls, 4f, 0f);
            scheduler.Start(P2, PenaltyEffect.LimitedDash, 4f, 0f);

            scheduler.ClearAll();

            Assert.Empty(applier.Active);
            Assert.Empty(scheduler.Active);
        }

        [Fact]
        public void LimpiarUnJugadorNoTocaAlOtro()
        {
            var applier = new FakeApplier();
            var scheduler = new PenaltyScheduler(applier, null);
            scheduler.Start(P1, PenaltyEffect.SlowFire, 4f, 0f);
            scheduler.Start(P2, PenaltyEffect.SlowFire, 4f, 0f);

            scheduler.ClearPlayer(P1);

            Assert.DoesNotContain("P1 SlowFire", applier.Active);
            Assert.Contains("P2 SlowFire", applier.Active);
        }

        [Fact]
        public void SiAplicarFallaSeIntentaRestaurarYNoQuedaActivo()
        {
            var applier = new FakeApplier { ThrowOnApply = PenaltyEffect.LimitedDash };
            var log = new ListLog();
            var scheduler = new PenaltyScheduler(applier, null, log);

            Assert.False(scheduler.Start(P1, PenaltyEffect.LimitedDash, 4f, 0f));

            Assert.Empty(scheduler.Active);
            Assert.Contains("restore P1 LimitedDash", applier.Calls);
            Assert.Contains(log.Entries, e => e.StartsWith("ERROR"));
        }

        [Fact]
        public void SiRestaurarFallaElEfectoSeRetiraIgualmente()
        {
            var applier = new FakeApplier { ThrowOnRestore = PenaltyEffect.SlowFire };
            var scheduler = new PenaltyScheduler(applier, null, new ListLog());
            scheduler.Start(P1, PenaltyEffect.SlowFire, 4f, 0f);
            scheduler.Start(P1, PenaltyEffect.SlowMovement, 4f, 0f);

            scheduler.ClearAll();

            Assert.Empty(scheduler.Active);
            Assert.Contains("restore P1 SlowMovement", applier.Calls);
        }

        [Fact]
        public void NoSeAplicaUnEfectoIncompatibleConOtroActivo()
        {
            var compatibility = new EffectCompatibility();
            compatibility.MarkIncompatible(PenaltyEffect.SlowMovement, PenaltyEffect.LimitedDash);
            var applier = new FakeApplier();
            var scheduler = new PenaltyScheduler(applier, compatibility, new ListLog());
            scheduler.Start(P1, PenaltyEffect.SlowMovement, 4f, 0f);

            Assert.False(scheduler.Start(P1, PenaltyEffect.LimitedDash, 4f, 0f));
            Assert.True(scheduler.Start(P2, PenaltyEffect.LimitedDash, 4f, 0f));
        }

        [Fact]
        public void DuracionNoPositivaNoAplicaNada()
        {
            var applier = new FakeApplier();
            var scheduler = new PenaltyScheduler(applier, null);

            Assert.False(scheduler.Start(P1, PenaltyEffect.SlowFire, 0f, 0f));
            Assert.Empty(applier.Calls);
        }
    }
}
