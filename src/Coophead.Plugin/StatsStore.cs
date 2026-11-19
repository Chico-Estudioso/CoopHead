using System;
using System.IO;
using System.Text;
using Coophead.Core.Diagnostics;
using Coophead.Core.Stats;

namespace Coophead.Plugin
{
    /// <summary>
    /// Guarda el libro de recuerdos en BepInEx/config/Coophead/estadisticas.json. Escribe primero un archivo temporal
    /// y conserva una copia .bak, para no perder el historial si el juego se cierra a medio guardar.
    /// </summary>
    internal sealed class StatsStore
    {
        private readonly string path;
        private readonly ICoreLog log;

        public StatsStore(string directory, ICoreLog log)
        {
            path = Path.Combine(directory, "estadisticas.json");
            this.log = log;
        }

        public string FilePath
        {
            get { return path; }
        }

        public StatsBook Load()
        {
            if (!File.Exists(path))
            {
                return new StatsBook();
            }

            try
            {
                StatsBook book = StatsBook.FromJson(File.ReadAllText(path, Encoding.UTF8));
                log.Info("Libro de recuerdos cargado: " + book.Count + " niveles, " + book.TotalAttempts + " intentos.");
                return book;
            }
            catch (Exception e)
            {
                // No se pisa un archivo que no se entiende: se aparta para poder recuperarlo a mano.
                string aside = path + ".ilegible-" + DateTime.Now.ToString("yyyyMMddHHmmss");
                try
                {
                    File.Move(path, aside);
                }
                catch (Exception)
                {
                    // Si ni siquiera se puede mover, se empieza un libro nuevo en memoria igualmente.
                }

                log.Warning("No se pudo leer el libro de recuerdos (" + e.Message + "). Se aparta como " + Path.GetFileName(aside) + " y se empieza uno nuevo.");
                return new StatsBook();
            }
        }

        public void Save(StatsBook book)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string temp = path + ".tmp";
                File.WriteAllText(temp, book.ToJson(), new UTF8Encoding(false));
                if (File.Exists(path))
                {
                    File.Copy(path, path + ".bak", true);
                    File.Delete(path);
                }

                File.Move(temp, path);
            }
            catch (Exception e)
            {
                log.Error("No se pudo guardar el libro de recuerdos: " + e.Message);
            }
        }
    }
}
