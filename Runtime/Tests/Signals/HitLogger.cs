using UnityEngine;

namespace BattleTurn.SignalBus
{
    /// <summary>
    /// Simple MonoBehaviour that logs PlayerHit signals for testing.
    /// </summary>
    public class HitLogger : MonoBehaviour
    {
        private SignalBus _signalBus = new SignalBus();

        void OnEnable() => _signalBus.Subscribe<PlayerHit>(OnSignal);
        void OnDisable() => _signalBus.Unsubscribe<PlayerHit>(OnSignal);

        public void OnSignal(PlayerHit signal)
        {
            Debug.Log($"Player {signal.AttackerId} dealt {signal.Damage} damage");
        }
    }
}
