using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace fennecs
{
    internal static class DelegatesHelper
    {
        private static readonly Dictionary<IntPtr, int> knownDelegates = new();
        private static readonly Delegate[] delegates = new Delegate[4096];
        private static int count = 0;
        public static IntPtr AddAction(Delegate action)
        {
            GCHandle.Alloc(action);
            RuntimeHelpers.PrepareDelegate(action);
            var ptr = action.Method.MethodHandle.GetFunctionPointer();
            if (knownDelegates.TryAdd(ptr, count))
                delegates[count++] = action;

            return ptr;
        }

        public static ref T Get<T>(IntPtr ptr)
        {
            if (!knownDelegates.TryGetValue(ptr, out var index))
                return ref Unsafe.NullRef<T>();

            return ref Unsafe.As<Delegate, T>(ref delegates[index]);
        }
    }
}