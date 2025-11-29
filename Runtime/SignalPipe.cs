using System;

namespace BattleTurn.SignalBus
{
    /// <summary>
    /// A per-signal-type pipe that manages listeners and dispatches signals.
    /// Supports both Action&lt;T&gt; (with payload) and Action (no payload).
    /// During firing (reentrancy &gt; 0), subscribe/unsubscribe operations are queued
    /// and applied after dispatch to avoid modifying collections while iterating.
    /// </summary>
    [Serializable]
    public sealed class SignalPipe<T>
    {
        // Listeners that expect the signal payload
        private Action<T>[] _listenersParam = new Action<T>[8];
        private int _countParam;

        // Listeners that don't need payload (e.g., T is an empty struct/class)
        private Action[] _listenersNoArg = new Action[4];
        private int _countNoArg;

        #region PROPERTIES
        public int ParamListenerCount => _countParam;
        public int NoArgListenerCount => _countNoArg;
        #endregion

        /// <summary>
        /// Operation kinds stored in the pending ring-buffer during reentrancy.
        /// </summary>
        private enum OpKind : byte
        {
            AddParam, RemoveParam,
            AddNoArg, RemoveNoArg
        }

        /// <summary>
        /// Pending operation item in the circular queue.
        /// When kind is *Param*, use 'param'; when *NoArg*, use 'noArg'.
        /// </summary>
        private struct PendingOp
        {
            public OpKind kind;
            public Action<T> param;
            public Action noArg;
        }

        // Circular buffer for pending ops when firing
        private PendingOp[] _pending = new PendingOp[16];
        private int _pendingHead;
        private int _pendingTail;

        // Reentrancy counter; >0 means inside Fire
        private int _reentrancy;

        /// <summary>
        /// Subscribe a listener that receives the payload T.
        /// Deduplicates; if called during Fire, it is queued and applied later.
        /// </summary>
        public void Subscribe(Action<T> listener)
        {
            if (listener == null) return;
            if (_reentrancy > 0)
            {
                EnqueueParam(OpKind.AddParam, listener);
                return;
            }

            // Deduplicate
            for (int i = 0; i < _countParam; i++)
                if (_listenersParam[i] == listener) return;

            EnsureCapacity(ref _listenersParam, _countParam + 1);
            _listenersParam[_countParam++] = listener;
        }

        /// <summary>
        /// Subscribe a listener without payload.
        /// Useful when T is empty and you don't want Action&lt;T&gt; signature.
        /// </summary>
        public void Subscribe(Action listener)
        {
            if (listener == null) return;
            if (_reentrancy > 0)
            {
                EnqueueNoArg(OpKind.AddNoArg, listener);
                return;
            }

            // Deduplicate
            for (int i = 0; i < _countNoArg; i++)
                if (_listenersNoArg[i] == listener) return;

            EnsureCapacity(ref _listenersNoArg, _countNoArg + 1);
            _listenersNoArg[_countNoArg++] = listener;
        }

        /// <summary>
        /// Unsubscribe a payload listener. If called during Fire, queued.
        /// </summary>
        public void Unsubscribe(Action<T> listener)
        {
            if (listener == null) return;
            if (_reentrancy > 0)
            {
                EnqueueParam(OpKind.RemoveParam, listener);
                return;
            }

            // Swap-with-last removal to keep O(1) average
            for (int i = 0; i < _countParam; i++)
            {
                if (_listenersParam[i] == listener)
                {
                    int last = _countParam - 1;
                    _listenersParam[i] = _listenersParam[last];
                    _listenersParam[last] = null;
                    _countParam--;
                    return;
                }
            }
        }

        /// <summary>
        /// Unsubscribe a no-arg listener. If called during Fire, queued.
        /// </summary>
        public void Unsubscribe(Action listener)
        {
            if (listener == null) return;
            if (_reentrancy > 0)
            {
                EnqueueNoArg(OpKind.RemoveNoArg, listener);
                return;
            }

            // Swap-with-last removal
            for (int i = 0; i < _countNoArg; i++)
            {
                if (_listenersNoArg[i] == listener)
                {
                    int last = _countNoArg - 1;
                    _listenersNoArg[i] = _listenersNoArg[last];
                    _listenersNoArg[last] = null;
                    _countNoArg--;
                    return;
                }
            }
        }

        /// <summary>
        /// Dispatch the signal to all listeners (payload and no-arg).
        /// Uses snapshot counts to avoid iteration issues when listeners
        /// subscribe/unsubscribe during callbacks. Such ops are queued and
        /// flushed when reentrancy drops to 0.
        /// </summary>
        public void Fire(T signal)
        {
            _reentrancy++;
            int localParam = _countParam;
            int localNoArg = _countNoArg;
            try
            {
                // Invoke payload listeners
                for (int i = 0; i < localParam; i++)
                    _listenersParam[i]?.Invoke(signal);

                // Invoke no-arg listeners
                for (int i = 0; i < localNoArg; i++)
                    _listenersNoArg[i]?.Invoke();
            }
            finally
            {
                _reentrancy--;
                if (_reentrancy == 0) FlushPending();
            }
        }

