﻿// SPDX-License-Identifier: MIT

using System;
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
    public static class Query1Extensions
    {
        public static void Cross<C0>(this Query<C0> query, in Action<C0[], int> action)
        {
            query.AssertNotDisposed();
            using var worldLock = query.World.Lock;
            
            for (var i = 0; i < query.Archetypes.Count; i++)
            {
                var table = query.Archetypes[i];
                using var join = table.CrossJoin<C0>(query.StreamTypes);
                if (join.Empty) continue;

                action(join.Select, table.Count);
            }
        }
        
        public static void JobFor<C0>(this Query<C0> query, RefAction<C0> action, int chunkSize = 128)
            where C0 : struct
        {
            query.AssertNotDisposed();
            using var worldLock = query.World.Lock;
            
            // fill unity jobs
            var jobCount = query.Archetypes.Count;
            var jobs = new NativeArray<UnityJob<C0>>(jobCount, Allocator.Persistent);
            var jobHandles = new NativeArray<JobHandle>(jobCount, Allocator.Persistent);
            for (var i = 0; i < jobCount; i++)
            {
                var table = query.Archetypes[i];
                using var join = table.CrossJoin<C0>(query.StreamTypes);
                if (join.Empty) continue;

                var s0 = join.Select;
                unsafe
                {
                    jobs[i] = new UnityJob<C0>
                    {
                        count = table.Count, // storage.Length is the capacity, not the count.
                        Memory1 = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<C0>(Unsafe.AsPointer(ref s0), table.Count, Allocator.Persistent),
                        Action = Unsafe.AsPointer(ref action)
                    };
                }
            }

            // schedule unity jobs
            unsafe
            {
                for (var i = 0; i < jobCount; i++)
                {
                    ref var job = ref UnsafeUtility.ArrayElementAsRef<UnityJob<C0>>(jobs.GetUnsafePtr(), i);
                    jobHandles[i] = job.ScheduleParallelByRef(job.count, chunkSize, default);
                }
            }

            // wait all jobs completed
            JobHandle.CompleteAll(jobHandles);

            // release root level native arrays
            jobs.Dispose();
            jobHandles.Dispose();
        }

        public static void JobFor<C0, U>(this Query<C0> query, RefActionU<C0, U> action, ref U uniform, int chunkSize = 128)
            where C0 : struct
        {
            query.AssertNotDisposed();
            using var worldLock = query.World.Lock;
            
            // fill unity jobs
            var jobCount = query.Archetypes.Count;
            var jobs = new NativeArray<UnityJob<C0, U>>(jobCount, Allocator.Persistent);
            var jobHandles = new NativeArray<JobHandle>(jobCount, Allocator.Persistent);
            ListPool<NativeArrayAccess>.Get(out var nativeArrayAccesses);
            for (var i = 0; i < jobCount; i++)
            {
                var table = query.Archetypes[i];
                using var join = table.CrossJoin<C0>(query.StreamTypes);
                if (join.Empty) continue;

                var s0 = join.Select;
                unsafe
                {
                    nativeArrayAccesses.Add(s0.GetNativeArrayAccess(out var memory1));
                    jobs[i] = new UnityJob<C0, U>
                    {
                        count = table.Count,
                        Memory1 = memory1,
                        Uniform = uniform,
                        Action = Unsafe.AsPointer(ref action),
                        FP = BurstCompiler.CompileFunctionPointer(action)
                    };
                }
            }

            // schedule unity jobs
            unsafe
            {
                for (var i = 0; i < jobCount; i++)
                {
                    ref var job = ref UnsafeUtility.ArrayElementAsRef<UnityJob<C0, U>>(jobs.GetUnsafePtr(), i);
                    jobHandles[i] = job.ScheduleParallelByRef(job.count, chunkSize, default);
                }
            }

            // wait all jobs completed
            JobHandle.CompleteAll(jobHandles);
            
            nativeArrayAccesses.ForEach(naa => naa.Dispose());
            ListPool<NativeArrayAccess>.Release(nativeArrayAccesses);

            // release root level native arrays
            jobs.Dispose();
            jobHandles.Dispose();
        }
        
        [BurstCompile]
        private struct UnityJob<C0> : IJobFor
            where C0 : struct
        {
            internal int count;
            [NativeDisableContainerSafetyRestriction] public NativeArray<C0> Memory1;
            [NativeDisableUnsafePtrRestriction] public unsafe void* Action;

            public unsafe void Execute(int index)
            {
                Unsafe.AsRef<RefAction<C0>>(Action).Invoke(
                    ref UnsafeUtility.ArrayElementAsRef<C0>(Memory1.GetUnsafePtr(), index)
                );
            }
        }

        [BurstCompile]
        private struct UnityJob<C0, U> : IJobFor
            where C0 : struct
        {
            public int count;
            [NativeDisableUnsafePtrRestriction] public unsafe void* Action;
            [NativeDisableContainerSafetyRestriction] public NativeArray<C0> Memory1;
            public U Uniform;
            public FunctionPointer<RefActionU<C0, U>> FP;

            public unsafe void Execute(int index)
            {
                Unsafe.AsRef<RefActionU<C0, U>>(Action).Invoke(
                    ref UnsafeUtility.ArrayElementAsRef<C0>(Memory1.GetUnsafePtr(), index),
                    Uniform
                    );
            }
        }
    }
}