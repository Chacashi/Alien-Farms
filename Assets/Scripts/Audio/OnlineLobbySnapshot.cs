using AlienFarms.Core;
using AlienFarms.Data;

namespace AlienFarms.Audio
{
    public readonly struct OnlineLobbySnapshot
    {
        public OnlineLobbySnapshot(OnlineLobbyPhase phase, bool isLocalSpectator, int seatCount,
            OnlineLobbySeat seat0, OnlineLobbySeat seat1)
        {
            Phase = phase;
            IsLocalSpectator = isLocalSpectator;
            SeatCount = seatCount;
            Seat0 = seat0;
            Seat1 = seat1;
        }

        public OnlineLobbyPhase Phase { get; }
        public bool IsLocalSpectator { get; }
        public int SeatCount { get; }
        public OnlineLobbySeat Seat0 { get; }
        public OnlineLobbySeat Seat1 { get; }

        public bool HasLocalSeat => (SeatCount > 0 && Seat0.IsLocal) || (SeatCount > 1 && Seat1.IsLocal);
        public bool IsInRoom => Phase == OnlineLobbyPhase.Open && (HasLocalSeat || IsLocalSpectator);

        public static OnlineLobbySnapshot Capture(OnlineLobbyChannel channel)
        {
            int count = channel.SeatCount;
            return new OnlineLobbySnapshot(channel.Phase, channel.IsLocalSpectator, count,
                count > 0 ? channel.GetSeat(0) : default,
                count > 1 ? channel.GetSeat(1) : default);
        }

        public bool TryGetSeat(PlayerId id, out OnlineLobbySeat seat)
        {
            if (id != PlayerId.None && SeatCount > 0 && Seat0.Id == id)
            {
                seat = Seat0;
                return true;
            }

            if (id != PlayerId.None && SeatCount > 1 && Seat1.Id == id)
            {
                seat = Seat1;
                return true;
            }

            seat = default;
            return false;
        }
    }
}
