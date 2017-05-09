using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RPCServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var factory = new ConnectionFactory() { HostName = "192.168.0.101" };
            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(queue: "rpc_queue", 
                        durable: false,
                        exclusive: false, 
                        autoDelete: false, 
                        arguments: null);

                    channel.BasicQos(0, 1, false);

                    var consumer = new EventingBasicConsumer(channel);

                    channel.BasicConsume(queue: "rpc_queue",
                        noAck: false, 
                        consumer: consumer);

                    Console.WriteLine(" [x] Awaiting RPC requests");

                    consumer.Received += (model, ea) =>
                    {
                        string response = null;

                        var body = ea.Body;
                        var props = ea.BasicProperties;
                        // ReSharper disable once AccessToDisposedClosure
                        var replyProps = channel.CreateBasicProperties();
                        replyProps.CorrelationId = props.CorrelationId;

                        try
                        {
                            var message = Encoding.UTF8.GetString(body);
                            var n = int.Parse(message);
                            Console.WriteLine(" [.] fib({0})", message);
                            response = Fib(n).ToString();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(" [.] " + e.Message);
                            response = "";
                        }
                        finally
                        {
                            var responseBytes = Encoding.UTF8.GetBytes(response);
                            // ReSharper disable once AccessToDisposedClosure
                            channel.BasicPublish(exchange: "", 
                                routingKey: props.ReplyTo,
                                basicProperties: replyProps, 
                                body: responseBytes);
                            // ReSharper disable once AccessToDisposedClosure
                            channel.BasicAck(deliveryTag: ea.DeliveryTag,
                                multiple: false);
                        }
                    };

                    Console.WriteLine(" Press [enter] to exit.");
                    Console.ReadLine();
                }
            }
        }

        private static int Fib(int n)
        {
            if (n == 0 || n == 1)
            {
                return n;
            }

            return Fib(n - 1) + Fib(n - 2);
        }
    }
}