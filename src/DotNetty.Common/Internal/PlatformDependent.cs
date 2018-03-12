﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#define USE_SPAN2
// ReSharper disable ConvertToAutoPropertyWhenPossible
namespace DotNetty.Common.Internal
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reflection;
    using System.Runtime.CompilerServices;
#if USE_SPAN2
    using System.Runtime.InteropServices;
#endif
    using System.Threading;
    using DotNetty.Common.Internal.Logging;

    public static class PlatformDependent
    {
        static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance(typeof(PlatformDependent));

        static readonly bool UseDirectBuffer;
        static readonly bool SpanSupportInNative;

        static PlatformDependent()
        {
            UseDirectBuffer = !SystemPropertyUtil.GetBoolean("io.netty.noPreferDirect", false);
            if (Logger.DebugEnabled)
            {
                Logger.Debug("-Dio.netty.noPreferDirect: {}", !UseDirectBuffer);
            }

            try
            {
                SpanSupportInNative = typeof(Span<>).GetTypeInfo().Assembly == typeof(string).GetTypeInfo().Assembly;
            }
            catch
            {
                SpanSupportInNative = false;
            }

        }

        public static bool DirectBufferPreferred => UseDirectBuffer;

        static int seed = (int)(Stopwatch.GetTimestamp() & 0xFFFFFFFF); //used to safly cast long to int, because the timestamp returned is long and it doesn't fit into an int
        static readonly ThreadLocal<Random> ThreadLocalRandom = new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref seed))); //used to simulate java ThreadLocalRandom

        public static IQueue<T> NewFixedMpscQueue<T>(int capacity) where T : class => new MpscArrayQueue<T>(capacity);

        public static IQueue<T> NewMpscQueue<T>() where T : class => new CompatibleConcurrentQueue<T>();

        public static IDictionary<TKey, TValue> NewConcurrentHashMap<TKey, TValue>() => new ConcurrentDictionary<TKey, TValue>();

        public static ILinkedQueue<T> NewSpscLinkedQueue<T>() where T : class => new SpscLinkedQueue<T>();

        public static Random GetThreadLocalRandom() => ThreadLocalRandom.Value;

        public static unsafe bool ByteArrayEquals(byte[] bytes1, int startPos1, byte[] bytes2, int startPos2, int length)
        {
            fixed (byte* array1 = bytes1)
            fixed (byte* array2 = bytes2)
                return PlatformDependent0.ByteArrayEquals(array1, startPos1, array2, startPos2, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ByteArrayEqualsEx(byte[] bytes1, int startPos1, byte[] bytes2, int startPos2, int length)
        {
            return PlatformDependent0.ByteArrayEquals(bytes1, startPos1, bytes2, startPos2, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ByteArrayCompareToEx(byte[] bytes1, int startPos1, int len1, byte[] bytes2, int startPos2, int len2)
        {
            return PlatformDependent0.ByteArrayCompareTo(bytes1, startPos1, len1, bytes2, startPos2, len2);
        }

        public static unsafe void CopyMemory(byte[] src, int srcIndex, byte[] dst, int dstIndex, int length)
        {
            if (length > 0)
            {
                Unsafe.CopyBlockUnaligned(ref dst[dstIndex], ref src[srcIndex], unchecked((uint)length));
            }
        }

        public static unsafe void CopyMemory(byte* src, byte* dst, int length)
        {
            if (length > 0)
            {
                Unsafe.CopyBlock(dst, src, unchecked((uint)length));
            }
        }

        public static unsafe void CopyMemory(byte* src, byte[] dst, int dstIndex, int length)
        {
            if (length > 0)
            {
                fixed (byte* destination = &dst[dstIndex])
                    Unsafe.CopyBlock(destination, src, unchecked((uint)length));
            }
        }

        public static unsafe void CopyMemory(byte[] src, int srcIndex, byte* dst, int length)
        {
            if (length > 0)
            {
                fixed (byte* source = &src[srcIndex])
                    Unsafe.CopyBlock(dst, source, unchecked((uint)length));
            }
        }

        public static unsafe void CopyMemory(ref byte src, ref byte dst, int length)
        {
            if (length > 0)
            {
                Unsafe.CopyBlockUnaligned(ref dst, ref src, unchecked((uint)length));
            }
        }

        public static unsafe void Clear(ref byte src, int length)
        {
            if (length > 0)
            {
                Unsafe.InitBlockUnaligned(ref src, default(byte), unchecked((uint)length));
            }
        }

        public static unsafe void Clear(byte[] src, int srcIndex, int length)
        {
            if (length > 0)
            {
                Unsafe.InitBlockUnaligned(ref src[srcIndex], default(byte), unchecked((uint)length));
            }
        }

        public static unsafe void SetMemory(byte* src, int length, byte value)
        {
            if (length > 0)
            {
                Unsafe.InitBlockUnaligned(src, value, unchecked((uint)length));
            }
        }

        public static unsafe void SetMemory(ref byte src, int length, byte value)
        {
            if (length > 0)
            {
                Unsafe.InitBlockUnaligned(ref src, value, unchecked((uint)length));
            }
        }

        public static unsafe void SetMemory(byte[] src, int srcIndex, int length, byte value)
        {
            if (length > 0)
            {
                Unsafe.InitBlockUnaligned(ref src[srcIndex], value, unchecked((uint)length));
            }
        }


        #region Test

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref byte Add(ref byte array, int index) => ref Unsafe.Add(ref array, index);//see Unsafe.AddByteOffset?

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref byte AsRef(this byte[] array, int index = 0) =>
#if USE_SPAN2
            ref Add(ref MemoryMarshal.GetReference(new ReadOnlySpan<byte>(array)), index);
#elif USE_SPAN
            ref MemoryMarshal.GetReference(new ReadOnlySpan<byte>(array, index, 1));
#else
            //better for !SpanSupportInNative
            ref array[index];
#endif
#endregion
    }
}