namespace BattleTurn.SignalBus
{
    /// <summary>
    /// A lightweight pipe for a specific signal type T. Supports subscribe/unsubscribe
    /// without allocations and allows subscribe/unsubscribe to be deferred while firing.
    /// </summary>
    public sealed class SignalPipe<T>
    {
        private ISignalListener<T>[] _listeners;
        private int _count;

        private PendingOp[] _pending;
        private int _pendingHead, _pendingTail;
        private int _reentrancy;

        public SignalPipe(int listenerCap = 16, int pendingCap = 16)
        {
            _listeners = new ISignalListener<T>[listenerCap];
            _pending = new PendingOp[pendingCap];
        }

        public bool Subscribe(ISignalListener<T> listener)
        {
            if (listener == null) return false;

            if (_reentrancy > 0)
                return Enqueue(PendingKind.Add, listener);

            // avoid duplicates
            for (int i = 0; i < _count; i++)
                if (ReferenceEquals(_listeners[i], listener))
                    return true;

            if (_count >= _listeners.Length) return false;

            _listeners[_count++] = listener;
            return true;
        }

        public bool Unsubscribe(ISignalListener<T> listener)
        {
            if (listener == null) return false;

            if (_reentrancy > 0)
                return Enqueue(PendingKind.Remove, listener);

            for (int i = 0; i < _count; i++)
            {
                if (ReferenceEquals(_listeners[i], listener))
                {
                    int last = _count - 1;
                    _listeners[i] = _listeners[last];
                    _listeners[last] = null;
                    _count--;
                    return true;
                }
            }
            return false;
        }

        public void Fire(in T signal)
        {
            _reentrancy++;
            try
            {
                int localCount = _count;
                for (int i = 0; i < localCount; i++)
                {
                    _listeners[i]?.OnSignal(signal);
                }
            }
            finally
            {
                _reentrancy--;
                if (_reentrancy == 0)
                    FlushPending();
            }
        }

        // ===== Pending ops =====
        private enum PendingKind : byte { Add, Remove }
        private struct PendingOp { public PendingKind Kind; public ISignalListener<T> Listener; }

        private bool Enqueue(PendingKind kind, ISignalListener<T> l)
        {
            int nextTail = (_pendingTail + 1) % _pending.Length;
            if (nextTail == _pendingHead) return false; // full queue

            _pending[_pendingTail].Kind = kind;
            _pending[_pendingTail].Listener = l;
            _pendingTail = nextTail;
            return true;
        }

        private void FlushPending()
        {
            while (_pendingHead != _pendingTail)
            {
                var op = _pending[_pendingHead];
                _pendingHead = (_pendingHead + 1) % _pending.Length;

                if (op.Kind == PendingKind.Add)
                {
                    bool dup = false;
                    for (int i = 0; i < _count; i++)
                        if (ReferenceEquals(_listeners[i], op.Listener)) { dup = true; break; }

                    if (!dup && _count < _listeners.Length)
                        _listeners[_count++] = op.Listener;
                }
                else // Remove
                {
                    for (int i = 0; i < _count; i++)
                    {
                        if (ReferenceEquals(_listeners[i], op.Listener))
                        {
                            int last = _count - 1;
                            _listeners[i] = _listeners[last];
                            _listeners[last] = null;
                            _count--;
                            break;
                        }
                    }
                }
            }
        }
    }
}