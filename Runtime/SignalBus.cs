using System;
using System.Collections.Generic;

namespace BattleTurn.SignalBus
{
    /// <summary>
    /// Container for signal pipes. Use <see cref="For{T}"/> to get a pipe for a specific signal type.
    /// </summary>
    public class SignalBus
    {
        private readonly Dictionary<Type, object> _pipes = new();

        public SignalPipe<T> For<T>()
        {
            if (_pipes.TryGetValue(typeof(T), out var obj))
                return (SignalPipe<T>)obj;

            var pipe = new SignalPipe<T>();
            _pipes[typeof(T)] = pipe;
            return pipe;
        }

        /// <summary>
        /// Clear all pipes (for example when unloading a scene).
        /// </summary>
        public void Clear() => _pipes.Clear();
    }
}
