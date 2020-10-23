using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Threading;

namespace ETModel
{
	public class OneThreadSynchronizationContext : SynchronizationContext
	{
		public static OneThreadSynchronizationContext Instance { get; } = new OneThreadSynchronizationContext();

		public  readonly Thread mainThread = Thread.CurrentThread;
		private readonly int mainThreadId  = Thread.CurrentThread.ManagedThreadId;

		// 线程同步队列,发送接收socket回调都放到该队列,由poll线程统一执行
		private readonly ConcurrentQueue<Action> queue = new ConcurrentQueue<Action>();

		private Action a;

		public void Update()
		{
			try
			{
				while (true)
				{
					if (!this.queue.TryDequeue(out a))
					{
						return;
					}
					a();
				}
			}
            catch(Exception)
			{
				Log.Error("OneThreadSynchronizationContext.Update");
			}
		}

		public override void Post(SendOrPostCallback callback, object state)
		{
			if (Thread.CurrentThread.ManagedThreadId == this.mainThreadId)
			{
				try
				{
					callback(state);
				}
				catch (Exception)
				{
					Log.Error("OneThreadSynchronizationContext.Post callback");
				}
				return;
			}
		
			this.queue.Enqueue(() => {
				try
				{
					callback(state);
				}
				catch (Exception)
				{
					Log.Error("OneThreadSynchronizationContext.Post Enqueue");
				}
			});
		}

	}
}
