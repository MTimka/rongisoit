// See https://aka.ms/new-console-template for more information

using ConsoleApp1;
using gRPC_Service;
using Grpc.Net.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

Console.WriteLine("Hello, World!");

using var channnel = GrpcChannel.ForAddress("http://13.50.101.103:5001");
// using var channnel = GrpcChannel.ForAddress("http://192.168.8.100:5001");
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

static bool IsValidJson(string strInput)
{
    if (string.IsNullOrWhiteSpace(strInput)) { return false;}
    strInput = strInput.Trim();
    if ((strInput.StartsWith("{") && strInput.EndsWith("}")) || //For object
        (strInput.StartsWith("[") && strInput.EndsWith("]"))) //For array
    {
        try
        {
            var obj = JToken.Parse(strInput);
            return true;
        }
        catch (JsonReaderException jex)
        {
            //Exception in parsing json
            Console.WriteLine(jex.Message);
            return false;
        }
        catch (Exception ex) //some other exception
        {
            Console.WriteLine(ex.ToString());
            return false;
        }
    }
    else
    {
        return false;
    }
}

async void ListenServer() {
    var servElron = new ServElron("Host=localhost;Username=postgres;Password=123;Database=rongisoit");

    
    using (var call = client.SubscribeForDataCollector(new SubscribeRequest { Id = "test1" }))
    {
        var cancellationToken = new CancellationToken();
        var responseStream = call.ResponseStream;
        while (await responseStream.MoveNext(cancellationToken))
        {
            var update = responseStream.Current;
            // Do something with the update from the server
            Console.WriteLine($"Update received: {update}");
            
            var isValidJson = IsValidJson(update.Json);
            if (isValidJson)
            {
                // update train locations
                var jsonArray = JArray.Parse(update.Json);
                foreach (JObject jsonObject in jsonArray)
                {
                    var reis = (string)jsonObject["reis"];
                    var liin = (string)jsonObject["liin"];
                    var reisi_algus_aeg = (string)jsonObject["reisi_algus_aeg"];
                    var reisi_lopp_aeg = (string)jsonObject["reisi_lopp_aeg"];
                    var kiirus = (string)jsonObject["kiirus"];
                    var latitude = (string)jsonObject["latitude"];
                    var longitude = (string)jsonObject["longitude"];
                    var rongi_suund = (string)jsonObject["rongi_suund"];
                    var erinevus_plaanist = (string)jsonObject["erinevus_plaanist"];
                    var lisateade = (string)jsonObject["lisateade"];
                    var pohjus_teade = (string)jsonObject["pohjus_teade"];
                    var avalda_kodulehel = (string)jsonObject["avalda_kodulehel"];
                    var asukoha_uuendus = (string)jsonObject["asukoha_uuendus"];
                    var reisi_staatus = (string)jsonObject["reisi_staatus"];
                    var viimane_peatus = (string)jsonObject["viimane_peatus"];

                    servElron.InsertServElron(
                        reis: reis,
                        liin: liin,
                        reisiAlgusAeg: reisi_algus_aeg,
                        reisiLoppAeg: reisi_lopp_aeg,
                        kiirus: kiirus,
                        latitude: latitude,
                        longitude: longitude,
                        rongiSuund: rongi_suund,
                        erinevusPlaanist: erinevus_plaanist,
                        lisateade: lisateade,
                        pohjusTeade: pohjus_teade,
                        avaldaKodulehel: avalda_kodulehel,
                        asukohaUuendus: asukoha_uuendus,
                        reisiStaatus: reisi_staatus,
                        viimanePeatus: viimane_peatus
                    );

                }
                
            }
        }
    }
    
    Console.WriteLine($"ListenServer ended");
}

ListenServer();

Console.WriteLine("on pause");
Console.ReadKey();


