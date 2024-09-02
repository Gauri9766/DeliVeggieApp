using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MessageModels;
public class Program
{
    public static async Task Main(string[] args)
    {
        var factory = new ConnectionFactory() { HostName = "localhost" };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        // Declare the queues
        channel.QueueDeclare(queue: "requestQueue",
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);

        channel.QueueDeclare(queue: "responseQueue",
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var requestJson = Encoding.UTF8.GetString(body);
            var request = JsonConvert.DeserializeObject<Request>(requestJson);

            await HandleRequest(channel, request);
        };

        channel.BasicConsume(queue: "requestQueue",
                             autoAck: true,
                             consumer: consumer);

        Console.WriteLine("Subscriber is listening...");
        Console.ReadLine();
    }

    private static async Task HandleRequest(IModel channel, Request request)
    {
        // MongoDB setup
        var client = new MongoClient("mongodb://localhost:27017");
        var database = client.GetDatabase("DeliVeggieDb");
        var collection = database.GetCollection<Product>("Products");

        Response response = null;

        if (request.RequestType == RequestType.GetAll)
        {
            List<ProductResponse> productResponseList = new();
            List<Product> products = await collection.Find(new BsonDocument()).ToListAsync();
            foreach(Product product in products)
            {
                var productResponse = new ProductResponse() { ProductId = product.ProductId, Name = product.Name, EntryDate = product.EntryDate, PriceForToday = product.PriceForToday };
                productResponseList.Add(productResponse);
            }

            response = new Response
            {
                CorrelationId = request.CorrelationId,
                ProductsList = productResponseList
            };
        }
        else
        {
            var filter = Builders<Product>.Filter.Eq(p => p.ProductId, request.Id);
            var product = await collection.Find(filter).FirstOrDefaultAsync();
            var productResponse = new ProductResponse() { ProductId = product.ProductId, Name = product.Name, EntryDate = product.EntryDate, PriceForToday = product.PriceForToday };

            if (product != null)
            {
                response = new Response
                {
                    CorrelationId = request.CorrelationId,
                    ProductsList = new List<ProductResponse> { productResponse }
                };
            }
            else
            {
                // Optionally handle cases where the product is not found
            }
        }

        if (response != null)
        {
            var responseJson = JsonConvert.SerializeObject(response);
            var body = Encoding.UTF8.GetBytes(responseJson);

            channel.BasicPublish(exchange: "",
                                 routingKey: "responseQueue",
                                 basicProperties: null,
                                 body: body);
        }
    }
}


