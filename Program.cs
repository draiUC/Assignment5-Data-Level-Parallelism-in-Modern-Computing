using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;

if (!AdvSimd.IsSupported) throw new Exception("This demo needs an Arm computer with Neon.");
int n = 1_000_003; // The final three elements test the scalar cleanup loop.
float[] x = new float[n], y = new float[n], z = new float[n], temp = new float[n];
for (int i = 0; i < n; i++) { x[i] = (i % 100) * 0.125f; y[i] = (i % 50) * 0.25f; }

// Original version: two loops and a temporary array.
void TwoLoops()
{
    for (int i = 0; i < n; i++) temp[i] = 1.25f * x[i];
    for (int i = 0; i < n; i++) z[i] = temp[i] + y[i];
}

// Loop fusion removes the temporary array accesses.
void Scalar()
{
    for (int i = 0; i < n; i++) z[i] = 1.25f * x[i] + y[i];
}

// Neon performs the same arithmetic on four floats at once.
void Neon()
{
    var a = Vector128.Create(1.25f);
    int i = 0;
    for (; i <= n - 4; i += 4)
    {
        var vx = Vector128.LoadUnsafe(ref x[i]);
        var vy = Vector128.LoadUnsafe(ref y[i]);
        var result = AdvSimd.Add(AdvSimd.Multiply(a, vx), vy);
        result.StoreUnsafe(ref z[i]);
    }
    for (; i < n; i++) z[i] = 1.25f * x[i] + y[i];
}

string[] names = ["TwoLoops", "Scalar", "Neon"];
Action[] methods = [TwoLoops, Scalar, Neon];
double[,] times = new double[3, 5];
using var raw = new StreamWriter("raw_times.csv");
raw.WriteLine("method,trial,ms_per_call");
foreach (var method in methods)
{
    method();
    for (int i = 0; i < n; i++)
        if (z[i] != 1.25f * x[i] + y[i]) throw new Exception("Incorrect output");
    for (int warmup = 0; warmup < 10; warmup++) method();
}
for (int trial = 0; trial < 5; trial++)
    for (int offset = 0; offset < 3; offset++)
    {
        int k = (trial + offset) % 3; // Rotate the order to reduce order bias.
        var watch = Stopwatch.StartNew();
        for (int repeat = 0; repeat < 20; repeat++) methods[k]();
        watch.Stop();
        times[k, trial] = watch.Elapsed.TotalMilliseconds / 20;
        raw.WriteLine(FormattableString.Invariant($"{names[k]},{trial + 1},{times[k, trial]:F6}"));
    }
Console.WriteLine($"Runtime: {RuntimeInformation.FrameworkDescription}; architecture: {RuntimeInformation.ProcessArchitecture}");
Console.WriteLine($"Neon supported: {AdvSimd.IsSupported}; N = {n}; float32; one thread");
Console.WriteLine("Correctness: PASS for all three versions, including the three-element tail");
Console.WriteLine("Five trials, 20 calls per trial, 10 warmup calls per method");
using var summary = new StreamWriter("results.csv");
summary.WriteLine("method,median_ms");
for (int k = 0; k < 3; k++)
{
    double[] values = Enumerable.Range(0, 5).Select(t => times[k, t]).Order().ToArray();
    summary.WriteLine(FormattableString.Invariant($"{names[k]},{values[2]:F6}"));
    Console.WriteLine($"{names[k]}: {values[2]:F3} ms per call (median)");
}
Console.WriteLine($"Sample output z[123] = {z[123]}");
