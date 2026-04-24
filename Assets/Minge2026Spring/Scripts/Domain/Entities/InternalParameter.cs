using System;
using Minge2026Spring.Scripts.Domain.ValueObjects;
using UnityEngine;

namespace Minge2026Spring.Scripts.Domain.Entities
{
    [Serializable]
    public class InternalParameter
    {
        private string _characterId;
        private int _morale;

        public int Morale
        {
            get => Mathf.Clamp(_morale, MoraleRange.MIN_VALUE, MoraleRange.MAX_VALUE);
            set => _morale = Mathf.Clamp(value, MoraleRange.MIN_VALUE, MoraleRange.MAX_VALUE);
        }
    }
}