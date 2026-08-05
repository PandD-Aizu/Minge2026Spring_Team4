using Minge2026Spring.Scripts.Application.DTOs;

namespace Minge2026Spring.Scripts.Application.Interface
{
    public interface ITmpMoraleJsonExporter
    {
        void Export(MoraleDto moraleDto);
    }

    public interface IHiddenItemStatusRepository
    {
        HiddenItemStatus LoadHiddenItemStatus();
    }

    public readonly struct HiddenItemStatus
    {
        public bool GetItem1 { get; }
        public bool GetItem2 { get; }

        public HiddenItemStatus(bool getItem1, bool getItem2)
        {
            GetItem1 = getItem1;
            GetItem2 = getItem2;
        }
    }
}

