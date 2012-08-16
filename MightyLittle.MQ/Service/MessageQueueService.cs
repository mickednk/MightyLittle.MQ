using System;
using System.Collections.Generic;
using System.Linq;
using System.Messaging;
using MightyLittle.MQ.Service.Consumers;
using MightyLittle.MQ.Service.Providers;
using log4net;
using log4net.Config;

namespace MightyLittle.MQ.Service
{
	public class MessageQueueService : IDisposable
	{
		private readonly List<IConsumer> _registeredConsumers;
		private readonly ILog logger;
		private ReceiveProviderBase _receiveProvider;

		private MessageQueueService()
		{
			logger = LogManager.GetLogger(typeof (MessageQueueService));
			XmlConfigurator.Configure();
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
			if (Queue == null) return;

			//unsubscribe and dispose
			Queue.PeekCompleted -= QueueOnPeekCompleted;
			Queue.Dispose();
		}

		public void RegisterConsumer<TConsumer>() where TConsumer : IConsumer, new()
		{
			RegisterConsumer(typeof (TConsumer));
		}

		public void RegisterConsumer(Type consumerType)
		{
			if (consumerType == null)
			{
				throw new ArgumentNullException("consumerType");
			}
			if (consumerType.GetInterface(typeof (IConsumer).FullName) == null)
			{
				throw new ArgumentException("consumerType must implement IConsumer!");
			}

			//make instance.
			var consumer = (IConsumer) Activator.CreateInstance(consumerType);

			if (consumer == null)
			{
				throw new Exception(string.Format("Failed to create instance of type '{0}'", consumerType));
			}

			var formatter = (XmlMessageFormatter) Queue.Formatter;
			if (formatter.TargetTypes.Contains(consumer.MessageType))
				throw new Exception("Message type is already registered with another consumer.");

			_registeredConsumers.Add(consumer);
			formatter.TargetTypes = formatter.TargetTypes.Concat(new[] {consumer.MessageType}).Distinct().ToArray();
			logger.DebugFormat("Consumer '{0}' registered to handel messages of type '{1}'", consumerType.FullName, consumer.MessageType);
		}

		public void Start()
		{
			Queue.BeginPeek();
			logger.DebugFormat("Started listening on queue '{0}'", Queue.QueueName);
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

			if (Queue.Transactional)
			{
				_receiveProvider = new TransactionalReceive();
			}
			else
			{
				_receiveProvider = new NonTransactionalReceive();
			}

			//setup events and start.
			Queue.PeekCompleted += QueueOnPeekCompleted;
			logger.DebugFormat("Queue '{0}' initiated.", Queue.QueueName);
		}

		private void QueueOnPeekCompleted(object sender, PeekCompletedEventArgs peekCompletedEventArgs)
		{
			var asyncQueue = (MessageQueue) sender;
			logger.DebugFormat("Peeked message with id {0}", peekCompletedEventArgs.Message.Id);

			_receiveProvider.GetMessageFromQueue(
				peekCompletedEventArgs.Message.Id,
				asyncQueue,
				delegate(Message message)
				{
					IConsumer consumer = null;
					try
					{
						consumer = _registeredConsumers.FirstOrDefault(c => c.MessageType == message.Body.GetType());
					}
					catch (InvalidOperationException ex)
					{
						logger.Error("Failed to ready message body", ex);
					}
					catch (Exception ex)
					{
						logger.Error("Failed to get consumer, might be related to message body", ex);
					}

					if (consumer != null)
					{
						try
						{
							consumer.ProcessMessage(message);
						}
						catch (Exception ex)
						{
							logger.Warn("Failed to process message", ex);
						}
					}
				});

			logger.Debug("Peeking for next message.");
			//peek next.
			asyncQueue.BeginPeek();
		}
	}
}