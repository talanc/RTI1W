# RTI1W Ray Tracer Optimization Case Study

## Methodology

This optimization study followed a simple iterative cycle:

1. Make a code change
2. Run prof.ps1 (before and after)
3. Share results and git diff with Claude
4. Claude analyzes the performance impact
5. Keep or revert based on results

All analysis was done by Claude Haiku 4.5 based on profiling data.

Full conversation: https://claude.ai/chat/3c086d86-9dce-4670-88a1-9214be103aba

## Executive Summary

This document chronicles a systematic optimization effort on a .NET 10 ray tracing application (RTI1W). Through targeted micro-optimizations, we achieved **~23% performance improvement on the Tree BVH algorithm and ~20% improvement on the Linear BVH algorithm**, without modifying core algorithms or adding complexity.

**Baseline Performance:**
- Tree BVH: ~2.15s
- Linear BVH: ~2.34s

**Final Performance:**
- Tree BVH: ~1.66s (**23% faster**)
- Linear BVH: ~1.87s (**20% faster**)

---

## Optimization #1: `in` Parameters for Struct Arguments

**Change:** Added `in` modifiers to struct parameters passed through hot paths.

**Files Modified:**
- `Hittable.cs` - `Hit()` method signature
- `LinearBvh.cs` - `Hit()` method signature
- `Helpers.cs` - `IntersectRayBox()` and `SetFaceNormal()` methods

**Example:**
```csharp
// Before
public override bool Hit(Ray r, float tMin, float tMax, out HitRecord hit)

// After
public override bool Hit(in Ray r, float tMin, float tMax, out HitRecord hit)
```

**Rationale:**
- `Ray` is a 36-byte struct (two Vector3s + Vector3 inverse direction)
- Without `in`, each method call copies all 36 bytes
- With `in`, Ray is passed by reference (8-byte pointer)
- This optimization applies to millions of ray-object intersection tests

**Performance Impact: ~6% improvement**

**Why It Works:**
- Eliminates unnecessary struct copying
- Reduces memory bandwidth pressure
- Better CPU cache utilization
- JIT generates more efficient code paths

**Lesson:** Pass large structs by reference in hot paths. Modern C# makes this easy and safe.

---

## Optimization #2: Aggressive Method Inlining

**Change:** Applied `[MethodImpl(MethodImplOptions.AggressiveInlining)]` to frequently-called utility methods.

**Methods Inlined:**
- Vector3 construction helpers: `C3()`, `V3()`, `P3()`
- Vector operations: `Dot()`, `Cross()`, `UnitVector()`
- Random value generation: `RandomValue()`, `RandomVector3()`, `RandomInUnitSphere()`
- Mathematical operations: `Lerp()`, `IsNearZero()`, `IntersectRayBox()`

**Example:**
```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public static Vector3 V3(float x, float y, float z)
{
    return new Vector3(x, y, z);
}
```

**Rationale:**
- These tiny methods are called millions of times per frame
- Method call overhead (stack frame setup, return) is expensive in tight loops
- Inlining replaces the call with direct code generation
- Struct construction helpers are ideal candidates

**Performance Impact: ~4% improvement**

**Why It Works:**
- Eliminates function call overhead
- Allows JIT to optimize across method boundaries
- Keeps frequently-used values in CPU registers

**Lesson:** Inline tiny utility methods that are called in hot loops. Mark them explicitly rather than hoping the JIT does it.

---

## Optimization #3: Remove Nullable Overhead

**Change:** Converted `ScatterRecord?` (nullable struct) to non-nullable `ScatterRecord`.

**Files Modified:**
- `Material.cs` - Changed return type of `Scatter()` method
- `Program.cs` - Simplified scatter logic

**Example:**
```csharp
// Before
var matRecMaybe = hit.Material.Scatter(r, hit);
if (matRecMaybe.HasValue)
{
    var matRec = matRecMaybe.Value;
    return matRec.Attenuation * RayColor(matRec.Scattered, depth - 1);
}
return ColorBlack;

// After
var matRec = hit.Material.Scatter(r, hit);
return matRec.Attenuation * RayColor(matRec.Scattered, depth - 1);
```

**Rationale:**
- Nullable<T> adds overhead: tracking `HasValue` state, boxing/unboxing checks
- Every scatter operation needs a `HasValue` check and `.Value` unwrap
- In ray tracing, "no scatter" is represented by zero attenuation (natural sentinel value)
- This eliminates a branch in a hot loop with millions of iterations

**Performance Impact: ~5-10% improvement** (10% on Linear BVH)

