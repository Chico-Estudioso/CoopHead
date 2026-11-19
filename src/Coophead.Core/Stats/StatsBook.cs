using System;
using System.Collections.Generic;
using System.Globalization;
using Coophead.Core.Roulette;
using Coophead.Core.Rules;

namespace Coophead.Core.Stats
{
    /// <summary>Estadísticas sumadas de un jugador (de un intento o acumuladas).</summary>
    public sealed class PlayerTotals
    {
        public PlayerTotals()
        {
            EffectCounts = new int[RouletteOutcomes.AllEffects().Length];
        }

        public int Deaths { get; set; }

        public int RevivesGiven { get; set; }

        public int RevivesReceived { get; set; }

        public int CreditsEarned { get; set; }

        public int CreditsConsumed { get; set; }

        public int Parries { get; set; }

        public int Supers { get; set; }

        public int ExMoves { get; set; }

        public int HitsTaken { get; set; }

        public double DamageDealt { get; set; }

        public int ThanksVoice { get; set; }

        public int ThanksManual { get; set; }

        public int ThanksMissed { get; set; }

        public int Penalties { get; set; }

        public int DoublePunishments { get; set; }

        /// <summary>Castigos recibidos por efecto, en el orden de <see cref="RouletteOutcomes.AllEffects"/>.</summary>
        public int[] EffectCounts { get; private set; }

        public int ThanksTotal
        {
            get { return ThanksVoice + ThanksManual; }
        }

        public static PlayerTotals From(PlayerRoundStats stats)
        {
            var totals = new PlayerTotals();
            totals.Add(stats);
            return totals;
        }

        public void Add(PlayerRoundStats stats)
        {
            Deaths += stats.Deaths;
            RevivesGiven += stats.RevivesGiven;
            RevivesReceived += stats.TotalRevivesReceived;
            CreditsEarned += stats.CreditsEarned;
            CreditsConsumed += stats.CreditsConsumed;
            Parries += stats.Parries;
            Supers += stats.Supers;
            ExMoves += stats.ExMoves;
            HitsTaken += stats.HitsTaken;
            DamageDealt += stats.DamageDealt;
            ThanksVoice += stats.ThanksByVoice;
            ThanksManual += stats.ThanksManual;
            ThanksMissed += stats.ThanksMissed;
            Penalties += stats.PenaltiesReceived;
            DoublePunishments += stats.DoublePunishments;
            foreach (PenaltyEffect effect in RouletteOutcomes.AllEffects())
            {
                EffectCounts[(int)effect] += stats.GetEffectCount(effect);
            }
        }

        public void Add(PlayerTotals other)
        {
            Deaths += other.Deaths;
            RevivesGiven += other.RevivesGiven;
            RevivesReceived += other.RevivesReceived;
            CreditsEarned += other.CreditsEarned;
            CreditsConsumed += other.CreditsConsumed;
            Parries += other.Parries;
            Supers += other.Supers;
            ExMoves += other.ExMoves;
            HitsTaken += other.HitsTaken;
            DamageDealt += other.DamageDealt;
            ThanksVoice += other.ThanksVoice;
            ThanksManual += other.ThanksManual;
            ThanksMissed += other.ThanksMissed;
            Penalties += other.Penalties;
            DoublePunishments += other.DoublePunishments;
            for (int i = 0; i < EffectCounts.Length; i++)
            {
                EffectCounts[i] += other.EffectCounts[i];
            }
        }

        /// <summary>true si no ha pasado nada medible (sirve para descartar entradas y salidas sin jugar).</summary>
        public bool IsEmpty
        {
            get
            {
                return Deaths == 0 && RevivesGiven == 0 && Parries == 0 && Supers == 0 && ExMoves == 0
                    && HitsTaken == 0 && DamageDealt <= 0d && Penalties == 0 && ThanksTotal == 0;
            }
        }

        internal Dictionary<string, object> ToJson()
        {
            var effects = new List<object>();
            foreach (int count in EffectCounts)
            {
                effects.Add(count);
            }

            return new Dictionary<string, object>
            {
                { "muertes", Deaths },
                { "rescatesDados", RevivesGiven },
                { "reanimacionesRecibidas", RevivesReceived },
                { "creditosGanados", CreditsEarned },
                { "creditosUsados", CreditsConsumed },
                { "parrys", Parries },
                { "supers", Supers },
                { "ex", ExMoves },
                { "golpesRecibidos", HitsTaken },
                { "danoInfligido", DamageDealt },
                { "graciasVoz", ThanksVoice },
                { "graciasTecla", ThanksManual },
                { "graciasOlvidadas", ThanksMissed },
                { "castigos", Penalties },
                { "castigosDobles", DoublePunishments },
                { "castigosPorEfecto", effects },
            };
        }

