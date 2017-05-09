using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;

namespace RPCClient
{
    class RpcClient
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly string _replyQueueName;
        private readonly QueueingBasicConsumer _consumer;

        public RpcClient()
        {
            var factory = new ConnectionFactory() { HostName = "192.168.0.101" };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _replyQueueName = _channel.QueueDeclare().QueueName;
            _consumer = new QueueingBasicConsumer(_channel);
            _channel.BasicConsume(queue: _replyQueueName,
                noAck: true,
                consumer: _consumer);
        }

        public string Call(string message)
        {
            var corrId = Guid.NewGuid().ToString();
            var props = _channel.CreateBasicProperties();
            props.ReplyTo = _replyQueueName;
            props.CorrelationId = corrId;

            var messageBytes = Encoding.UTF8.GetBytes(message);
            _channel.BasicPublish(exchange: "",
                routingKey: "rpc_queue",
                basicProperties: props,
                body: messageBytes);

            while (true)
            {
                var ea = (BasicDeliverEventArgs)_consumer.Queue.Dequeue();
                if (ea.BasicProperties.CorrelationId == corrId)
                {
                    return Encoding.UTF8.GetString(ea.Body);
                }
            }
        }

        public void Close()
        {
            _connection.Close();
        }
    }
}
