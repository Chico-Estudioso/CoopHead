using System;
using System.Collections.Generic;
using Coophead.Core.Configuration;
using Coophead.Core.Roulette;
using Xunit;

namespace Coophead.Core.Tests
{
    public class CoopheadSettingsTests
    {
        private sealed class FakeBinder : IConfigBinder
        {
            public readonly Dictionary<string, object> Overrides = new Dictionary<string, object>();
            public readonly List<string> Keys = new List<string>();
            public readonly Dictionary<string, string> Descriptions = new Dictionary<string, string>();

            public IConfigValue<T> Bind<T>(string section, string key, T defaultValue, string description)
            {
                return Register(section, key, defaultValue, description);
            }

            public IConfigValue<T> BindRange<T>(string section, string key, T defaultValue, T min, T max, string description)
                where T : IComparable
            {
                Assert.True(min.CompareTo(defaultValue) <= 0 && max.CompareTo(defaultValue) >= 0, key + " tiene el valor por defecto fuera de rango");
                return Register(section, key, defaultValue, description);
            }

            private IConfigValue<T> Register<T>(string section, string key, T defaultValue, string description)
            {
                string fullKey = section + "/" + key;
                Assert.DoesNotContain(fullKey, Keys);
                Keys.Add(fullKey);
                Descriptions[fullKey] = description;
                return new FakeValue<T>(this, fullKey, defaultValue);
            }

            private sealed class FakeValue<T> : IConfigValue<T>
            {
                private readonly FakeBinder owner;
                private readonly string key;
                private readonly T defaultValue;

                public FakeValue(FakeBinder owner, string key, T defaultValue)
                {
                    this.owner = owner;
                    this.key = key;
                    this.defaultValue = defaultValue;
                }

                public T Value
                {
                    get
                    {
                        object value;
                        return owner.Overrides.TryGetValue(key, out value) ? (T)value : defaultValue;
                    }
                }
            }
        }

        [Fact]
        public void LosValoresPorDefectoCoincidenConElCoreSalvoElReinicio()
        {
            var binder = new FakeBinder();
            IList<string> warnings;

            CoopheadConfig config = CoopheadSettings.Bind(binder).Build(out warnings);
            var reference = new CoopheadConfig();

            Assert.Empty(warnings);
            Assert.False(config.Rules.AutoRestartEnabled);
            Assert.Equal(reference.Rules.PlayerOneReviveLimit, config.Rules.PlayerOneReviveLimit);
            Assert.Equal(reference.Rules.TeamReviveLimit, config.Rules.TeamReviveLimit);
            Assert.Equal(reference.Roulette.DefaultDurationSeconds, config.Roulette.DefaultDurationSeconds);
            Assert.Equal(reference.Roulette.CooldownSeconds, config.Roulette.CooldownSeconds);
            Assert.Equal(0f, config.Roulette.FocusIntensity);
            Assert.Null(config.Roulette.DebugSeed);
            Assert.Equal(reference.Balance.MaxPressure, config.Balance.MaxPressure);
            Assert.Equal(reference.Thanks.WindowSeconds, config.Thanks.WindowSeconds);
            Assert.Equal(reference.Thanks.AcceptedNames, config.Thanks.AcceptedNames);
            foreach (RouletteOutcome outcome in RouletteOutcomes.All())
            {
                Assert.Equal(reference.Roulette.Get(outcome).Weight, config.Roulette.Get(outcome).Weight);
                Assert.Equal(reference.Roulette.Get(outcome).Enabled, config.Roulette.Get(outcome).Enabled);
            }
        }

        [Fact]
        public void LosCambiosDelUsuarioLleganALaConfiguracion()
        {
            var binder = new FakeBinder();
            CoopheadSettings settings = CoopheadSettings.Bind(binder);
            binder.Overrides[CoopheadSettings.RulesSection + "/LimiteReanimacionesEquipo"] = 9;
            binder.Overrides[CoopheadSettings.RouletteSection + "/CastigoDobleActivado"] = false;
            binder.Overrides[CoopheadSettings.RouletteSection + "/DisparoLentoDuracion"] = 6f;
            binder.Overrides[CoopheadSettings.RouletteSection + "/SemillaDepuracion"] = 77;
            binder.Overrides[CoopheadSettings.RouletteSection + "/ObjetivoCastigo"] = PenaltyTargetMode.Rescuer;
            binder.Overrides[CoopheadSettings.ThanksSection + "/Apelativos"] = " cari , cielo,, cari ";
            binder.Overrides[CoopheadSettings.ThanksSection + "/ExigirApelativo"] = true;

            IList<string> warnings;
            CoopheadConfig config = settings.Build(out warnings);

            Assert.Empty(warnings);
            Assert.Equal(9, config.Rules.TeamReviveLimit);
            Assert.False(config.Roulette.Get(RouletteOutcome.DoublePunishment).Enabled);
            Assert.Equal(6f, config.Roulette.GetBaseDuration(PenaltyEffect.SlowFire));
            Assert.Equal(4f, config.Roulette.GetBaseDuration(PenaltyEffect.SlowMovement));
            Assert.Equal(77, config.Roulette.DebugSeed);
            Assert.Equal(PenaltyTargetMode.Rescuer, config.PenaltyTarget);
            Assert.Equal(new[] { "cari", "cielo" }, config.Thanks.AcceptedNames);
            Assert.True(config.Thanks.RequireName);
        }

        [Fact]
        public void AplicarDeNuevoQuitaLaDuracionEspecifica()
        {
            var binder = new FakeBinder();
            CoopheadSettings settings = CoopheadSettings.Bind(binder);
            string key = CoopheadSettings.RouletteSection + "/DisparoLentoDuracion";
            binder.Overrides[key] = 6f;
            var config = new CoopheadConfig();
            settings.ApplyTo(config);

            binder.Overrides[key] = 0f;
            settings.ApplyTo(config);

            Assert.Equal(config.Roulette.DefaultDurationSeconds, config.Roulette.GetBaseDuration(PenaltyEffect.SlowFire));
        }

        [Fact]
        public void TodasLasClavesSonUnicasYTienenDescripcion()
        {
            var binder = new FakeBinder();
            CoopheadSettings.Bind(binder);

            Assert.True(binder.Keys.Count > 40);
            foreach (KeyValuePair<string, string> entry in binder.Descriptions)
            {
                Assert.False(string.IsNullOrEmpty(entry.Value), entry.Key + " sin descripción");
                Assert.DoesNotMatch("[=\\n\\t\\\\\"'\\[\\]]", entry.Key.Substring(entry.Key.IndexOf('/') + 1));
            }
        }

        [Fact]
        public void ParseListLimpiaYQuitaDuplicados()
        {
            Assert.Equal(new[] { "cari", "amor" }, CoopheadSettings.ParseList("cari, amor ,cari,,"));
            Assert.Empty(CoopheadSettings.ParseList(null));
        }
    }
}
