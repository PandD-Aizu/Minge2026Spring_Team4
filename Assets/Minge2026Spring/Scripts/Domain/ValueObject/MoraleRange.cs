using System;

namespace Minge2026Spring.Scripts.Domain.ValueObject
{
    public readonly struct MoraleRange
    {
        public const int MIN_VALUE = 0;
        public const int MAX_VALUE = 100;
        
        public int Min { get; }
        public int Max { get; }
        public MoraleLabel Label { get; }

        private MoraleRange(int min, int max, MoraleLabel label)
        {
            if (min > max)
                throw new ArgumentException($"Min value {min} cannot be greater than Max value {max}.");
            
            Min = min;
            Max = max;
            Label = label;
        }

        public static readonly MoraleRange Low = new (0, 30, MoraleLabel.MORALE_LOW);
        public static readonly MoraleRange Mid = new (31, 70, MoraleLabel.MORALE_MEDIUM);
        public static readonly MoraleRange High = new (71, 100, MoraleLabel.MORALE_HIGH);
        
        public bool Contains(int morale) => morale >= Min && morale <= Max;

        public static MoraleLabel GetLabel(int morale)
        {
            if (Low.Contains(morale)) return MoraleLabel.MORALE_LOW;
            if (Mid.Contains(morale)) return MoraleLabel.MORALE_MEDIUM;
            if (High.Contains(morale)) return MoraleLabel.MORALE_HIGH;
            throw new ArgumentException($"Morale value {morale} is out of range (0-100).");
        }
        
        public override bool Equals(object obj) => obj is MoraleRange other && Min == other.Min && Max == other.Max;
        public override int GetHashCode() => HashCode.Combine(Min, Max);
        public override string ToString() => $"{Label}({Min}-{Max})";
    }
}