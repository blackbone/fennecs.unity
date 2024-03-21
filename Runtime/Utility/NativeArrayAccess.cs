using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace fennecs.Utility
{
    public readonly struct NativeArrayAccess : IDisposable
    {
        private readonly ulong gcHandle;

        internal NativeArrayAccess(ulong gcHandle) => this.gcHandle = gcHandle;
        public void Dispose() => UnsafeUtility.ReleaseGCObject(gcHandle);
    }

    public static class ArrayExtensions
    {
        public static NativeArrayAccess GetNativeArrayAccess<T>(this T[] array, out NativeArray<T> nativeArray) where T : struct
        {
            unsafe
            {
                var ptr = UnsafeUtility.PinGCArrayAndGetDataAddress(array, out var gcHandle);
                nativeArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(ptr, array.Length, Allocator.None);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref nativeArray, AtomicSafetyHandle.GetTempMemoryHandle());
#endif
                return new NativeArrayAccess(gcHandle);
            }
        }
    }
}