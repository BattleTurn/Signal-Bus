namespace BattleTurn.SignalBus
{
    /// <summary>
    /// Generic listener interface for receiving signals of type T.
    /// </summary>
    public interface ISignalListener<T>
    {
        void OnSignal(T signal);
    }
}