**Why It Works:**
- Eliminates nullable boxing logic
- One fewer branch in hot path
- Simpler control flow = better branch prediction
- JIT generates tighter code

**Lesson:** Nullable structs have real overhead in hot paths. Use sentinel values or non-nullable designs when possible.

---

## Optimization #4: `out` Parameters for Large Structs

**Change:** Changed `Scatter()` method to use `out ScatterRecord` instead of returning by value.

**Files Modified:**
- `Material.cs` - Method signatures
- `Program.cs` - Call site

**Example:**
```csharp
// Before
public abstract ScatterRecord Scatter(in Ray rayIn, in HitRecord rec);
var matRec = hit.Material.Scatter(r, hit);

// After
public abstract void Scatter(in Ray rayIn, in HitRecord rec, out ScatterRecord sr);
hit.Material.Scatter(r, hit, out var matRec);
```

**Rationale:**
- `ScatterRecord` is 48 bytes (Vector3 + Ray)
- Return value optimization (RVO) helps, but `out` guarantees no copying
- Called millions of times in deep recursion

**Performance Impact: ~2% improvement on Tree**

**Why It Works:**
- Passes struct by reference, not by value
- Avoids stack copying overhead
- More pronounced benefit in deep recursion trees

**Lesson:** For large structs returned from hot paths, consider `out` parameters. Measure to confirm it helps.

---

## Optimization #5: Skip Default Struct Initialization

**Change:** Used `Unsafe.SkipInit()` to skip zero-initialization of `HitRecord` struct.

**Files Modified:**
- `Hittable.cs` - `Hit()` methods in BvhHittable, HittableList, Triangle, Sphere
- `LinearBvh.cs` - `Hit()` method

**Example:**
```csharp
// Before
hit = default;  // Zero-initializes all 48 bytes

// After
Unsafe.SkipInit(out hit);  // Skip initialization; set fields explicitly
```

**Rationale:**
- `hit = default` generates a memset instruction to zero 48 bytes
- Happens in every `Hit()` call (millions per frame)
- The struct fields are always set before being read, so zeroing is wasted work
- `Unsafe.SkipInit()` tells the JIT to skip this

**Performance Impact: ~5% improvement**

**Safety:** This is safe because:
- Every code path that returns `true` explicitly sets all fields
- Code paths that return `false` don't read the struct
- We're not reading uninitialized data

**Why It Works:**
- Eliminates wasteful memory write operations
- Reduces memory bandwidth consumption
- JIT generates leaner code

**Lesson:** When you always initialize struct fields explicitly, skip the default initialization. But verify safety carefully — this is unsafe code.

---

## Optimization #6: Array Instead of List

**Change:** Replaced `List<Hittable>` with `Hittable[]` for the scene objects.

**Files Modified:**
- `Hittable.cs` - HittableList class
- `LinearBvh.cs` - LinearBvhHelper class

**Example:**
```csharp
// Before
public List<Hittable> List;
foreach (var obj in List)
{
    if (obj.Hit(r, tMin, closestSoFar, out var objHit))
    {
        hasHit = true;
        hit = objHit;
        closestSoFar = objHit.T;
    }
}

// After
public Hittable[] Items;
foreach (var obj in Items)
{
    if (obj.Hit(r, tMin, closestSoFar, out hit))
    {
        hasHit = true;
        closestSoFar = hit.T;
    }
}
```

**Rationale:**
- Array access is direct memory indexing
- List<T> wraps an array with indirection and enumerator overhead
- foreach on array is optimized by JIT into tight loops
- foreach on List requires enumerator method calls
- Scene is built once at startup, then traversed millions of times
- No need for dynamic mutation

**Performance Impact: ~3% improvement on Tree**

**Why It Works:**
- Array iteration is extremely optimized by JIT
- Better cache locality (contiguous memory)
- No enumerator allocation or virtual calls
- JIT can unroll and SIMD-optimize array loops

**Lesson:** In hot paths that iterate collections repeatedly, arrays beat Lists. Use arrays for read-mostly data structures.

---

## Optimization #7: Code Cleanup (Performance-Neutral)

**Changes:**
- Created `HitRecord` constructor to consolidate initialization
- Removed redundant temporary variables (e.g., `hit2` → reuse `hit`)
- Cleaner variable naming

**Performance Impact: None (performance-neutral refactoring)**

**Why It Still Matters:**
- Clearer, more maintainable code
- Reduces chance of bugs in hot paths
- Zero performance cost
- Good engineering practice

**Lesson:** Performance and code clarity aren't always in conflict. Optimize for readability when there's no cost.

