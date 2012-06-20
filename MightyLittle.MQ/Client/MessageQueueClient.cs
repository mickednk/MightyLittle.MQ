using System;
using System.Messaging;
using MightyLittle.MQ.Client.Providers;
using log4net;

namespace MightyLittle.MQ.Client
{
	public class MessageQueueClient : IDisposable
	{
		private readonly ILog logger;
		private readonly MessageQueue _queue;
		private readonly SendProviderBase _sendProvider;

		private MessageQueueClient()
		{
			logger = LogManager.GetLogger(typeof (MessageQueueClient));
		}

		public MessageQueueClient(string queueName)
			: this()
		{
			if (string.IsNullOrEmpty(queueName))
				throw new ArgumentException("queueName must be set to a valid address!");

			if (!queueName.StartsWith("FormatName:", StringComparison.OrdinalIgnoreCase) && !MessageQueue.Exists(queueName))
				throw new Exception("Queue doesn't exist!");

			_queue = new MessageQueue(queueName, true, true, QueueAccessMode.Send)
			{
				Formatter = new XmlMessageFormatter()
			};

			if (_queue.Transactional)
			{
				_sendProvider = new TransactionalProvider(_queue);				
			}
			else
			{
				_sendProvider = new NonTransactional(_queue);
			}
		}

		public void Send(params object[] messages)
		{
			if (_sendProvider == null)
				throw new Exception("No sendprovider has been initiated.");

			_sendProvider.Send(messages);
		}

		public void Dispose()
		{
			if (_queue != null)
				_queue.Dispose();
		}
	}
}