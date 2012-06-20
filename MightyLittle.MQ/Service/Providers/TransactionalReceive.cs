using System;
using System.Messaging;

namespace MightyLittle.MQ.Service.Providers
{
	public class TransactionalReceive : ReceiveProviderBase
	{
		public override void GetMessageFromQueue(string messageId, MessageQueue queue, Action<Message> processMethod)
		{
			using (var transaction = new MessageQueueTransaction())
			{
				transaction.Begin();
				Logger.DebugFormat("Transaction for {0} started.", messageId);

				Message message;
				try
				{
					message = queue.ReceiveById(messageId, TimeSpan.FromSeconds(30), transaction);
					Logger.DebugFormat("Message with id {0} received.", messageId);
				}
				catch (Exception ex)
				{
					transaction.Abort();
					Logger.Error(
						string.Concat("Failed to receive message with id ", messageId, "transactions aborted.")
						, ex);
					return;
				}

				if (message != null)
				{
					processMethod(message);
				}

				if (transaction.Status != MessageQueueTransactionStatus.Aborted)
					transaction.Commit();
			}
		}
	}
}