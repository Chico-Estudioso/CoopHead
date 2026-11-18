using System;
using BepInEx.Configuration;
using Coophead.Core.Configuration;

namespace Coophead.Plugin
{
    /// <summary>Implementa el esquema de ajustes del Core sobre el ConfigFile de BepInEx.</summary>
    internal sealed class BepInExConfigBinder : IConfigBinder
    {
        private readonly ConfigFile file;

        public BepInExConfigBinder(ConfigFile file)
        {
            this.file = file;
        }

        public IConfigValue<T> Bind<T>(string section, string key, T defaultValue, string description)
        {
            return new EntryValue<T>(file.Bind(section, key, defaultValue, description));
        }

        public IConfigValue<T> BindRange<T>(string section, string key, T defaultValue, T min, T max, string description)
            where T : IComparable
        {
            var range = new AcceptableValueRange<T>(min, max);
            return new EntryValue<T>(file.Bind(section, key, defaultValue, new ConfigDescription(description, range)));
        }

        private sealed class EntryValue<T> : IConfigValue<T>
        {
            private readonly ConfigEntry<T> entry;

            public EntryValue(ConfigEntry<T> entry)
            {
                this.entry = entry;
            }

            public T Value
            {
                get { return entry.Value; }
            }
        }
    }
}
