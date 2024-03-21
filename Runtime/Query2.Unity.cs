// SPDX-License-Identifier: MIT

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using fennecs.Utility;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine.Pool;

namespace fennecs
{
    /// <summary>
    /// <para>
    /// Query with 1 output Stream Type, <c>C0</c>.
    /// </para>
    /// <para>
    /// Queries expose methods to rapidly iterate all Entities that match their Mask and Stream Types.
    /// </para>
    /// <ul>
    /// <li><c>ForEach(...)</c> - call a delegate <see cref="RefAction{C0}"/> for each Entity.</li>
    /// <li><c>Job(...)</c> - parallel process, calling a delegate <see cref="RefAction{C0}"/> for each Entity.</li>
    /// <li><c>Raw(...)</c> - pass Memory regions / Spans too a delegate <see cref="MemoryAction{C0}"/> per matched Archetype (× matched Wildcards) of entities.</li>
    /// </ul>
    /// </summary>
    /// <remarks>
    /// 
    /// </remarks>
    public static class Query2Extensions
    {
        public static void Cross<C0, C1>(this Query<C0, C1> query, in Action<C0[], C1[]> action)
        {
            query.AssertNotDisposed();
            using var worldLock = query.World.Lock;
            
            for (var i = 0; i < query.Archetypes.Count; i++)
            {
                var table = query.Archetypes[i];
                using var join = table.CrossJoin<C0, C1>(query.StreamTypes);
                if (join.Empty) continue;

                var (s0, s1) = join.Select;
                action(s0, s1);
            }
        }
        
        public static unsafe void JobFor<C0, C1>(this Query<C0, C1> query, RefAction<C0, C1> action, int chunkSize = 128)
            where C0 : struct
            where C1 : struct
        {
            query.AssertNotDisposed();
            using var worldLock = query.World.Lock;

            // fill unity jobs
            var jobCount = query.Archetypes.Count;
            var jobs = new NativeArray<UnityJob<C0, C1>>(jobCount, Allocator.Persistent);
            var jobHandles = new NativeArray<JobHandle>(jobCount, Allocator.Persistent);
            ListPool<NativeArrayAccess>.Get(out var nativeArrayAccesses);
            for (var i = 0; i < jobCount; i++)
            {
                var table = query.Archetypes[i];
                using var join = table.CrossJoin<C0, C1>(query.StreamTypes);
                if (join.Empty) continue;

                var (s0, s1) = join.Select;
                nativeArrayAccesses.Add(s0.GetNativeArrayAccess(out var memory1));
                nativeArrayAccesses.Add(s1.GetNativeArrayAccess(out var memory2));

                jobs[i] = new UnityJob<C0, C1>
                {
                    count = table.Count, // storage.Length is the capacity, not the count.
                    Memory1 = memory1,
                    Memory2 = memory2,
                    Action = Unsafe.AsPointer(ref action)
                };
            }

            // schedule unity jobs
            for (var i = 0; i < jobCount; i++)
            {
                ref var job = ref UnsafeUtility.ArrayElementAsRef<UnityJob<C0, C1>>(jobs.GetUnsafePtr(), i);
                if (job.count > 0) jobHandles[i] = job.ScheduleParallelByRef(job.count, chunkSize, default);
            }

            // wait all jobs completed
            JobHandle.CompleteAll(jobHandles);

            // clear native array accessors for jobs
            nativeArrayAccesses.ForEach(naa => naa.Dispose());
            ListPool<NativeArrayAccess>.Release(nativeArrayAccesses);

            // release root level native arrays
            jobs.Dispose();
            jobHandles.Dispose();
        }

