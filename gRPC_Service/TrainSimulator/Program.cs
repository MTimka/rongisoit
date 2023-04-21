// See https://aka.ms/new-console-template for more information

using gRPC_Service;
using Grpc.Net.Client;
using TrainSimulator;

Console.WriteLine("Hello, World!");

using var channel = GrpcChannel.ForAddress("http://192.168.8.100:5001");
var client = new Greeter.GreeterClient(channel);

// var startingPos = new LatLng { Latitude = 50.0, Longitude = 50.0 };
// var endingPos = new LatLng { Latitude = 10.0, Longitude = 5.0 };  // 10, 5 will impact

var startingPos = new LatLng { Latitude = 59.352107, Longitude = 24.907485 };
var endingPos = new LatLng { Latitude = 59.351100, Longitude = 24.907665 };

// var reply = client.UpdateTrainLocation(new TrainLocation { Latitude = startingPos.Latitude, Longitude = startingPos.Longitude });
// Console.WriteLine("reply: " + reply.Code);

LatLng MakeStep(LatLng from)
{
    // Calculate bearing from startingPos to endingPos
    // var dLat = (endingPos.Latitude - startingPos.Latitude) * Math.PI / 180;
    var dLon = (endingPos.Longitude - from.Longitude) * Math.PI / 180;
    var y = Math.Sin(dLon) * Math.Cos(endingPos.Latitude * Math.PI / 180);
    var x = Math.Cos(from.Latitude * Math.PI / 180) * Math.Sin(endingPos.Latitude * Math.PI / 180) - Math.Sin(from.Latitude * Math.PI / 180) * Math.Cos(endingPos.Latitude * Math.PI / 180) * Math.Cos(dLon);
    var bearing = Math.Atan2(y, x) * 180 / Math.PI;

    var distance = 0.005; // in km
    var R = 6371.0; // Earth's radius in kilometers
    var lat1 = from.Latitude * Math.PI / 180;
    var lon1 = from.Longitude * Math.PI / 180;
    var lat2 = Math.Asin(Math.Sin(lat1) * Math.Cos(distance / R) + Math.Cos(lat1) * Math.Sin(distance / R) * Math.Cos(bearing * Math.PI / 180));
    var lon2 = lon1 + Math.Atan2(Math.Sin(bearing * Math.PI / 180) * Math.Sin(distance / R) * Math.Cos(lat1), Math.Cos(distance / R) - Math.Sin(lat1) * Math.Sin(lat2));
    var newLatLng = new LatLng { Latitude = lat2 * 180 / Math.PI, Longitude = lon2 * 180 / Math.PI };

    return newLatLng;
}

var interval = TimeSpan.FromMilliseconds(200);

var lastDist = startingPos.HaversineDistance(endingPos);
for (var pos = startingPos; ; pos = MakeStep(pos))
{
    var reply2 = client.UpdateTrainLocation(new TrainLocation
    {
        TrainId = "train1", 
        Latitude = pos.Latitude,
        Longitude = pos.Longitude
    });
    
    Console.WriteLine("reply: " + reply2.Code);
    
    var dist = pos.HaversineDistance(endingPos);
    Console.WriteLine("dist: " + dist);

    if (dist < 0.001 || dist > lastDist) 
    { break; }

    lastDist = dist;
    
    await Task.Delay(interval);
}

client.RemoveActiveTrain(new RemoveActiveTrainRequest()
{
    TrainId = "train1",
});
