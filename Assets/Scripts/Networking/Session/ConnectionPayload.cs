using System;
using Unity.Collections;
using Unity.Netcode;

namespace AlienFarms.Networking.Session
{
    /// <summary>
    /// What a client sends in <see cref="NetworkConfig.ConnectionData"/> when it connects. Written by the client, read by
    /// the server's approval. <see cref="UgsPlayerId"/> is reserved for reconnection (week 2) and not evaluated yet.
    /// </summary>
    public struct ConnectionPayload : INetworkSerializable
    {
        /// <summary><see cref="CharacterIndex"/> value meaning "no character picked".</summary>
        public const byte NoCharacter = byte.MaxValue;

        // Fixed fields: ~140 bytes at most. Generous so a future field never overflows silently.
        private const int MaxSerializedSize = 256;

        public int ProtocolVersion;
        public FixedString32Bytes DisplayName;
        public byte CharacterIndex;
        public FixedString64Bytes UgsPlayerId;

        /// <summary>Builds a payload; the name is truncated to what <see cref="FixedString32Bytes"/> holds (29 UTF-8 bytes).</summary>
        public static ConnectionPayload Create(int protocolVersion, string displayName, int characterIndex)
        {
            var payload = new ConnectionPayload
            {
                ProtocolVersion = protocolVersion,
                CharacterIndex = characterIndex >= 0 && characterIndex < NoCharacter ? (byte)characterIndex : NoCharacter
            };

            if (!string.IsNullOrEmpty(displayName))
            {
                payload.DisplayName.CopyFromTruncated(displayName);
            }

            return payload;
        }

        public bool HasCharacter => CharacterIndex != NoCharacter;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ProtocolVersion);
            serializer.SerializeValue(ref DisplayName);
            serializer.SerializeValue(ref CharacterIndex);
            serializer.SerializeValue(ref UgsPlayerId);
        }

        /// <summary>Serializes this payload for <see cref="NetworkConfig.ConnectionData"/>.</summary>
        public byte[] ToBytes()
        {
            using (var writer = new FastBufferWriter(MaxSerializedSize, Allocator.Temp))
            {
                writer.WriteNetworkSerializable(this);
                return writer.ToArray();
            }
        }

        /// <summary>
        /// Reads a payload written by <see cref="ToBytes"/>. False (never throws) for null, empty, truncated or malformed
        /// data: every field is bounds-checked and each string's length prefix is validated against its capacity
        /// before reading it.
        /// </summary>
        public static bool TryParse(byte[] data, out ConnectionPayload payload)
        {
            payload = default;
            if (data == null || data.Length == 0)
            {
                return false;
            }

            try
            {
                using (var reader = new FastBufferReader(data, Allocator.Temp))
                {
                    var result = new ConnectionPayload();
                    if (!reader.TryBeginRead(sizeof(int)))
                    {
                        return false;
                    }

                    reader.ReadValue(out result.ProtocolVersion);

                    if (!TryReadFixedString(reader, FixedString32Bytes.UTF8MaxLengthInBytes, out result.DisplayName))
                    {
                        return false;
                    }

                    if (!reader.TryBeginRead(sizeof(byte)))
                    {
                        return false;
                    }

                    reader.ReadValue(out result.CharacterIndex);

                    if (!TryReadFixedString(reader, FixedString64Bytes.UTF8MaxLengthInBytes, out result.UgsPlayerId))
                    {
                        return false;
                    }

                    payload = result;
                    return true;
                }
            }
            catch (Exception)
            {
                // Untrusted bytes: any failure is just a rejected payload.
                payload = default;
                return false;
            }
        }

        // FixedString wire format (NGO): uint byte length, then the UTF-8 bytes.
        private static bool TryReadFixedString<T>(FastBufferReader reader, int maxBytes, out T value)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            value = default;
            int start = reader.Position;
            if (!reader.TryBeginRead(sizeof(uint)))
            {
                return false;
            }

            reader.ReadValue(out uint length);
            if (length > (uint)maxBytes || !reader.TryBeginRead((int)length))
            {
                return false;
            }

            reader.Seek(start);
            reader.ReadValueSafe(out value);
            return true;
        }
    }
}
