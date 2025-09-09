using UnityEngine;

namespace BattleTurn.SignalBus
{
    /// <summary>
    /// Simple runtime tester that fires int signals each frame.
    /// </summary>
    public class SignalBusRuntimeTester : MonoBehaviour, ISignalListener<int>
    {
        private SignalBus _bus;
        private SignalPipe<int> _pipe;

        private int _counter;

        void Start()
        {
            _bus = new SignalBus();
            _pipe = _bus.For<int>();
            _pipe.Subscribe(this);
        }

        void Update()
        {
            _pipe.Fire(_counter++);
        }

        public void OnSignal(int signal)
        {
            // no-op, or uncomment the Debug.Log to test
            // Debug.Log($"Signal: {signal}");
        }
    }
}
