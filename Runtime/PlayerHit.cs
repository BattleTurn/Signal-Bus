namespace BattleTurn.SignalBus
{
    /// <summary>
    /// Example payload for a player hit event.
    /// </summary>
    public struct PlayerHit
    {
        public int AttackerId;
        public float Damage;
    }
}