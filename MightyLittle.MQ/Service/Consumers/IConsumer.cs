using System;
using System.Messaging;

namespace MightyLittle.MQ.Service.Consumers
{
	public interface IConsumer
	{
		Type MessageType { get; }

		void ProcessMessage(Message message);
	}
}