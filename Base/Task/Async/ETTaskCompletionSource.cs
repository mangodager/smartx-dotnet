using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace ETModel
{
    public class ETTaskCompletionSource: IAwaiter
    {
        // State(= AwaiterStatus)
        private const int Pending = 0;
        private const int Succeeded = 1;
        private const int Faulted = 2;
        private const int Canceled = 3;

        private int state;
        private ExceptionDispatchInfo exception;
        private Action continuation; // action or list

        AwaiterStatus IAwaiter.Status => (AwaiterStatus) state;

        bool IAwaiter.IsCompleted => state != Pending;

        public ETTask Task => new ETTask(this);

        void IAwaiter.GetResult()
        {
            switch (this.state)
            {
                case Succeeded:
                    return;
                case Faulted:
#if DEBUG
                    this.exception?.Throw();
#else
                    this.exception = null;
#endif
                    return;
                case Canceled:
                {
#if DEBUG
                    this.exception?.Throw(); // guranteed operation canceled exception.
#else
                    this.exception = null;
#endif
                    throw new OperationCanceledException();
                }
                default:
                    throw new NotSupportedException("ETTask does not allow call GetResult directly when task not completed. Please use 'await'.");
            }
        }

        void ICriticalNotifyCompletion.UnsafeOnCompleted(Action action)
        {
            this.continuation = action;
            if (state != Pending)
            {
                TryInvokeContinuation();
            }
        }

        private void TryInvokeContinuation()
        {
            this.continuation?.Invoke();
            this.continuation = null;
        }

        public void SetResult()
        {
            if (this.TrySetResult())
            {
                return;
            }

            throw new InvalidOperationException("TaskT_TransitionToFinal_AlreadyCompleted");
        }

        public void SetException(Exception e)
        {
            if (this.TrySetException(e))
            {
                return;
            }

            throw new InvalidOperationException("TaskT_TransitionToFinal_AlreadyCompleted");
        }

        public bool TrySetResult()
        {
            if (this.state != Pending)
            {
                return false;
            }

            this.state = Succeeded;

            this.TryInvokeContinuation();
            return true;

        }

        public bool TrySetException(Exception e)
        {
            if (this.state != Pending)
            {
                return false;
            }

            this.state = Faulted;

            this.exception = ExceptionDispatchInfo.Capture(e);
            this.TryInvokeContinuation();
            return true;

        }

        public bool TrySetCanceled()
        {
            if (this.state != Pending)
            {
                return false;
            }

            this.state = Canceled;

            this.TryInvokeContinuation();
            return true;

        }

        public bool TrySetCanceled(OperationCanceledException e)
        {
            if (this.state != Pending)
            {
                return false;
            }

            this.state = Canceled;

            this.exception = ExceptionDispatchInfo.Capture(e);
            this.TryInvokeContinuation();
            return true;

        }

        void INotifyCompletion.OnCompleted(Action action)
        {
            ((ICriticalNotifyCompletion) this).UnsafeOnCompleted(action);
        }

        // 超时
        public async void SetTimeOut(float time)
        {
            await System.Threading.Tasks.Task.Delay((int)time*1000);
            if (this.state != Pending)
            {
                return;
            }
            SetResult();
        }

    }

    public class ETTaskCompletionSource<T>: IAwaiter<T>
    {
        // State(= AwaiterStatus)
        private const int Pending = 0;
        private const int Succeeded = 1;
        private const int Faulted = 2;
        private const int Canceled = 3;

        private int state;
        private T value;
        private ExceptionDispatchInfo exception;
        private Action continuation; // action or list

        bool IAwaiter.IsCompleted => state != Pending;

        public ETTask<T> Task => new ETTask<T>(this);

        AwaiterStatus IAwaiter.Status => (AwaiterStatus) state;

        T IAwaiter<T>.GetResult()
        {
            switch (this.state)
            {
                case Succeeded:
                    return this.value;
                case Faulted:
//#if DEBUG
//                    this.exception?.Throw();
//#else
//                    this.exception = null;
//#endif
                    return default(T);
                case Canceled:
                {
#if DEBUG
                    this.exception?.Throw(); // guranteed operation canceled exception.
#else
                    this.exception = null;
#endif
                    throw new OperationCanceledException();
                }
                default:
                    throw new NotSupportedException("ETTask does not allow call GetResult directly when task not completed. Please use 'await'.");
            }
        }

        void ICriticalNotifyCompletion.UnsafeOnCompleted(Action action)
        {
            this.continuation = action;
            if (state != Pending)
            {
                TryInvokeContinuation();
            }
        }

        private void TryInvokeContinuation()
        {
            this.continuation?.Invoke();
            this.continuation = null;
        }

        public void SetResult(T result)
        {
            if (this.TrySetResult(result))
            {
                return;
            }

            //throw new InvalidOperationException("TaskT_TransitionToFinal_AlreadyCompleted");
        }

        public void SetException(Exception e)
        {
            if (this.TrySetException(e))
            {
                return;
            }

            //throw new InvalidOperationException("TaskT_TransitionToFinal_AlreadyCompleted");
        }

        public bool TrySetResult(T result)
        {
            if (this.state != Pending)
            {
                return false;
            }

            this.state = Succeeded;

            this.value = result;
            this.TryInvokeContinuation();
            return true;

        }

        public bool TrySetException(Exception e)
        {
            if (this.state != Pending)
            {
                return false;
            }

            this.state = Faulted;

            this.exception = ExceptionDispatchInfo.Capture(e);
            this.TryInvokeContinuation();
            return true;

        }

        public bool TrySetCanceled()
        {
            if (this.state != Pending)
            {
                return false;
            }

            this.state = Canceled;

            this.TryInvokeContinuation();
            return true;

        }

        public bool TrySetCanceled(OperationCanceledException e)
        {
            if (this.state != Pending)
            {
                return false;
            }

            this.state = Canceled;

            this.exception = ExceptionDispatchInfo.Capture(e);
            this.TryInvokeContinuation();
            return true;

        }

        void IAwaiter.GetResult()
        {
            ((IAwaiter<T>) this).GetResult();
        }

        void INotifyCompletion.OnCompleted(Action action)
        {
            ((ICriticalNotifyCompletion) this).UnsafeOnCompleted(action);
        }

        // 超时
        public async void SetTimeOut(float time, T result)
        {
            await System.Threading.Tasks.Task.Delay((int)(time * 1000));
            if (this.state != Pending)
            {
                return;
            }
            SetResult(result);
        }

    }
}