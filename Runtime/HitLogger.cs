using UnityEngine;

namespace BattleTurn.SignalBus
{
    /// <summary>
    /// Simple MonoBehaviour that logs PlayerHit signals for testing.
    /// </summary>
    public class HitLogger : MonoBehaviour, ISignalListener<PlayerHit>
    {
        private SignalBus _signalBus = new SignalBus();

        void OnEnable() => _signalBus.For<PlayerHit>().Subscribe(this);
        void OnDisable() => _signalBus.For<PlayerHit>().Unsubscribe(this);

        public void OnSignal(PlayerHit signal)
        {
            Debug.Log($"Player {signal.AttackerId} dealt {signal.Damage} damage");
        }
    }
}
