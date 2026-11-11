using System;
using AlienFarms.Core;
using Unity.Collections;
using Unity.Netcode;

namespace AlienFarms.Networking.Session
{
    /// <summary>
    /// What a client sends in <see cref="NetworkConfig.ConnectionData"/> when it connects. Written by the client, read by
    /// the server's approval (<see cref="ApprovalRules"/>): <see cref="UgsPlayerId"/> identifies the player across
    /// reconnections (empty for a direct dev connection without Unity Gaming Services). The character is picked inside
    /// the online room (<see cref="NetworkLobbyState"/>), so it is not part of the payload (removed in protocol 2).
    /// </summary>
    public struct ConnectionPayload : INetworkSerializable
    {
        /// <summary>
        /// Largest valid payload in bytes: the version (int) plus each fixed string as a uint length prefix and its UTF-8
        /// bytes at capacity, 4 + (4 + 29) + (4 + 61) = 102. <see cref="TryParse"/> refuses anything longer unread.
        /// Update it with the layout (<see cref="NetworkSerialize{T}"/>).
        /// </summary>
        public const int MaxSerializedSize = sizeof(int)
            + sizeof(uint) + FixedString32BytesMaxBytes
            + sizeof(uint) + FixedString64BytesMaxBytes;

        // FixedString32Bytes.UTF8MaxLengthInBytes / FixedString64Bytes.UTF8MaxLengthInBytes, as constants (checked by tests).
        private const int FixedString32BytesMaxBytes = 29;
        private const int FixedString64BytesMaxBytes = 61;

        public int ProtocolVersion;
        public FixedString32Bytes DisplayName;
        public FixedString64Bytes UgsPlayerId;

        /// <summary>
        /// Builds a payload; the name and the UGS id are truncated to what their fixed strings hold (29 / 61 UTF-8 bytes).
        /// </summary>
        public static ConnectionPayload Create(int protocolVersion, string displayName, string ugsPlayerId = null)
        {
            var payload = new ConnectionPayload { ProtocolVersion = protocolVersion };

            if (!string.IsNullOrEmpty(displayName))
            {
                payload.DisplayName.CopyFromTruncated(displayName);
            }

            if (!string.IsNullOrEmpty(ugsPlayerId))
            {
                payload.UgsPlayerId.CopyFromTruncated(ugsPlayerId);
            }

            return payload;
        }

        /// <summary>The client identified itself with a Unity Gaming Services player id.</summary>
        public bool HasUgsPlayerId => UgsPlayerId.Length > 0;

        /// <summary>
        /// This payload with its <see cref="DisplayName"/> cleaned by <see cref="DisplayNameSanitizer"/> (the server
        /// applies it to every accepted client: the name is shown to the other players). Allocates only when it changes.
        /// </summary>
        public ConnectionPayload WithSanitizedName()
        {
            if (DisplayName.Length == 0)
            {
                return this;
            }

            string name = DisplayName.ToString();
            string clean = DisplayNameSanitizer.Sanitize(name);
            if (ReferenceEquals(clean, name))
            {
                return this;
            }

            ConnectionPayload result = this;
            result.DisplayName = default;
            if (clean.Length > 0)
            {
                result.DisplayName.CopyFromTruncated(clean);
            }

            return result;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ProtocolVersion);
            serializer.SerializeValue(ref DisplayName);
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
        /// Reads a payload written by <see cref="ToBytes"/>. False (never throws) for null, empty, oversized
        /// (&gt; <see cref="MaxSerializedSize"/>, refused before reading), truncated or malformed data, or data with
        /// trailing bytes: every field is bounds-checked and each string's length prefix is validated
        /// against its capacity before reading it. On failure <paramref name="payload"/> is default except for
        /// <see cref="ProtocolVersion"/> when its 4 bytes could be read, so a client of another protocol (whose layout
        /// differs) can still be told <see cref="DisconnectReasons.ProtocolMismatch"/>.
        /// </summary>
        public static bool TryParse(byte[] data, out ConnectionPayload payload)
        {
            payload = default;
            if (data == null || data.Length == 0)
            {
                return false;
            }

            // Oversized data (hostile or another layout): only the version is kept, nothing else is read.
            if (data.Length > MaxSerializedSize)
            {
                if (data.Length >= sizeof(int))
                {
                    payload.ProtocolVersion = BitConverter.ToInt32(data, 0);
                }

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
                    payload.ProtocolVersion = result.ProtocolVersion;

                    // Trailing bytes mean another layout (e.g. an older protocol): reject instead of misreading it.
                    if (!TryReadFixedString(reader, FixedString32Bytes.UTF8MaxLengthInBytes, out result.DisplayName)
                        || !TryReadFixedString(reader, FixedString64Bytes.UTF8MaxLengthInBytes, out result.UgsPlayerId)
                        || reader.Position != reader.Length)
                    {
                        return false;
                    }

                    payload = result;
                    return true;
                }
            }
            catch (Exception)
            {
                // Untrusted bytes: any failure is just a rejected payload (keeping the version if it was read).
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
