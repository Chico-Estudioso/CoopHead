namespace Coophead.Core.Diagnostics
{
    /// <summary>
    /// Registro mínimo que el plugin conecta con el log de BepInEx.
    /// No debe recibir datos personales, rutas privadas ni audio.
    /// </summary>
    public interface ICoreLog
    {
        void Info(string message);
        void Warning(string message);
        void Error(string message);
    }

    /// <summary>Log que descarta todo; útil por defecto y en pruebas.</summary>
    public sealed class NullLog : ICoreLog
    {
        public static readonly NullLog Instance = new NullLog();

        private NullLog()
        {
        }

        public void Info(string message)
        {
        }

        public void Warning(string message)
        {
        }

        public void Error(string message)
        {
        }
    }
}
