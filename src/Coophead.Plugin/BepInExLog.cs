using BepInEx.Logging;
using Coophead.Core.Diagnostics;

namespace Coophead.Plugin
{
    /// <summary>Conecta el log del Core con el de BepInEx (BepInEx/LogOutput.log).</summary>
    internal sealed class BepInExLog : ICoreLog
    {
        private readonly ManualLogSource source;

        public BepInExLog(ManualLogSource source)
        {
            this.source = source;
        }

        public void Info(string message)
        {
            source.LogInfo(message);
        }

        public void Warning(string message)
        {
            source.LogWarning(message);
        }

        public void Error(string message)
        {
            source.LogError(message);
        }
    }
}
