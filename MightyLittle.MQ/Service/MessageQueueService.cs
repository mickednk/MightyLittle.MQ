using System;
using System.Collections.Generic;
using System.Linq;
using System.Messaging;
using MightyLittle.MQ.Service.Consumers;

namespace MightyLittle.MQ.Service
{
	public class MessageQueueService : IDisposable
	{
		private readonly List<IConsumer> _registeredConsumers;

		private MessageQueueService()
		{
			_registeredConsumers = new List<IConsumer>();
		}

		public MessageQueueService(string queueName)
			: this()
		{
			Initialize(queueName);
		}

		protected MessageQueue Queue { get; private set; }

		public void Dispose()
		{
			if (Queue != null)
				Queue.Dispose();
		}

		public void RegisterConsumer<TConsumer>() where TConsumer : IConsumer, new()
		{
			var consumer = new TConsumer();
			_registeredConsumers.Add(consumer);

			var formatter = (XmlMessageFormatter) Queue.Formatter;
			formatter.TargetTypes = formatter.TargetTypes.Concat(new[] {consumer.MessageType}).Distinct().ToArray();
		}

		public void Start()
		{
			Queue.BeginPeek();
		}

		private void Initialize(string queueName)
		{
			if (string.IsNullOrEmpty(queueName))
				throw new ArgumentException("queueName must be set to a valid address!");

			if (!queueName.StartsWith("FormatName:", StringComparison.OrdinalIgnoreCase) && !MessageQueue.Exists(queueName))
				MessageQueue.Create(queueName, true);

			Queue = new MessageQueue(queueName, true, true, QueueAccessMode.Receive)
			        {
			        	Formatter = new XmlMessageFormatter()
			        };

			//setup events and start.
			Queue.PeekCompleted += QueueOnPeekCompleted;
		}

		private void ProcessMessageFromQueue(string messageId, MessageQueue asyncQueue)
		{
			using (var transaction = new MessageQueueTransaction())
			{
				transaction.Begin();

				Message message;
				try
				{
					message = asyncQueue.ReceiveById(messageId, TimeSpan.FromSeconds(30), transaction);
				}
				catch (Exception)
				{
					transaction.Abort();
					return;
				}

				if (message != null)
				{
					IConsumer consumer = null;
					try
					{
						consumer = _registeredConsumers.FirstOrDefault(c => c.MessageType == message.Body.GetType());
					}
					catch (InvalidOperationException ex)
					{
						Console.WriteLine(ex.Message);
					}
					catch (Exception ex)
					{
						Console.WriteLine(ex.Message);
					}

					if (consumer != null)
					{
						try
						{
							consumer.ProcessMessage(message);
						}
						catch (Exception ex)
						{
							Console.WriteLine(ex.Message);
						}
					}
				}

				if (transaction.Status != MessageQueueTransactionStatus.Aborted)
					transaction.Commit();
			}
		}

		private void QueueOnPeekCompleted(object sender, PeekCompletedEventArgs peekCompletedEventArgs)
		{
			var asyncQueue = (MessageQueue) sender;

			ProcessMessageFromQueue(peekCompletedEventArgs.Message.Id, asyncQueue);

			//peek next.
			asyncQueue.BeginPeek();
		}
	}
}