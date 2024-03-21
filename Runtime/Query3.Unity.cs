// SPDX-License-Identifier: MIT

using System;
using System.Runtime.CompilerServices;
using fennecs.Utility;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine.Pool;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

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
    public static class Query3Extensions
    {
        public static void JobFor<C0, C1, C2>(this Query<C0, C1, C2> query, RefAction<C0, C1, C2> action, int chunkSize = 128)
            where C0 : struct
            where C1 : struct
            where C2 : struct
        {
            query.AssertNotDisposed();
            using var worldLock = query.World.Lock;
            
            // fill unity jobs
            var jobCount = query.Archetypes.Count;
            var jobs = new NativeArray<UnityJob<C0, C1, C2>>(jobCount, Allocator.Persistent);
            var jobHandles = new NativeArray<JobHandle>(jobCount, Allocator.Persistent);
            ListPool<NativeArrayAccess>.Get(out var nativeArrayAccesses);
            for (var i = 0; i < jobCount; i++)
            {
                var table = query.Archetypes[i];
                using var join = table.CrossJoin<C0, C1, C2>(query.StreamTypes);
                if (join.Empty) continue;

                var (s0, s1, s2) = join.Select;
                nativeArrayAccesses.Add(s0.GetNativeArrayAccess(out var memory1));
                nativeArrayAccesses.Add(s1.GetNativeArrayAccess(out var memory2));
                nativeArrayAccesses.Add(s2.GetNativeArrayAccess(out var memory3));
                unsafe
                {
                    jobs[i] = new UnityJob<C0, C1, C2>
                    {
                        count = table.Count, // storage.Length is the capacity, not the count.
                        Memory1 = memory1,
                        Memory2 = memory2,
                        Memory3 = memory3,
                        Action = Unsafe.AsPointer(ref action)
                    };
                }
            }

            // schedule unity jobs
            unsafe
            {
                for (var i = 0; i < jobCount; i++)
                {
                    ref var job = ref UnsafeUtility.ArrayElementAsRef<UnityJob<C0, C1, C2>>(jobs.GetUnsafePtr(), i);
                    jobHandles[i] = job.ScheduleParallelByRef(job.count, chunkSize, default);
                }
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

        public static void JobFor<C0, C1, C2, U>(this Query<C0, C1, C2> query, RefActionU<C0, C1, C2, U> action, in U uniform, int chunkSize = 128)
            where C0 : struct
            where C1 : struct
            where C2 : struct
        {
            query.AssertNotDisposed();
            using var worldLock = query.World.Lock;
            
            // fill unity jobs
            var jobCount = query.Archetypes.Count;
            var jobs = new NativeArray<UnityJob<C0, C1, C2, U>>(jobCount, Allocator.Persistent);
            var jobHandles = new NativeArray<JobHandle>(jobCount, Allocator.Persistent);
            ListPool<NativeArrayAccess>.Get(out var nativeArrayAccesses);
            for (var i = 0; i < jobCount; i++)
            {
                var table = query.Archetypes[i];
                using var join = table.CrossJoin<C0, C1, C2>(query.StreamTypes);
                if (join.Empty) continue;

                var (s0, s1, s2) = join.Select;
                
                unsafe
                {
                    jobs[i] = new UnityJob<C0, C1, C2, U>
                    {
                        count = table.Count,
                        Memory1 = Unsafe.AsPointer(ref s0[0]),
                        Memory2 = Unsafe.AsPointer(ref s1[0]),
                        Memory3 = Unsafe.AsPointer(ref s2[0]),
                        Uniform = uniform,
                        Action = Unsafe.AsPointer(ref action)
                    };
                }
            }

            // schedule unity jobs
            unsafe
            {
                for (var i = 0; i < jobCount; i++)
                {
                    ref var job = ref UnsafeUtility.ArrayElementAsRef<UnityJob<C0, C1, C2, U>>(jobs.GetUnsafePtr(), i);
                    jobHandles[i] = job.ScheduleParallelByRef(job.count, chunkSize, default);
                }
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
        
        [BurstCompile]
        private struct UnityJob<C0, C1, C2> : IJobFor
            where C0 : struct
            where C1 : struct
            where C2 : struct
        {
            internal int count;
            [NativeDisableUnsafePtrRestriction] public unsafe void* Action;
            [NativeDisableContainerSafetyRestriction] public NativeArray<C0> Memory1;
            [NativeDisableContainerSafetyRestriction] public NativeArray<C1> Memory2;
            [NativeDisableContainerSafetyRestriction] public NativeArray<C2> Memory3;

            public unsafe void Execute(int index)
            {
                Unsafe.AsRef<RefAction<C0, C1, C2>>(Action).Invoke(
                    ref UnsafeUtility.ArrayElementAsRef<C0>(Memory1.GetUnsafePtr(), index),
                    ref UnsafeUtility.ArrayElementAsRef<C1>(Memory2.GetUnsafePtr(), index),
                    ref UnsafeUtility.ArrayElementAsRef<C2>(Memory3.GetUnsafePtr(), index)
                );
            }
        }

        [BurstCompile]
        private struct UnityJob<C0, C1, C2, U> : IJobFor
            where C0 : struct
            where C1 : struct
            where C2 : struct
        {
            internal int count;
            [NativeDisableUnsafePtrRestriction] public unsafe void* Action;
            [NativeDisableUnsafePtrRestriction] public unsafe void* Memory1;
            [NativeDisableUnsafePtrRestriction] public unsafe void* Memory2;
            [NativeDisableUnsafePtrRestriction] public unsafe void* Memory3;
            public U Uniform;

            public unsafe void Execute(int index)
            {
                Unsafe.AsRef<RefActionU<C0, C1, C2, U>>(Action).Invoke(
                    ref UnsafeUtility.ArrayElementAsRef<C0>(Memory1, index),
                    ref UnsafeUtility.ArrayElementAsRef<C1>(Memory2, index),
                    ref UnsafeUtility.ArrayElementAsRef<C2>(Memory3, index),
                    Uniform
                    );
            }
        }
    }
}