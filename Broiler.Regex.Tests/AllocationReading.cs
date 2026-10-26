using System;
using System.Runtime;
using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// The tests that read check A-8's allocations (<see cref="AllocationReading"/>): run as one collection, after the
/// collections that run in parallel and with no other test beside them, so that no other thread's allocations reach the
/// garbage collector's budget while a reading holds it off.
/// </summary>
[CollectionDefinition(AllocationReading.Collection, DisableParallelization = true)]
public sealed class AllocationReadingCollection;

/// <summary>
/// Bundle <c>sjl-11-001</c>'s check A-8, read (requirement R8): the bytes the measuring thread allocates in each of a
/// number of calls, made after calls that warm the shared array pool, with the garbage collector held off from before the
/// first warming call to after the last measured one (<see cref="GC.TryStartNoGCRegion(long)"/>). A collection is what
/// trims the pool, so with none every measured call finds what the one before it gave back, and each, not the least of
/// them, must allocate nothing for the check to hold.
/// </summary>
internal static class AllocationReading
{
    /// <summary>The name of the collection the reading tests are in.</summary>
    internal const string Collection = "Check A-8's allocation readings";

    /// <summary>The bytes the collector may hand out while it is held off: the warming calls' arrays and the test's own.</summary>
    private const long Budget = 64L << 20;

    /// <summary>
    /// The bytes this thread allocates in each of <paramref name="calls"/> calls of <paramref name="call"/>, after
    /// <paramref name="warm"/> calls that are not measured, with no collection from the first of them to the last. The
    /// call asserts nothing itself, as an assertion may allocate; it keeps what it answers for the test to assert after.
    /// </summary>
    internal static long[] BytesPerCall(Action call, int warm = 3, int calls = 20)
    {
        // A collection now, and its finalizers, the pool's trimming among them, run before the reading starts, so that
        // the collector has room for the budget and no trimming of an earlier collection runs during the reading.
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Assert.True(GC.TryStartNoGCRegion(Budget), "the garbage collector could not be held off");
        var bytes = new long[calls];
        try
        {
            GC.WaitForPendingFinalizers();
            for (var i = 0; i < warm; i++)
                call();
            for (var i = 0; i < calls; i++)
            {
                var before = GC.GetAllocatedBytesForCurrentThread();
                call();
                bytes[i] = GC.GetAllocatedBytesForCurrentThread() - before;
            }
        }
        finally
        {
            var held = GCSettings.LatencyMode == GCLatencyMode.NoGCRegion;
            if (held)
                GC.EndNoGCRegion();
            Assert.True(held, "a collection ran during the reading, so the pool may have been trimmed between two calls");
        }

        return bytes;
    }
}
