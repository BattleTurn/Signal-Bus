using System;

namespace BattleTurn.SignalBus
{
    internal sealed class SignalPipe<T>
    {
        private Action<T>[] _listenersParam = new Action<T>[8];
        private int _countParam;

        private Action[] _listenersNoArg = new Action[4];
        private int _countNoArg;

        private enum OpKind : byte
        {
            AddParam, RemoveParam,
            AddNoArg, RemoveNoArg
        }

        private struct PendingOp
        {
            public OpKind Kind;
            public Action<T> Param;
            public Action NoArg;
        }

        private PendingOp[] _pending = new PendingOp[16];
        private int _pendingHead;
        private int _pendingTail;
        private int _reentrancy;

        // Subscribe with Action<T>
        public void Subscribe(Action<T> listener)
        {
            if (listener == null) return;
            if (_reentrancy > 0)
            {
                EnqueueParam(OpKind.AddParam, listener);
                return;
            }
            for (int i = 0; i < _countParam; i++)
                if (_listenersParam[i] == listener) return;

            EnsureCapacity(ref _listenersParam, _countParam + 1);
            _listenersParam[_countParam++] = listener;
        }

        // Subscribe with Action (no parameter)
        public void Subscribe(Action listener)
        {
            if (listener == null) return;
            if (_reentrancy > 0)
            {
                EnqueueNoArg(OpKind.AddNoArg, listener);
                return;
            }
            for (int i = 0; i < _countNoArg; i++)
                if (_listenersNoArg[i] == listener) return;

            EnsureCapacity(ref _listenersNoArg, _countNoArg + 1);
            _listenersNoArg[_countNoArg++] = listener;
        }

        // Unsubscribe Action<T>
        public void Unsubscribe(Action<T> listener)
        {
            if (listener == null) return;
            if (_reentrancy > 0)
            {
                EnqueueParam(OpKind.RemoveParam, listener);
                return;
            }
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

        // Unsubscribe Action (no parameter)
        public void Unsubscribe(Action listener)
        {
            if (listener == null) return;
            if (_reentrancy > 0)
            {
                EnqueueNoArg(OpKind.RemoveNoArg, listener);
                return;
            }
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

        public void Fire(T signal)
        {
            _reentrancy++;
            int localParam = _countParam;
            int localNoArg = _countNoArg;
            try
            {
                for (int i = 0; i < localParam; i++)
                    _listenersParam[i]?.Invoke(signal);
                for (int i = 0; i < localNoArg; i++)
                    _listenersNoArg[i]?.Invoke();
            }
            finally
            {
                _reentrancy--;
                if (_reentrancy == 0) FlushPending();
            }
        }

        private void EnqueueParam(OpKind kind, Action<T> listener)
        {
            int nextTail = (_pendingTail + 1) % _pending.Length;
            if (nextTail == _pendingHead) GrowPending();
            nextTail = (_pendingTail + 1) % _pending.Length;
            _pending[_pendingTail] = new PendingOp { Kind = kind, Param = listener };
            _pendingTail = nextTail;
        }

        private void EnqueueNoArg(OpKind kind, Action listener)
        {
            int nextTail = (_pendingTail + 1) % _pending.Length;
            if (nextTail == _pendingHead) GrowPending();
            nextTail = (_pendingTail + 1) % _pending.Length;
            _pending[_pendingTail] = new PendingOp { Kind = kind, NoArg = listener };
            _pendingTail = nextTail;
        }

        private void FlushPending()
        {
            while (_pendingHead != _pendingTail)
            {
                var op = _pending[_pendingHead];
                _pendingHead = (_pendingHead + 1) % _pending.Length;
                switch (op.Kind)
                {
                    case OpKind.AddParam:
                        if (op.Param != null)
                        {
                            bool exists = false;
                            for (int i = 0; i < _countParam; i++)
                                if (_listenersParam[i] == op.Param) { exists = true; break; }
                            if (!exists)
                            {
                                EnsureCapacity(ref _listenersParam, _countParam + 1);
                                _listenersParam[_countParam++] = op.Param;
                            }
                        }
                        break;
                    case OpKind.RemoveParam:
                        if (op.Param != null)
                        {
                            for (int i = 0; i < _countParam; i++)
                            {
                                if (_listenersParam[i] == op.Param)
                                {
                                    int last = _countParam - 1;
                                    _listenersParam[i] = _listenersParam[last];
                                    _listenersParam[last] = null;
                                    _countParam--;
                                    break;
                                }
                            }
                        }
                        break;
                    case OpKind.AddNoArg:
                        if (op.NoArg != null)
                        {
                            bool exists2 = false;
                            for (int i = 0; i < _countNoArg; i++)
                                if (_listenersNoArg[i] == op.NoArg) { exists2 = true; break; }
                            if (!exists2)
                            {
                                EnsureCapacity(ref _listenersNoArg, _countNoArg + 1);
                                _listenersNoArg[_countNoArg++] = op.NoArg;
                            }
                        }
                        break;
                    case OpKind.RemoveNoArg:
                        if (op.NoArg != null)
                        {
                            for (int i = 0; i < _countNoArg; i++)
                            {
                                if (_listenersNoArg[i] == op.NoArg)
                                {
                                    int last = _countNoArg - 1;
                                    _listenersNoArg[i] = _listenersNoArg[last];
                                    _listenersNoArg[last] = null;
                                    _countNoArg--;
                                    break;
                                }
                            }
                        }
                        break;
                }
            }
        }

        private static void EnsureCapacity<TArr>(ref TArr[] arr, int needed)
        {
            if (arr.Length >= needed) return;
            int newSize = Math.Max(arr.Length * 2, needed);
            Array.Resize(ref arr, newSize);
        }

        private void GrowPending()
        {
            var old = _pending;
            var newArr = new PendingOp[old.Length * 2];
            int idx = 0;
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