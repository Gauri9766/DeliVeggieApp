using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using MessageModels;

[ApiController]
[Route("[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private static readonly ConcurrentDictionary<string, TaskCompletionSource<Response>> _responses = new();

    public ProductsController()
    {
        var factory = new ConnectionFactory() { HostName = "localhost" };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.QueueDeclare(queue: "responseQueue",
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);

        // Initialize the consumer for the response queue
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var responseJson = Encoding.UTF8.GetString(body);
            var response = JsonConvert.DeserializeObject<Response>(responseJson);

            if (_responses.TryRemove(response.CorrelationId, out var tcs))
            {
                tcs.TrySetResult(response);
            }
        };

        _channel.BasicConsume(queue: "responseQueue",
                             autoAck: true,
                             consumer: consumer);
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] int requestType, [FromQuery] string id)
    {
        var correlationId = Guid.NewGuid().ToString();
        var request = new Request
        {
            RequestType = (RequestType)requestType,
            Id = id.Trim('"'),
            CorrelationId = correlationId
        };

        var requestJson = JsonConvert.SerializeObject(request);
        var body = Encoding.UTF8.GetBytes(requestJson);

        // Send the request
        _channel.BasicPublish(exchange: "",
                             routingKey: "requestQueue", // The queue where the subscriber listens
                             basicProperties: null,
                             body: body);

        // Create and store TaskCompletionSource
        var tcs = new TaskCompletionSource<Response>();
        _responses[correlationId] = tcs;

        // Wait for response with a timeout
        var response = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(30))) == tcs.Task
            ? await tcs.Task
            : null;

        _responses.TryRemove(correlationId, out _); // Cleanup after response

        return response != null ? Ok(response) : NotFound("Product not found");
    }
}

