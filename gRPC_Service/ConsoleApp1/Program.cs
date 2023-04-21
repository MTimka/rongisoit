// See https://aka.ms/new-console-template for more information

using gRPC_Service;
using Grpc.Net.Client;

Console.WriteLine("Hello, World!");

using var channnel = GrpcChannel.ForAddress("http://192.168.8.100:5001");
var client = new Greeter.GreeterClient(channnel);
var reply = client.SayHello(new HelloRequest { Name = "mambo" });
Console.WriteLine("reply: " + reply.Message);

// async void ListenServer() {
//     using (var call = client.SubscribeForImpact(new SubscribeRequest { Id = "test1" }))
//     {
//         var cancellationToken = new CancellationToken();
//         var responseStream = call.ResponseStream;
//         while (await responseStream.MoveNext(cancellationToken))
//         {
//             var update = responseStream.Current;
//             // Do something with the update from the server
//             Console.WriteLine($"Update received: {update}");
//         }
//     }
//     
//     Console.WriteLine($"ListenServer ended");
// }
//
// var reply2 = client.UpdateUserLocation(new UserLocation { Id = "test1", Latitude = 14.16, Longitude = 7.90 });
// Console.WriteLine("reply: " + reply2.Code);
//
// ListenServer();


async void ListenServer() {
    using (var call = client.SubscribeForDataCollector(new SubscribeRequest { Id = "test1" }))
    {
        var cancellationToken = new CancellationToken();
        var responseStream = call.ResponseStream;
        while (await responseStream.MoveNext(cancellationToken))
        {
            var update = responseStream.Current;
            // Do something with the update from the server
            Console.WriteLine($"Update received: {update}");
        }
    }
    
    Console.WriteLine($"ListenServer ended");
}

ListenServer();

Console.WriteLine("on pause");
Console.ReadKey();


