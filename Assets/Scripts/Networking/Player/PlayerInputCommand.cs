using Unity.Netcode;
using UnityEngine;

namespace AlienFarms.Networking.Player
{
    /// <summary>
    /// One physics step of a player's input, quantized for the wire (11 bytes): sequence, move (sbyte per axis), camera
    /// view (yaw as ushort over 360 deg, pitch in centidegrees) and the held buttons and the pause flag as bits.
    /// </summary>
    public struct PlayerInputCommand : INetworkSerializable
    {
        public const byte SprintBit = 1 << 0;
        public const byte GrabBit = 1 << 1;
        public const byte AimBit = 1 << 2;

        /// <summary>The owner is paused (menu open): the server keeps what it carries even though Grab reads released.</summary>
        public const byte PausedBit = 1 << 3;

        private const float MoveScale = 127f;
        private const float YawSteps = 65536f;
        private const float PitchScale = 100f;
        private const float MaxPitch = 90f;

        /// <summary>Owner-side sequence, starting at 1 (0 = no command).</summary>
        public uint Seq;
        public sbyte MoveX;
        public sbyte MoveY;
        public ushort YawQuantized;
        public short PitchCentidegrees;
        public byte Buttons;

        public static PlayerInputCommand Create(uint seq, Vector2 move, float yaw, float pitch, bool sprint, bool grab, bool aim, bool paused = false)
        {
            move = Vector2.ClampMagnitude(move, 1f);
            float normalizedYaw = Mathf.Repeat(yaw, 360f) / 360f;
            int yawSteps = Mathf.RoundToInt(normalizedYaw * YawSteps) & 0xFFFF;

            byte buttons = 0;
            if (sprint)
            {
                buttons |= SprintBit;
            }

            if (grab)
            {
                buttons |= GrabBit;
            }

            if (paused)
            {
                buttons |= PausedBit;
            }

            if (aim)
            {
                buttons |= AimBit;
            }

            return new PlayerInputCommand
            {
                Seq = seq,
                MoveX = QuantizeAxis(move.x),
                MoveY = QuantizeAxis(move.y),
                YawQuantized = (ushort)yawSteps,
                PitchCentidegrees = (short)Mathf.RoundToInt(Mathf.Clamp(pitch, -MaxPitch, MaxPitch) * PitchScale),
                Buttons = buttons
            };
        }

        public Vector2 Move => new Vector2(MoveX / MoveScale, MoveY / MoveScale);

        /// <summary>Camera yaw (deg, 0..360).</summary>
        public float Yaw => YawQuantized * (360f / YawSteps);

        /// <summary>Camera pitch (deg).</summary>
        public float Pitch => PitchCentidegrees / PitchScale;

        public bool IsSprintHeld => (Buttons & SprintBit) != 0;

        public bool IsGrabHeld => (Buttons & GrabBit) != 0;

        public bool IsAimHeld => (Buttons & AimBit) != 0;

        public bool IsPaused => (Buttons & PausedBit) != 0;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Seq);
            serializer.SerializeValue(ref MoveX);
            serializer.SerializeValue(ref MoveY);
            serializer.SerializeValue(ref YawQuantized);
            serializer.SerializeValue(ref PitchCentidegrees);
            serializer.SerializeValue(ref Buttons);
        }

        private static sbyte QuantizeAxis(float value)
        {
            return (sbyte)Mathf.RoundToInt(Mathf.Clamp(value, -1f, 1f) * MoveScale);
        }
    }
}