---

## What Didn't Work

### Enum Switch vs Polymorphism (Regression: -3%)

**Attempted Change:** Consolidated three Material subclasses (Lambertian, Metal, Dielectric) into a single class with an enum discriminator and switch statement.

**Why It Failed:**
- Virtual dispatch on Material.Scatter() was already optimized by JIT (devirtualization)
- Modern .NET JIT is excellent at devirtualizing virtual calls with few implementations
- Switch statements in hot loops are slower than optimized virtual dispatch
- Single Material class bloated with unused fields (Fuzz for Lambertian, etc.)

**Lesson:** **Don't assume virtual dispatch is slow.** Modern JITs handle it well, especially with few implementations. Polymorphism can be faster than complex branching logic in hot paths. Measure before refactoring away OOP patterns.

---

## Performance Summary

| Algorithm | Baseline | Final | Improvement |
|-----------|----------|-------|-------------|
| Tree BVH  | 2.15s    | 1.66s | 23%         |
| Linear BVH| 2.34s    | 1.87s | 20%         |

**Per-Optimization Impact:**
1. `in` parameters: ~6%
2. Aggressive inlining: ~4%
3. Remove nullable: ~5-10%
4. `out` parameters: ~2%
5. Skip initialization: ~5%
6. Array vs List: ~3%
7. Code cleanup: ~0%

**Total: ~20-23% improvement**

### Consistency Improvements

Beyond raw speed, optimizations improved consistency:
- Eliminated warm-up spikes from early runs
- Tighter variance across iterations
- Better predictability for benchmarking

---

## Lessons Learned

### What Worked Well

1. **Measure First** — Profile before optimizing. The biggest wins came from identifying hot paths (ray-object intersections, scatter calculations).

2. **Small, Focused Changes** — Each optimization was surgical and easy to revert if it didn't work. This allowed confident experimentation.

3. **Struct Handling** — Modern C# gives you tools (`in`, `out`, `Unsafe.SkipInit`) to eliminate struct copying overhead. Use them in hot paths.

4. **Trust the JIT** — Don't outsmart the optimizer. The JIT is good at:
   - Devirtualizing virtual calls
   - Inlining methods
   - Return value optimization
   - Loop unrolling

5. **Data Structures Matter** — Array vs List isn't a micro-optimization; it's a fundamental choice that affects performance by 3% in tight loops.

### What Didn't Work

1. **Bit Manipulation for Sign Checks** — Clever bit-packing of direction flags didn't help. Simple comparisons were already optimal.

2. **Polymorphism → Switch Statements** — Modern virtual dispatch beats complex branching in hot paths.

3. **Premature Optimization** — Some changes (constructor inlining, code shuffling) had zero impact. Only measure matters.

### The Optimization Wall

After 23% improvement, further gains require:
- Algorithm changes (different BVH traversal, ray-object algorithms)
- SIMD vectorization (ray-box/ray-sphere intersection batching)
- Memory layout optimization (cache-conscious data structures)
- Parallel rendering (multi-threaded ray tracing)
- Hardware acceleration (GPU rendering)

Micro-optimizations have diminishing returns. The low-hanging fruit is gone.

---

## Conclusion

This optimization journey demonstrates that **disciplined micro-optimization can yield significant real-world performance gains** without algorithmic changes. A 23% speedup on a compute-intensive workload translates directly to user experience (faster renders, higher framerates).

Key takeaways:
- Profile first, optimize second
- Use language features (`in`, `out`, `Unsafe`) appropriately in hot paths
- Don't assume what's slow — measure
- Trust modern JIT compilers
- Know when to stop (diminishing returns)

The codebase is now clean, fast, and maintainable. Future optimization should focus on algorithmic improvements: better BVH construction, smarter ray-object testing, or parallel rendering.

---

## Files Modified

- `Metrics.cs` - Inlined metrics collection
- `Helpers.cs` - Aggressive inlining on utility methods
- `Hittable.cs` - `in` parameters, `Unsafe.SkipInit()`, constructor consolidation
- `LinearBvh.cs` - `in` parameters, `Unsafe.SkipInit()`, array iteration
- `Material.cs` - Remove nullable, `out` parameters
- `Program.cs` - Update call sites

---

## Benchmarking Methodology

All measurements taken with:
- .NET 10.0 Release build
- 10-iteration warm-up run (first result discarded)
- 10 measurement iterations reported
- BVH=Tree and BVH=Linear tested separately
- Average of iterations calculated (outliers noted but not excluded)

Performance is stable, with <5% variance in final runs, indicating robust optimizations.
