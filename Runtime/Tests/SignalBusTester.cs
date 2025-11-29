using UnityEngine;

namespace BattleTurn.SignalBus
{
    /// <summary>
    /// Simple runtime tester that fires int signals each frame.
    /// </summary>
    public class SignalBusTester : MonoBehaviour
    {
        [SerializeField] private SignalBus _bus;

        private int _counter;

        void Start()
        {
            _bus = new SignalBus();
            _bus.Subscribe<int>(OnSignal);
        }

        void Update()
        {
            _bus.Fire(_counter++);
        }

        public void OnSignal(int signal)
        {
            // no-op, or uncomment the Debug.Log to test
            Debug.Log($"Signal: {signal}");
        }
    }
}
