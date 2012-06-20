using System;
using System.Messaging;

namespace MightyLittle.MQ.Service.Providers
{
	public class NonTransactionalReceive : ReceiveProviderBase
	{
		public override void GetMessageFromQueue(string messageId, MessageQueue queue, Action<Message> processMethod)
		{
			Message message;
			try
			{
				message = queue.ReceiveById(messageId, TimeSpan.FromSeconds(30));
				Logger.DebugFormat("Message with id {0} received.", messageId);
			}
			catch (Exception ex)
			{
				Logger.Error(
					string.Concat("Failed to receive message with id ", messageId)
					, ex);
				return;
			}

			if (message != null)
			{
				processMethod(message);
			}
		}
	}
}