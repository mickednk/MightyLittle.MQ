using System;
using System.Messaging;
using log4net;

namespace MightyLittle.MQ.Client.Providers
{
	public abstract class SendProviderBase
	{
		protected readonly ILog Logger;
		protected readonly MessageQueue Queue;

		private SendProviderBase()
		{
			Logger = LogManager.GetLogger(GetType());
		}

		protected SendProviderBase(MessageQueue queue) : this()
		{
			if (Queue == null)
				throw new ArgumentNullException("queue");
			if (Queue.CanWrite)
				throw new Exception("You don't have permission to send to queue.");

			Queue = queue;
		}

		public virtual void Send(object message)
		{
			Send(new[] {message});
		}

		public abstract void Send(params object[] messages);
	}
}