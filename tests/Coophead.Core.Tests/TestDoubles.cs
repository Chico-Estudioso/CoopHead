using System;
using System.Collections.Generic;
using Coophead.Core;
using Coophead.Core.Diagnostics;
using Coophead.Core.Roulette;

namespace Coophead.Core.Tests
{
    internal sealed class FakeApplier : IPenaltyApplier
    {
        public readonly List<string> Calls = new List<string>();
        public readonly HashSet<string> Active = new HashSet<string>();
        public PenaltyEffect? ThrowOnApply;
        public PenaltyEffect? ThrowOnRestore;

        public void Apply(PlayerSlot player, PenaltyEffect effect)
        {
            Calls.Add("apply " + player.Label() + " " + effect);
            if (ThrowOnApply == effect)
            {
                throw new InvalidOperationException("fallo simulado");
            }

            Active.Add(player.Label() + " " + effect);
        }

        public void Restore(PlayerSlot player, PenaltyEffect effect)
        {
            Calls.Add("restore " + player.Label() + " " + effect);
            Active.Remove(player.Label() + " " + effect);
            if (ThrowOnRestore == effect)
            {
                throw new InvalidOperationException("fallo simulado");
            }
        }
    }

    /// <summary>Devuelve una secuencia fija y después repite el último valor.</summary>
    internal sealed class SequenceRandom : IRandomSource
    {
        private readonly double[] values;
        private int index;

        public SequenceRandom(params double[] values)
        {
            this.values = values;
        }

        public double NextDouble()
        {
            double value = values[Math.Min(index, values.Length - 1)];
            index++;
            return value;
        }
    }

    internal sealed class ListLog : ICoreLog
    {
        public readonly List<string> Entries = new List<string>();

        public void Info(string message)
        {
            Entries.Add("INFO " + message);
        }

        public void Warning(string message)
        {
            Entries.Add("WARN " + message);
        }

        public void Error(string message)
        {
            Entries.Add("ERROR " + message);
        }
    }
}
