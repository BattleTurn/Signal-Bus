using System;
using System.Collections.Generic;

namespace BattleTurn.SignalBus
{
    /// <summary>
    /// Container for signal pipes. Use Subscribe/Unsubscribe/Fire with Action<T>.
    /// </summary>
    public class SignalBus
    {
        private readonly Dictionary<Type, object> _pipes = new();

        public void Subscribe<T>(Action<T> handler)
        {
            GetPipe<T>().Subscribe(handler);
        }

        public void Unsubscribe<T>(Action<T> handler)
        {
            GetPipe<T>().Unsubscribe(handler);
        }

        public void Subscribe(Action handler)
        {
            GetPipe<object>().Subscribe(handler);
        }

        public void Unsubscribe(Action handler)
        {
            GetPipe<object>().Unsubscribe(handler);
        }

        public void Fire<T>(T signal)
        {
            GetPipe<T>().Fire(signal);
        }

        public void Clear() => _pipes.Clear();

        private SignalPipe<T> GetPipe<T>()
        {
            var key = typeof(T);
            if (_pipes.TryGetValue(key, out var obj))
                return (SignalPipe<T>)obj;

            var pipe = new SignalPipe<T>();
            _pipes[key] = pipe;
            return pipe;
        }
    }
}