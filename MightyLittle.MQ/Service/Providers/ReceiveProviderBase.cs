using System;
using System.Messaging;
using log4net;

namespace MightyLittle.MQ.Service.Providers
{
	public abstract class ReceiveProviderBase
	{
		protected readonly ILog Logger;
		protected ReceiveProviderBase()
		{
			Logger = LogManager.GetLogger(GetType());
		}
		public abstract void GetMessageFromQueue(string messageId, MessageQueue queue, Action<Message> processMethod);
	}
}