        public static unsafe void JobFor<C0, C1, U>(this Query<C0, C1> query, RefActionU<C0, C1, U> action, in U uniform, int chunkSize = 128)
            where C0 : struct
            where C1 : struct
            where U : struct
        {
            query.AssertNotDisposed();
            using var worldLock = query.World.Lock;
            
            // pin delegate and get pointer, store reference
            var gcHandle = GCHandle.Alloc(action);
            RuntimeHelpers.PrepareDelegate(action);
            
            var actionPtr = Unsafe.AsPointer(ref action);
            
            // fill unity jobs
            ListPool<NativeArrayAccess>.Get(out var nativeArrayAccesses);
            var jobCount = query.Archetypes.Count;
            var jobs = new NativeArray<UnityJob<C0, C1, U>>(jobCount, Allocator.Persistent);
            var jobHandles = new NativeArray<JobHandle>(jobCount, Allocator.Persistent);
            for (var i = 0; i < jobCount; i++)
            {
                var table = query.Archetypes[i];
                using var join = table.CrossJoin<C0, C1>(query.StreamTypes);
                if (join.Empty) continue;

                var (s0, s1) = join.Select;
                nativeArrayAccesses.Add(s0.GetNativeArrayAccess(out var memory1));
                nativeArrayAccesses.Add(s1.GetNativeArrayAccess(out var memory2));
                
                jobs[i] = new UnityJob<C0, C1, U>
                {
                    count = table.Count,
                    Memory1 = memory1,
                    Memory2 = memory2,
                    Uniform = uniform,
                    Action = actionPtr
                };
            }

            // schedule unity jobs
            for (var i = 0; i < jobCount; i++)
            {
                ref var job = ref UnsafeUtility.ArrayElementAsRef<UnityJob<C0, C1, U>>(jobs.GetUnsafePtr(), i);
                if (job.count > 0) jobHandles[i] = job.ScheduleParallelByRef(job.count, chunkSize, default);
            }

            // wait all jobs completed
            JobHandle.CompleteAll(jobHandles);
            
            // clear native array accessors for jobs
            nativeArrayAccesses.ForEach(naa => naa.Dispose());
            ListPool<NativeArrayAccess>.Release(nativeArrayAccesses);

            // release root level native arrays
            jobs.Dispose();
            jobHandles.Dispose();
            gcHandle.Free();
        }
        
        [BurstCompile]
        private struct UnityJob<C0, C1> : IJobFor
            where C0 : struct
            where C1 : struct
        {
            internal int count;
            [NativeDisableUnsafePtrRestriction] public unsafe void* Action;
            public NativeArray<C0> Memory1;
            public NativeArray<C1> Memory2;

            public unsafe void Execute(int index)
            {
                ref var c0 = ref UnsafeUtility.ArrayElementAsRef<C0>(Memory1.GetUnsafePtr(), index);
                ref var c1 = ref UnsafeUtility.ArrayElementAsRef<C1>(Memory2.GetUnsafePtr(), index);
                ref var action = ref Unsafe.AsRef<RefAction<C0, C1>>(Action);                
                action.Invoke(ref c0, ref c1);
            }
        }

        [BurstCompile]
        private struct UnityJob<C0, C1, U> : IJobFor
            where C0 : struct
            where C1 : struct
            where U : struct
        {
            internal int count;
            [NativeDisableUnsafePtrRestriction] public unsafe void* Action;
            [NativeDisableContainerSafetyRestriction] public NativeArray<C0> Memory1;
            [NativeDisableContainerSafetyRestriction] public NativeArray<C1> Memory2;
            public U Uniform;

            public unsafe void Execute(int index)
            {
                ref var c0 = ref UnsafeUtility.ArrayElementAsRef<C0>(Memory1.GetUnsafePtr(), index);
                ref var c1 = ref UnsafeUtility.ArrayElementAsRef<C1>(Memory2.GetUnsafePtr(), index);
                ref var action = ref Unsafe.AsRef<RefActionU<C0, C1, U>>(Action);
                action.Invoke(ref c0, ref c1, Uniform);
            }

            public unsafe void Execute()
            {
                ref var action = ref Unsafe.AsRef<RefActionU<C0, C1, U>>(Action);
                for (var i = 0; i < count; i++)
                {
                    ref var c0 = ref UnsafeUtility.ArrayElementAsRef<C0>(Memory1.GetUnsafePtr(), i);
                    ref var c1 = ref UnsafeUtility.ArrayElementAsRef<C1>(Memory2.GetUnsafePtr(), i);
                    action.Invoke(ref c0, ref c1, Uniform);
                }
            }
        }
    }
}