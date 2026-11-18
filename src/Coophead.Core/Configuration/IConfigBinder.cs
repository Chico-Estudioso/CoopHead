using System;

namespace Coophead.Core.Configuration
{
    /// <summary>Valor configurable que se lee en cada momento (el usuario puede cambiarlo en caliente).</summary>
    public interface IConfigValue<T>
    {
        T Value { get; }
    }

    /// <summary>
    /// Abstracción del sistema de configuración. El plugin la implementa con el ConfigFile de BepInEx; las pruebas,
    /// con un diccionario. Así el esquema de ajustes se prueba sin cargar BepInEx.
    /// </summary>
    public interface IConfigBinder
    {
        IConfigValue<T> Bind<T>(string section, string key, T defaultValue, string description);

        IConfigValue<T> BindRange<T>(string section, string key, T defaultValue, T min, T max, string description)
            where T : IComparable;
    }
}
