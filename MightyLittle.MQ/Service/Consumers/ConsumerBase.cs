using System;
using System.Messaging;
using log4net;
using log4net.Config;

namespace MightyLittle.MQ.Service.Consumers
{
	public abstract class ConsumerBase<TMessage> : IConsumer
	{
		protected readonly ILog Logger;

		protected ConsumerBase()
		{
			Logger = LogManager.GetLogger(GetType());
			XmlConfigurator.Configure();
		}

		public virtual Type MessageType
		{
			get { return typeof (TMessage); }
		}

		public abstract void ProcessMessage(Message message);
	}
}