        internal static PlayerTotals FromJson(object value)
        {
            var totals = new PlayerTotals();
            var map = value as Dictionary<string, object>;
            if (map == null)
            {
                return totals;
            }

            totals.Deaths = JsonRead.Int(map, "muertes");
            totals.RevivesGiven = JsonRead.Int(map, "rescatesDados");
            totals.RevivesReceived = JsonRead.Int(map, "reanimacionesRecibidas");
            totals.CreditsEarned = JsonRead.Int(map, "creditosGanados");
            totals.CreditsConsumed = JsonRead.Int(map, "creditosUsados");
            totals.Parries = JsonRead.Int(map, "parrys");
            totals.Supers = JsonRead.Int(map, "supers");
            totals.ExMoves = JsonRead.Int(map, "ex");
            totals.HitsTaken = JsonRead.Int(map, "golpesRecibidos");
            totals.DamageDealt = JsonRead.Double(map, "danoInfligido");
            totals.ThanksVoice = JsonRead.Int(map, "graciasVoz");
            totals.ThanksManual = JsonRead.Int(map, "graciasTecla");
            totals.ThanksMissed = JsonRead.Int(map, "graciasOlvidadas");
            totals.Penalties = JsonRead.Int(map, "castigos");
            totals.DoublePunishments = JsonRead.Int(map, "castigosDobles");
            object effects;
            var list = map.TryGetValue("castigosPorEfecto", out effects) ? effects as List<object> : null;
            if (list != null)
            {
                for (int i = 0; i < totals.EffectCounts.Length && i < list.Count; i++)
                {
                    totals.EffectCounts[i] = (int)JsonRead.ToDouble(list[i]);
                }
            }

            return totals;
        }
    }

    /// <summary>Un intento concreto (el último jugado o la última victoria).</summary>
    public sealed class AttemptRecord
    {
        public AttemptRecord()
        {
            WhenUtc = string.Empty;
            PlayerOne = new PlayerTotals();
            PlayerTwo = new PlayerTotals();
        }

        /// <summary>Fecha y hora UTC en formato ISO 8601.</summary>
        public string WhenUtc { get; set; }

        public float DurationSeconds { get; set; }

        public LevelEndReason Reason { get; set; }

        public PlayerTotals PlayerOne { get; private set; }

        public PlayerTotals PlayerTwo { get; private set; }

        public PlayerTotals Get(PlayerSlot slot)
        {
            return slot == PlayerSlot.PlayerOne ? PlayerOne : PlayerTwo;
        }

        internal static AttemptRecord From(LevelSummary summary, string whenUtc)
        {
            var attempt = new AttemptRecord
            {
                WhenUtc = whenUtc,
                DurationSeconds = summary.DurationSeconds,
                Reason = summary.Reason,
            };
            attempt.PlayerOne.Add(summary.PlayerOne);
            attempt.PlayerTwo.Add(summary.PlayerTwo);
            return attempt;
        }

        internal Dictionary<string, object> ToJson()
        {
            return new Dictionary<string, object>
            {
                { "fecha", WhenUtc },
                { "duracion", DurationSeconds },
                { "final", Reason.ToString() },
                { "p1", PlayerOne.ToJson() },
                { "p2", PlayerTwo.ToJson() },
            };
        }

        internal static AttemptRecord FromJson(object value)
        {
            var map = value as Dictionary<string, object>;
            if (map == null)
            {
                return null;
            }

            var attempt = new AttemptRecord
            {
                WhenUtc = JsonRead.String(map, "fecha"),
                DurationSeconds = (float)JsonRead.Double(map, "duracion"),
                Reason = JsonRead.EndReason(map, "final"),
            };
            object p1;
            object p2;
            map.TryGetValue("p1", out p1);
            map.TryGetValue("p2", out p2);
            attempt.PlayerOne.Add(PlayerTotals.FromJson(p1));
            attempt.PlayerTwo.Add(PlayerTotals.FromJson(p2));
            return attempt;
        }
    }

    /// <summary>Recuerdos acumulados de un nivel (jefe, plataformas…).</summary>
    public sealed class BossRecord
    {
        public BossRecord(string levelKey)
        {
            LevelKey = levelKey ?? string.Empty;
            FirstPlayedUtc = string.Empty;
            LastPlayedUtc = string.Empty;
            FirstWinUtc = string.Empty;
            PlayerOne = new PlayerTotals();
            PlayerTwo = new PlayerTotals();
        }

        public string LevelKey { get; private set; }

        public string DisplayName
        {
            get { return LevelNames.Display(LevelKey); }
        }

        public int Attempts { get; set; }

        public int Wins { get; set; }

        public int Losses { get; set; }

        public int Restarts { get; set; }

        public int Exits { get; set; }

