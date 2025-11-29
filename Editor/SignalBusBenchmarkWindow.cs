
using UnityEditor;
using UnityEngine;
using System.Diagnostics;

namespace BattleTurn.SignalBus.Editor
{
    /// <summary>
    /// Editor window for benchmarking the signal bus performance.
    /// </summary>
    public class SignalBusBenchmarkWindow : EditorWindow
    {
        private const int Iterations = 100000;
        private string _result = "";

        [MenuItem("Window/Signal Bus Benchmark")]
        public static void ShowWindow()
        {
            GetWindow<SignalBusBenchmarkWindow>("Signal Bus Benchmark");
        }

        private void OnGUI()
        {
            if (GUILayout.Button("Run Benchmark"))
            {
                RunBenchmark();
            }

            EditorGUILayout.TextArea(_result, GUILayout.Height(200));
        }

        private void RunBenchmark()
        {
            var bus = new SignalBus();

            bus.Subscribe<int>(OnSignal);

            // Warmup
            bus.Fire(123);

            // Force GC before test
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();

            long memBefore = System.GC.GetTotalMemory(true);

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < Iterations; i++)
            {
                bus.Fire(i);
            }
            sw.Stop();

            long memAfter = System.GC.GetTotalMemory(false);

            _result =
                $"Iterations: {Iterations}\n" +
                $"Time: {sw.ElapsedMilliseconds} ms\n" +
                $"Alloc: {memAfter - memBefore} bytes";
        }

        public void OnSignal(int signal)
        {
            // no-op
        }
    }
}
