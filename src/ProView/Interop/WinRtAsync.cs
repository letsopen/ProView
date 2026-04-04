using System;
using System.Threading.Tasks;
using Windows.Foundation;

namespace ProView.Interop
{
    /// <summary>
    /// 将 WinRT IAsyncOperation / IAsyncAction 转为 Task（不依赖扩展方法程序集加载顺序）。
    /// </summary>
    internal static class WinRtAsync
    {
        public static Task<TResult> AsTask<TResult>(IAsyncOperation<TResult> operation)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            if (operation.Status == AsyncStatus.Completed)
            {
                return Task.FromResult(operation.GetResults());
            }

            var tcs = new TaskCompletionSource<TResult>();
            operation.Completed = (op, status) =>
            {
                try
                {
                    if (status == AsyncStatus.Completed)
                    {
                        tcs.TrySetResult(op.GetResults());
                    }
                    else if (status == AsyncStatus.Error)
                    {
                        Exception ex = op.ErrorCode;
                        tcs.TrySetException(ex ?? new Exception("WinRT async error"));
                    }
                    else
                    {
                        tcs.TrySetCanceled();
                    }
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            };

            return tcs.Task;
        }

        public static Task AsTask(IAsyncAction action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (action.Status == AsyncStatus.Completed)
            {
                return Task.CompletedTask;
            }

            var tcs = new TaskCompletionSource<object>();
            action.Completed = (op, status) =>
            {
                try
                {
                    if (status == AsyncStatus.Completed)
                    {
                        tcs.TrySetResult(null);
                    }
                    else if (status == AsyncStatus.Error)
                    {
                        Exception ex = op.ErrorCode;
                        tcs.TrySetException(ex ?? new Exception("WinRT async error"));
                    }
                    else
                    {
                        tcs.TrySetCanceled();
                    }
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            };

            return tcs.Task;
        }
    }
}