        /// <summary>
        /// Queue a payload operation into the circular buffer.
        /// Grows the buffer if full.
        /// </summary>
        private void EnqueueParam(OpKind kind, Action<T> listener)
        {
            int nextTail = (_pendingTail + 1) % _pending.Length;
            if (nextTail == _pendingHead) GrowPending(); // buffer full -> grow

            nextTail = (_pendingTail + 1) % _pending.Length;
            _pending[_pendingTail] = new PendingOp { kind = kind, param = listener };
            _pendingTail = nextTail;
        }

        /// <summary>
        /// Queue a no-arg operation into the circular buffer.
        /// Grows the buffer if full.
        /// </summary>
        private void EnqueueNoArg(OpKind kind, Action listener)
        {
            int nextTail = (_pendingTail + 1) % _pending.Length;
            if (nextTail == _pendingHead) GrowPending(); // buffer full -> grow

            nextTail = (_pendingTail + 1) % _pending.Length;
            _pending[_pendingTail] = new PendingOp { kind = kind, noArg = listener };
            _pendingTail = nextTail;
        }

        /// <summary>
        /// Apply all queued subscribe/unsubscribe operations.
        /// Runs only when leaving the outermost Fire (reentrancy == 0).
        /// </summary>
        private void FlushPending()
        {
            while (_pendingHead != _pendingTail)
            {
                var op = _pending[_pendingHead];
                _pendingHead = (_pendingHead + 1) % _pending.Length;

                switch (op.kind)
                {
                    case OpKind.AddParam:
                        if (op.param == null) continue;
                        AddParamListener(op.param);
                        break;

                    case OpKind.RemoveParam:
                        if (op.param == null) continue;
                        RemoveParamListener(op.param);
                        break;

                    case OpKind.AddNoArg:
                        if (op.noArg == null) continue;
                        AddNoArgListener(op.noArg);
                        break;

                    case OpKind.RemoveNoArg:
                        if (op.noArg == null) continue;
                        RemoveNoArgListener(op.noArg);
                        break;
                }
            }
        }

        // Helpers to reduce duplication in FlushPending
        private void AddParamListener(Action<T> listener)
        {
            for (int i = 0; i < _countParam; i++)
            {
                if (_listenersParam[i] == listener) return; // already exists
            }
            EnsureCapacity(ref _listenersParam, _countParam + 1);
            _listenersParam[_countParam++] = listener;
        }

        private void RemoveParamListener(Action<T> listener)
        {
            for (int i = 0; i < _countParam; i++)
            {
                if (_listenersParam[i] == listener)
                {
                    int last = _countParam - 1;
                    _listenersParam[i] = _listenersParam[last];
                    _listenersParam[last] = null;
                    _countParam--;
                    return;
                }
            }
        }

        private void AddNoArgListener(Action listener)
        {
            for (int i = 0; i < _countNoArg; i++)
            {
                if (_listenersNoArg[i] == listener) return; // already exists
            }
            EnsureCapacity(ref _listenersNoArg, _countNoArg + 1);
            _listenersNoArg[_countNoArg++] = listener;
        }

        private void RemoveNoArgListener(Action listener)
        {
            for (int i = 0; i < _countNoArg; i++)
            {
                if (_listenersNoArg[i] == listener)
                {
                    int last = _countNoArg - 1;
                    _listenersNoArg[i] = _listenersNoArg[last];
                    _listenersNoArg[last] = null;
                    _countNoArg--;
                    return;
                }
            }
        }

        /// <summary>
        /// Ensure the array has capacity for 'needed' items; doubles size if not.
        /// </summary>
        private static void EnsureCapacity<TArr>(ref TArr[] arr, int needed)
        {
            if (arr.Length >= needed) return;
            int newSize = Math.Max(arr.Length * 2, needed);
            Array.Resize(ref arr, newSize);
        }

        /// <summary>
        /// Grow the pending circular buffer by 2x.
        /// Preserves order and resets head/tail to linearized segment.
        /// </summary>
        private void GrowPending()
        {
            var old = _pending;
            var newArr = new PendingOp[old.Length * 2];
            int idx = 0;

            // Linearize [head..tail) into new array start
            int i = _pendingHead;
            while (i != _pendingTail)
            {
                newArr[idx++] = old[i];
                i = (i + 1) % old.Length;
            }

            _pending = newArr;
            _pendingHead = 0;
            _pendingTail = idx;
        }
    }
}