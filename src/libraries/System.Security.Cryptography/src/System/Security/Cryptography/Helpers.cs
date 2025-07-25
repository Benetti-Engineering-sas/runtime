// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography
{
    internal static partial class Helpers
    {
        internal static readonly PbeParameters Windows3desPbe =
            new PbeParameters(PbeEncryptionAlgorithm.TripleDes3KeyPkcs12, HashAlgorithmName.SHA1, 2000);

        internal static readonly PbeParameters WindowsAesPbe =
            new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 2000);

        internal static void AddRange<T>(this ICollection<T> coll, IEnumerable<T> newData)
        {
            foreach (T datum in newData)
            {
                coll.Add(datum);
            }
        }

        public static bool UsesIv(this CipherMode cipherMode)
        {
            return cipherMode != CipherMode.ECB;
        }

        public static byte[]? GetCipherIv(this CipherMode cipherMode, byte[]? iv)
        {
            if (cipherMode.UsesIv())
            {
                if (iv == null)
                {
                    throw new CryptographicException(SR.Cryptography_MissingIV);
                }

                return iv;
            }

            return null;
        }

        public static byte[] FixupKeyParity(this byte[] key)
        {
            byte[] oddParityKey = new byte[key.Length];
            for (int index = 0; index < key.Length; index++)
            {
                // Get the bits we are interested in
                oddParityKey[index] = (byte)(key[index] & 0xfe);

                // Get the parity of the sum of the previous bits
                byte tmp1 = (byte)((oddParityKey[index] & 0xF) ^ (oddParityKey[index] >> 4));
                byte tmp2 = (byte)((tmp1 & 0x3) ^ (tmp1 >> 2));
                byte sumBitsMod2 = (byte)((tmp2 & 0x1) ^ (tmp2 >> 1));

                // We need to set the last bit in oddParityKey[index] to the negation
                // of the last bit in sumBitsMod2
                if (sumBitsMod2 == 0)
                    oddParityKey[index] |= 1;
            }
            return oddParityKey;
        }

        // Encode a byte array as an array of upper-case hex characters.
        internal static char[] ToHexArrayUpper(this byte[] bytes)
        {
            char[] chars = new char[bytes.Length * 2];
            HexConverter.EncodeToUtf16(bytes, chars);
            return chars;
        }

        // Encode a byte array as an upper case hex string.
        internal static string ToHexStringUpper(this byte[] bytes) =>
            Convert.ToHexString(bytes);

        // Decode a hex string-encoded byte array passed to various X509 crypto api.
        // The parsing rules are overly forgiving but for compat reasons, they cannot be tightened.
        internal static byte[] LaxDecodeHexString(this string hexString)
        {
            int whitespaceCount = 0;

            ReadOnlySpan<char> s = hexString;

            if (s.StartsWith('\u200E'))
            {
                s = s.Slice(1);
            }

            for (int i = 0; i < s.Length; i++)
            {
                if (char.IsWhiteSpace(s[i]))
                    whitespaceCount++;
            }

            uint cbHex = (uint)(s.Length - whitespaceCount) / 2;
            byte[] hex = new byte[cbHex];
            byte accum = 0;
            bool byteInProgress = false;
            int index = 0;

            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];

                if (char.IsWhiteSpace(c))
                {
                    continue;
                }

                accum <<= 4;
                accum |= (byte)HexConverter.FromChar(c);

                byteInProgress = !byteInProgress;

                // If we've flipped from 0 to 1, back to 0, we have a whole byte
                // so add it to the buffer.
                if (!byteInProgress)
                {
                    Debug.Assert(index < cbHex);

                    hex[index] = accum;
                    index++;
                }
            }

            // .NET Framework compat:
            // The .NET Framework algorithm removed all whitespace before the loop, then went up to length/2
            // of what was left.  This means that in the event of odd-length input the last char is
            // ignored, no exception should be raised.
            Debug.Assert(index == cbHex);

            return hex;
        }

        internal static bool ContentsEqual(this byte[]? a1, byte[]? a2)
        {
            if (a1 == null)
            {
                return a2 == null;
            }

            if (a2 == null || a1.Length != a2.Length)
            {
                return false;
            }

            return a1.AsSpan().SequenceEqual(a2);
        }

        internal static ReadOnlyMemory<byte> DecodeOctetStringAsMemory(ReadOnlyMemory<byte> encodedOctetString)
        {
            try
            {
                ReadOnlySpan<byte> input = encodedOctetString.Span;

                if (AsnDecoder.TryReadPrimitiveOctetString(
                        input,
                        AsnEncodingRules.BER,
                        out ReadOnlySpan<byte> primitive,
                        out int consumed))
                {
                    if (consumed != input.Length)
                    {
                        throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                    }

                    if (input.Overlaps(primitive, out int offset))
                    {
                        return encodedOctetString.Slice(offset, primitive.Length);
                    }

                    Debug.Fail("input.Overlaps(primitive) failed after TryReadPrimitiveOctetString succeeded");
                }

                byte[] ret = AsnDecoder.ReadOctetString(input, AsnEncodingRules.BER, out consumed);

                if (consumed != input.Length)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                return ret;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static bool AreSamePublicECParameters(ECParameters aParameters, ECParameters bParameters)
        {
            if (aParameters.Curve.CurveType != bParameters.Curve.CurveType)
                return false;

            if (!aParameters.Q.X!.ContentsEqual(bParameters.Q.X!) ||
                !aParameters.Q.Y!.ContentsEqual(bParameters.Q.Y!))
            {
                return false;
            }

            ECCurve aCurve = aParameters.Curve;
            ECCurve bCurve = bParameters.Curve;

            if (aCurve.IsNamed)
            {
                // On Windows we care about FriendlyName, on Unix we care about Value
                return aCurve.Oid.Value == bCurve.Oid.Value &&
                    string.Equals(aCurve.Oid.FriendlyName, bCurve.Oid.FriendlyName, StringComparison.OrdinalIgnoreCase);
            }

            if (!aCurve.IsExplicit)
            {
                // Implicit curve, always fail.
                return false;
            }

            // Ignore Cofactor (which is derivable from the prime or polynomial and Order)
            // Ignore Seed and Hash (which are entirely optional, and about how A and B were built)
            if (!aCurve.G.X!.ContentsEqual(bCurve.G.X!) ||
                !aCurve.G.Y!.ContentsEqual(bCurve.G.Y!) ||
                !aCurve.Order.ContentsEqual(bCurve.Order) ||
                !aCurve.A.ContentsEqual(bCurve.A) ||
                !aCurve.B.ContentsEqual(bCurve.B))
            {
                return false;
            }

            if (aCurve.IsPrime)
            {
                return aCurve.Prime.ContentsEqual(bCurve.Prime);
            }

            if (aCurve.IsCharacteristic2)
            {
                return aCurve.Polynomial.ContentsEqual(bCurve.Polynomial);
            }

            Debug.Fail($"Missing match criteria for curve type {aCurve.CurveType}");
            return false;
        }

        internal static bool IsValidDay(this Calendar calendar, int year, int month, int day, int era)
        {
            return (calendar.IsValidMonth(year, month, era) && day >= 1 && day <= calendar.GetDaysInMonth(year, month, era));
        }

        private static bool IsValidMonth(this Calendar calendar, int year, int month, int era)
        {
            return (calendar.IsValidYear(year) && month >= 1 && month <= calendar.GetMonthsInYear(year, era));
        }

        private static bool IsValidYear(this Calendar calendar, int year)
        {
            return (year >= calendar.GetYear(calendar.MinSupportedDateTime) && year <= calendar.GetYear(calendar.MaxSupportedDateTime));
        }

        internal static void DisposeAll(this IEnumerable<IDisposable> disposables)
        {
            foreach (IDisposable disposable in disposables)
            {
                disposable.Dispose();
            }
        }

        internal static void ValidateDer(ReadOnlySpan<byte> encodedValue)
        {
            try
            {
                Asn1Tag tag;
                AsnValueReader reader = new AsnValueReader(encodedValue, AsnEncodingRules.DER);

                while (reader.HasData)
                {
                    tag = reader.PeekTag();

                    // If the tag is in the UNIVERSAL class
                    //
                    // DER limits the constructed encoding to SEQUENCE and SET, as well as anything which gets
                    // a defined encoding as being an IMPLICIT SEQUENCE.
                    if (tag.TagClass == TagClass.Universal)
                    {
                        switch ((UniversalTagNumber)tag.TagValue)
                        {
                            case UniversalTagNumber.External:
                            case UniversalTagNumber.Embedded:
                            case UniversalTagNumber.Sequence:
                            case UniversalTagNumber.Set:
                            case UniversalTagNumber.UnrestrictedCharacterString:
                                if (!tag.IsConstructed)
                                {
                                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                                }

                                break;
                            default:
                                if (tag.IsConstructed)
                                {
                                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                                }

                                break;
                        }
                    }

                    if (tag.IsConstructed)
                    {
                        ValidateDer(reader.PeekContentBytes());
                    }

                    // Skip past the current value.
                    reader.ReadEncodedValue();
                }
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        public static int GetPaddingSize(this SymmetricAlgorithm algorithm, CipherMode mode, int feedbackSizeInBits)
        {
            return (mode == CipherMode.CFB ? feedbackSizeInBits : algorithm.BlockSize) / 8;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe ref readonly byte GetNonNullPinnableReference(ReadOnlySpan<byte> buffer)
        {
            // Based on the internal implementation from MemoryMarshal.
            return ref buffer.Length != 0 ? ref MemoryMarshal.GetReference(buffer) : ref Unsafe.AsRef<byte>((void*)1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe ref byte GetNonNullPinnableReference(Span<byte> buffer)
        {
            // Based on the internal implementation from MemoryMarshal.
            return ref buffer.Length != 0 ? ref MemoryMarshal.GetReference(buffer) : ref Unsafe.AsRef<byte>((void*)1);
        }

        internal static ReadOnlySpan<byte> ArrayToSpanOrThrow(
            byte[] arg,
            [CallerArgumentExpression(nameof(arg))] string? paramName = null)
        {
            ArgumentNullException.ThrowIfNull(arg, paramName);

            return arg;
        }

        internal static int HashLength(HashAlgorithmName hashAlgorithmName)
        {
            if (hashAlgorithmName == HashAlgorithmName.SHA1)
            {
                return HMACSHA1.HashSizeInBytes;
            }
            else if (hashAlgorithmName == HashAlgorithmName.SHA256)
            {
                return HMACSHA256.HashSizeInBytes;
            }
            else if (hashAlgorithmName == HashAlgorithmName.SHA384)
            {
                return HMACSHA384.HashSizeInBytes;
            }
            else if (hashAlgorithmName == HashAlgorithmName.SHA512)
            {
                return HMACSHA512.HashSizeInBytes;
            }
            else if (hashAlgorithmName == HashAlgorithmName.SHA3_256)
            {
                return HMACSHA3_256.HashSizeInBytes;
            }
            else if (hashAlgorithmName == HashAlgorithmName.SHA3_384)
            {
                return HMACSHA3_384.HashSizeInBytes;
            }
            else if (hashAlgorithmName == HashAlgorithmName.SHA3_512)
            {
                return HMACSHA3_512.HashSizeInBytes;
            }
            else if (hashAlgorithmName == HashAlgorithmName.MD5)
            {
                return HMACMD5.HashSizeInBytes;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(hashAlgorithmName));
            }
        }

        internal static PbeParameters MapExportParametersToPbeParameters(Pkcs12ExportPbeParameters exportParameters)
        {
            return exportParameters switch
            {
                Pkcs12ExportPbeParameters.Pkcs12TripleDesSha1 => Windows3desPbe,
                Pkcs12ExportPbeParameters.Default or Pkcs12ExportPbeParameters.Pbes2Aes256Sha256 => WindowsAesPbe,
                _ => throw new CryptographicException(),
            };
        }

        internal static void ThrowIfInvalidPkcs12ExportParameters(Pkcs12ExportPbeParameters exportParameters)
        {
            if (exportParameters is < Pkcs12ExportPbeParameters.Default or > Pkcs12ExportPbeParameters.Pbes2Aes256Sha256)
            {
                throw new ArgumentOutOfRangeException(nameof(exportParameters));
            }
        }

        internal static void ThrowIfInvalidPkcs12ExportParameters(PbeParameters exportParameters)
        {
            if (exportParameters.EncryptionAlgorithm is
                PbeEncryptionAlgorithm.Aes128Cbc or PbeEncryptionAlgorithm.Aes192Cbc or PbeEncryptionAlgorithm.Aes256Cbc)
            {
                switch (exportParameters.HashAlgorithm.Name)
                {
                    case HashAlgorithmNames.SHA1:
                    case HashAlgorithmNames.SHA256:
                    case HashAlgorithmNames.SHA384:
                    case HashAlgorithmNames.SHA512:
                        return;
                    case null or "":
                        throw new CryptographicException(SR.Cryptography_HashAlgorithmNameNullOrEmpty);
                    default:
                        // Let SHA-3 fall in to default since SHA-3 has not been brought up for PKCS12.
                        throw new CryptographicException(SR.Format(SR.Cryptography_UnknownAlgorithmIdentifier, exportParameters.HashAlgorithm.Name));
                }
            }
            else if (exportParameters.EncryptionAlgorithm is PbeEncryptionAlgorithm.TripleDes3KeyPkcs12)
            {
                switch (exportParameters.HashAlgorithm.Name)
                {
                    case HashAlgorithmNames.SHA1:
                        return;
                    case null or "":
                        throw new CryptographicException(SR.Cryptography_HashAlgorithmNameNullOrEmpty);
                    default:
                        throw new CryptographicException(SR.Format(SR.Cryptography_UnknownAlgorithmIdentifier, exportParameters.HashAlgorithm.Name));
                }
            }

            throw new CryptographicException(SR.Format(SR.Cryptography_UnknownAlgorithmIdentifier, exportParameters.EncryptionAlgorithm));
        }

        internal static void ThrowIfPasswordContainsNullCharacter(string? password)
        {
            if (password is not null && password.Contains('\0'))
            {
                throw new ArgumentException(SR.Argument_PasswordNullChars, nameof(password));
            }
        }

        internal static ReadOnlyMemory<byte>? ToNullableMemory(this byte[]? array)
        {
            if (array is null)
            {
                return default(ReadOnlyMemory<byte>?);
            }

            return array;
        }

        internal static bool IsSlhDsaOid(string? oid) =>
            SlhDsaAlgorithm.GetAlgorithmFromOid(oid) is not null;

        internal delegate TResult PreHashFuncCallback<TKey, TSignature, TResult>(
            TKey key,
            ReadOnlySpan<byte> encodedMessage,
            TSignature signatureBuffer)
            where TSignature : allows ref struct;

        /// <summary>
        /// Encodes the message for ML-DSA pre-hash signing.
        /// Algorithm is described in FIPS 205: Algorithm 23.
        /// </summary>
        internal static TResult MLDsaPreHash<TKey, TSignature, TResult>(
            ReadOnlySpan<byte> hash,
            ReadOnlySpan<byte> context,
            ReadOnlySpan<char> hashAlgorithmOid,
            TKey key,
            TSignature signatureBuffer,
            PreHashFuncCallback<TKey, TSignature, TResult> callback)
            where TSignature : allows ref struct
            => MLDsaSlhDsaPreHash(hash, context, hashAlgorithmOid, key, signatureBuffer, callback);

        /// <summary>
        /// Encodes the message for SLH-DSA pre-hash signing.
        /// Algorithm is described in FIPS 204: Algorithm 4.
        /// </summary>
        internal static TResult SlhDsaPreHash<TKey, TSignature, TResult>(
            ReadOnlySpan<byte> hash,
            ReadOnlySpan<byte> context,
            ReadOnlySpan<char> hashAlgorithmOid,
            TKey key,
            TSignature signatureBuffer,
            PreHashFuncCallback<TKey, TSignature, TResult> callback)
            where TSignature : allows ref struct
            => MLDsaSlhDsaPreHash(hash, context, hashAlgorithmOid, key, signatureBuffer, callback);

        /// <summary>
        /// Encodes the message for ML-DSA and SLH-DSA pre-hash signing.
        /// Algorithm is described in FIPS 204: Algorithm 4 and equivalent algorithm in FIPS 205: Algorithm 23.
        /// </summary>
        private static TResult MLDsaSlhDsaPreHash<TKey, TSignature, TResult>(
            ReadOnlySpan<byte> hash,
            ReadOnlySpan<byte> context,
            ReadOnlySpan<char> hashAlgorithmOid,
            TKey key,
            TSignature signatureBuffer,
            PreHashFuncCallback<TKey, TSignature, TResult> callback)
            where TSignature : allows ref struct
        {
            // The OIDs for the algorithms above have max length 11. We'll just round up for a conservative initial estimate.
            const int MaxEncodedOidLengthForCommonHashAlgorithms = 16;
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER, MaxEncodedOidLengthForCommonHashAlgorithms);
            writer.WriteObjectIdentifier(hashAlgorithmOid);

            int encodedOidLength = writer.GetEncodedLength();
            int messageLength = checked(
                1 +                 // Pre-hash encoding flag
                1 +                 // Context length
                context.Length +    // Context
                encodedOidLength +  // OID
                hash.Length);       // Hash

            // Common hash algorithms are at most 64 bytes, but unknown hash algorithms or long contexts
            // might overshoot the estimate.
            const int StackAllocThreshold = 128;
            byte[]? rented = null;
            Span<byte> message =
                messageLength > StackAllocThreshold
                    ? (rented = CryptoPool.Rent(messageLength))
                    : stackalloc byte[StackAllocThreshold];

            try
            {
                // Pre-hash encoding flag
                message[0] = 0x01;

                // Context length
                message[1] = checked((byte)context.Length);

                // Context
                context.CopyTo(message.Slice(2));

                // OID
                writer.Encode(
                    message.Slice(2 + context.Length),
                    (dest, encoded) => encoded.CopyTo(dest));

                // Hash
                hash.CopyTo(message.Slice(2 + context.Length + encodedOidLength));

                return callback(key, message.Slice(0, messageLength), signatureBuffer);
            }
            finally
            {
                if (rented != null)
                {
                    CryptoPool.Return(rented);
                }
            }
        }
    }
}
