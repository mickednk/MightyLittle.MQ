using System;
using System.Messaging;

namespace MightyLittle.MQ.Service.Consumers
{
	public abstract class ConsumerBase<TMessage> : IConsumer
	{
		public virtual Type MessageType
		{
			get { return typeof (TMessage); }
		}

		public abstract void ProcessMessage(Message message);
	}
}