        /// <summary>Mejor tiempo de victoria en segundos; 0 si aún no hay victoria con duración conocida.</summary>
        public float BestWinSeconds { get; set; }

        public float TotalPlaySeconds { get; set; }

        public string FirstPlayedUtc { get; set; }

        public string LastPlayedUtc { get; set; }

        public string FirstWinUtc { get; set; }

        /// <summary>Totales de todos los intentos.</summary>
        public PlayerTotals PlayerOne { get; private set; }

        public PlayerTotals PlayerTwo { get; private set; }

        /// <summary>El intento de la última victoria («el try final»), o null.</summary>
        public AttemptRecord LastWin { get; set; }

        public AttemptRecord LastAttempt { get; set; }

        public PlayerTotals Get(PlayerSlot slot)
        {
            return slot == PlayerSlot.PlayerOne ? PlayerOne : PlayerTwo;
        }

        internal void Record(LevelSummary summary, string whenUtc)
        {
            Attempts++;
            switch (summary.Reason)
            {
                case LevelEndReason.Won:
                    Wins++;
                    break;
                case LevelEndReason.Lost:
                    Losses++;
                    break;
                case LevelEndReason.Restarted:
                    Restarts++;
                    break;
                default:
                    Exits++;
                    break;
            }

            if (FirstPlayedUtc.Length == 0)
            {
                FirstPlayedUtc = whenUtc;
            }

            LastPlayedUtc = whenUtc;
            TotalPlaySeconds += summary.DurationSeconds;
            PlayerOne.Add(summary.PlayerOne);
            PlayerTwo.Add(summary.PlayerTwo);

            AttemptRecord attempt = AttemptRecord.From(summary, whenUtc);
            LastAttempt = attempt;
            if (summary.Reason == LevelEndReason.Won)
            {
                LastWin = attempt;
                if (FirstWinUtc.Length == 0)
                {
                    FirstWinUtc = whenUtc;
                }

                if (summary.DurationSeconds > 0f && (BestWinSeconds <= 0f || summary.DurationSeconds < BestWinSeconds))
                {
                    BestWinSeconds = summary.DurationSeconds;
                }
            }
        }

        internal Dictionary<string, object> ToJson()
        {
            var map = new Dictionary<string, object>
            {
                { "nivel", LevelKey },
                { "intentos", Attempts },
                { "victorias", Wins },
                { "derrotas", Losses },
                { "reinicios", Restarts },
                { "salidas", Exits },
                { "mejorVictoria", BestWinSeconds },
                { "tiempoJugado", TotalPlaySeconds },
                { "primeraVez", FirstPlayedUtc },
                { "ultimaVez", LastPlayedUtc },
                { "primeraVictoria", FirstWinUtc },
                { "p1", PlayerOne.ToJson() },
                { "p2", PlayerTwo.ToJson() },
            };
            if (LastWin != null)
            {
                map["ultimaVictoria"] = LastWin.ToJson();
            }

            if (LastAttempt != null)
            {
                map["ultimoIntento"] = LastAttempt.ToJson();
            }

            return map;
        }

        internal static BossRecord FromJson(object value)
        {
            var map = value as Dictionary<string, object>;
            if (map == null)
            {
                return null;
            }

            string key = JsonRead.String(map, "nivel");
            if (key.Length == 0)
            {
                return null;
            }

            var record = new BossRecord(key)
            {
                Attempts = JsonRead.Int(map, "intentos"),
                Wins = JsonRead.Int(map, "victorias"),
                Losses = JsonRead.Int(map, "derrotas"),
                Restarts = JsonRead.Int(map, "reinicios"),
                Exits = JsonRead.Int(map, "salidas"),
                BestWinSeconds = (float)JsonRead.Double(map, "mejorVictoria"),
                TotalPlaySeconds = (float)JsonRead.Double(map, "tiempoJugado"),
                FirstPlayedUtc = JsonRead.String(map, "primeraVez"),
                LastPlayedUtc = JsonRead.String(map, "ultimaVez"),
                FirstWinUtc = JsonRead.String(map, "primeraVictoria"),
            };
            object p1;
            object p2;
            object lastWin;
            object lastAttempt;
            map.TryGetValue("p1", out p1);
            map.TryGetValue("p2", out p2);
            record.PlayerOne.Add(PlayerTotals.FromJson(p1));
            record.PlayerTwo.Add(PlayerTotals.FromJson(p2));
            if (map.TryGetValue("ultimaVictoria", out lastWin))
            {
                record.LastWin = AttemptRecord.FromJson(lastWin);
            }

            if (map.TryGetValue("ultimoIntento", out lastAttempt))
            {
                record.LastAttempt = AttemptRecord.FromJson(lastAttempt);
            }

            return record;
        }
    }

