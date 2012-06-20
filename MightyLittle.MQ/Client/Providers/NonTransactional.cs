using System;
using System.Messaging;

namespace MightyLittle.MQ.Client.Providers
{
	public class NonTransactional : SendProviderBase
	{
		public NonTransactional(MessageQueue queue) : base(queue)
		{
			if (queue.Transactional)
				throw new Exception("This Sendprovider does not support transactional queues.");
		}

		public override void Send(params object[] messages)
		{
			foreach (var message in messages)
			{
				Queue.Send(message);
			}
		}
	}
}