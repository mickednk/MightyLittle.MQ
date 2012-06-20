using System;
using System.Messaging;

namespace MightyLittle.MQ.Client.Providers
{
	public class TransactionalProvider : SendProviderBase
	{
		public TransactionalProvider(MessageQueue queue) : base(queue)
		{
			if (!queue.Transactional)
				throw new Exception("Queue must support transactional messages to use this provider.");
		}

		public override void Send(params object[] messages)
		{
			using (var transaction = new MessageQueueTransaction())
			{
				transaction.Begin();
				foreach (var message in messages)
				{
					Queue.Send(message, transaction);
				}
				transaction.Commit();
			}
		}
	}
}