    /// <summary>
    /// «Libro de recuerdos»: estadísticas acumuladas por nivel, que se guardan entre partidas.
    /// </summary>
    public sealed class StatsBook
    {
        public const int FormatVersion = 1;

        private readonly Dictionary<string, BossRecord> records = new Dictionary<string, BossRecord>();

        public int Count
        {
            get { return records.Count; }
        }

        /// <summary>
        /// Añade un intento terminado. Devuelve el registro actualizado, o null si el intento se descarta por no
        /// haber pasado nada (entrar y salir sin jugar, como en la casa del principio).
        /// </summary>
        public BossRecord Record(LevelSummary summary, DateTime utcNow)
        {
            if (summary == null || string.IsNullOrEmpty(summary.LevelKey))
            {
                return null;
            }

            bool nothingHappened = PlayerTotals.From(summary.PlayerOne).IsEmpty && PlayerTotals.From(summary.PlayerTwo).IsEmpty;
            if (nothingHappened && summary.Reason != LevelEndReason.Won && summary.Reason != LevelEndReason.Lost)
            {
                return null;
            }

            BossRecord record;
            if (!records.TryGetValue(summary.LevelKey, out record))
            {
                record = new BossRecord(summary.LevelKey);
                records[summary.LevelKey] = record;
            }

            record.Record(summary, utcNow.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
            return record;
        }

        public BossRecord Get(string levelKey)
        {
            BossRecord record;
            return levelKey != null && records.TryGetValue(levelKey, out record) ? record : null;
        }

        /// <summary>Registros ordenados del jugado más recientemente al más antiguo.</summary>
        public IList<BossRecord> Records()
        {
            var list = new List<BossRecord>(records.Values);
            list.Sort((a, b) => string.CompareOrdinal(b.LastPlayedUtc, a.LastPlayedUtc));
            return list;
        }

        public PlayerTotals GrandTotal(PlayerSlot slot)
        {
            var total = new PlayerTotals();
            foreach (BossRecord record in records.Values)
            {
                total.Add(record.Get(slot));
            }

            return total;
        }

        public int TotalAttempts
        {
            get
            {
                int sum = 0;
                foreach (BossRecord record in records.Values)
                {
                    sum += record.Attempts;
                }

                return sum;
            }
        }

        public int TotalWins
        {
            get
            {
                int sum = 0;
                foreach (BossRecord record in records.Values)
                {
                    sum += record.Wins;
                }

                return sum;
            }
        }

        public float TotalPlaySeconds
        {
            get
            {
                float sum = 0f;
                foreach (BossRecord record in records.Values)
                {
                    sum += record.TotalPlaySeconds;
                }

                return sum;
            }
        }

        public string ToJson()
        {
            var list = new List<object>();
            foreach (BossRecord record in Records())
            {
                list.Add(record.ToJson());
            }

            return MiniJson.Serialize(new Dictionary<string, object>
            {
                { "version", FormatVersion },
                { "niveles", list },
            });
        }

        /// <summary>Carga un libro. Lanza <see cref="FormatException"/> si el texto no es un libro válido.</summary>
        public static StatsBook FromJson(string json)
        {
            var root = MiniJson.Parse(json) as Dictionary<string, object>;
            if (root == null)
            {
                throw new FormatException("El archivo de estadísticas no es un objeto JSON.");
            }

            var book = new StatsBook();
            object levels;
            var list = root.TryGetValue("niveles", out levels) ? levels as List<object> : null;
            if (list == null)
            {
                return book;
            }

            foreach (object item in list)
            {
                BossRecord record = BossRecord.FromJson(item);
                if (record != null)
                {
                    book.records[record.LevelKey] = record;
                }
            }

            return book;
        }
    }

    internal static class JsonRead
    {
        public static int Int(Dictionary<string, object> map, string key)
        {
            return (int)Double(map, key);
        }

        public static double Double(Dictionary<string, object> map, string key)
        {
            object value;
            return map.TryGetValue(key, out value) ? ToDouble(value) : 0d;
        }

        public static double ToDouble(object value)
        {
            if (value is double)
            {
                return (double)value;
            }

            if (value is int)
            {
                return (int)value;
            }

            return 0d;
        }

        public static string String(Dictionary<string, object> map, string key)
        {
            object value;
            return map.TryGetValue(key, out value) && value is string ? (string)value : string.Empty;
        }

        public static LevelEndReason EndReason(Dictionary<string, object> map, string key)
        {
            switch (String(map, key))
            {
                case "Won":
                    return LevelEndReason.Won;
                case "Lost":
                    return LevelEndReason.Lost;
                case "Restarted":
                    return LevelEndReason.Restarted;
                case "Exited":
                    return LevelEndReason.Exited;
                default:
                    return LevelEndReason.Unknown;
            }
        }
    }
}
