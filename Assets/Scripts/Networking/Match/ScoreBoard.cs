using System;
using AlienFarms.Core;
using Unity.Netcode;

namespace AlienFarms.Networking.Match
{
    /// <summary>Both players' published scores. The match result is derived from it (no result message).</summary>
    public struct ScoreBoard : INetworkSerializable, IEquatable<ScoreBoard>
    {
        private int _player1;
        private int _player2;

        public ScoreBoard(int player1, int player2)
        {
            _player1 = player1;
            _player2 = player2;
        }

        public int Player1 => _player1;

        public int Player2 => _player2;

        public int Get(PlayerId id)
        {
            switch (id)
            {
                case PlayerId.Player1:
                    return _player1;
                case PlayerId.Player2:
                    return _player2;
                default:
                    return 0;
            }
        }

        /// <summary>Copy with <paramref name="id"/>'s score replaced (unchanged for <see cref="PlayerId.None"/>).</summary>
        public ScoreBoard With(PlayerId id, int score)
        {
            switch (id)
            {
                case PlayerId.Player1:
                    return new ScoreBoard(score, _player2);
                case PlayerId.Player2:
                    return new ScoreBoard(_player1, score);
                default:
                    return this;
            }
        }

        /// <summary>Same rule as the match's own evaluation: highest score wins, any tie is a draw.</summary>
        public MatchResult ToResult()
        {
            PlayerId winner = _player1 > _player2 ? PlayerId.Player1 : _player2 > _player1 ? PlayerId.Player2 : PlayerId.None;
            return new MatchResult(winner, _player1, _player2);
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _player1);
            serializer.SerializeValue(ref _player2);
        }

        public bool Equals(ScoreBoard other) => _player1 == other._player1 && _player2 == other._player2;

        public override bool Equals(object obj) => obj is ScoreBoard other && Equals(other);

        public override int GetHashCode() => (_player1 * 397) ^ _player2;
    }
}
