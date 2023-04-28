using System.Globalization;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using gRPC_Service;
using gRPC_Service.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace gRPC_Service.Services;

public class GreeterService : Greeter.GreeterBase
{
    private readonly ILogger<GreeterService> _logger;

    public static Dictionary<string, LatLng> m_userLocations = new Dictionary<string, LatLng>();
    public static Dictionary<string, double> m_userRadius = new Dictionary<string, double>();
    public static Dictionary<string, AutoResetEvent> m_userEvents1 = new Dictionary<string, AutoResetEvent>();
    public static Dictionary<string, AutoResetEvent> m_userEvents2 = new Dictionary<string, AutoResetEvent>();
    public static Dictionary<string, LatLng> m_trainLocations = new Dictionary<string, LatLng>();
    public static Dictionary<string, List<string>> m_userActiveTrains = new Dictionary<string, List<string>>();

    public static Dictionary<string, AutoResetEvent> m_userEvents3 = new Dictionary<string, AutoResetEvent>();
    public static Dictionary<string, List<string>> m_userCache = new Dictionary<string, List<string>>();
    public static int maxCacheSize = 100;
    
    public static Dictionary<string, List<TrainLocation>> m_trainLocationsCache = new Dictionary<string, List<TrainLocation>>();
    

    public GreeterService(ILogger<GreeterService> logger)
    {
        _logger = logger;
    }

    public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
        Console.WriteLine($"SayHello [] {request} ");

        return Task.FromResult(new HelloReply
        {
            Message = "Hello " + request.Name
        });
    }

    public override async Task StartTimer(StartTimerRequest request, IServerStreamWriter<PeriodicUpdate> responseStream,
        ServerCallContext context)
    {
        var interval = TimeSpan.FromSeconds(request.IntervalSeconds);
        while (!context.CancellationToken.IsCancellationRequested)
        {
            // Wait for the specified interval
            await Task.Delay(interval, context.CancellationToken);

            var unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var timestamp = (long)(DateTime.UtcNow - unixEpoch).TotalMilliseconds;

            // Send a periodic update to the client
            var update = new PeriodicUpdate { Timestamp = timestamp };
            await responseStream.WriteAsync(update);
        }
    }

    public static void UpdateTrainLocationRaw(TrainLocation request)
    {
        // calculate impact distance for each user
        foreach (var key in m_userLocations.Keys)
        {
            var dist = m_userLocations[key].HaversineDistance(new LatLng
                { Latitude = request.Latitude, Longitude = request.Longitude });

            if (dist < m_userLocations[key].DistanceFromClosestTrain)
            {
                m_userLocations[key].DistanceFromClosestTrain = dist;

                var radiusToCheck = 0.010; // in km
                if (m_userRadius.ContainsKey(key))
                {
                    radiusToCheck = m_userRadius[key] / 1000.0; // convert meters to km
                }
                
                if (m_userLocations[key].DistanceFromClosestTrain < radiusToCheck) // in km
                {
                    m_userEvents1[key].Set();
                }
            }

            // Console.WriteLine($"UpdateTrainLocation [] {key} dist: {dist} ");

        }

        m_trainLocations[request.TrainId] = new LatLng { Latitude = request.Latitude, Longitude = request.Longitude };
    }
    
    public override Task<Response> UpdateTrainLocation(TrainLocation request, ServerCallContext context)
    {
        Console.WriteLine($"UpdateTrainLocation [] {request.Latitude} {request.Longitude}");
        
        UpdateTrainLocationRaw(request);
        
        // tell users to update locations
        foreach (var it in m_userEvents2)
        { it.Value.Set(); }

        return Task.FromResult(new Response
        {
            Code = "OK"
        });
    }

    public override Task<Response> RemoveActiveTrain(RemoveActiveTrainRequest request, ServerCallContext context)
    {
        if (m_trainLocations.ContainsKey(request.TrainId))
        {
            m_trainLocations.Remove(request.TrainId);
        }
        
        // tell users to update locations
        foreach (var it in m_userEvents2)
        { it.Value.Set(); }
        
        return Task.FromResult(new Response
        {
            Code = "OK"
        });
    }

    public override Task<Response> UpdateUserLocation(UserLocation request, ServerCallContext context)
    {
        Console.WriteLine($"UpdateUserLocation [] {request.Id} {request.Latitude} {request.Longitude}");
        
        m_userLocations[request.Id] = new LatLng { Latitude = request.Latitude, Longitude = request.Longitude };

        return Task.FromResult(new Response
        {
            Code = "OK"
        });
    }
    
    public override Task<Response> UpdateUserImpactRadius(UserImpactRadiusRequest request, ServerCallContext context)
    {
        Console.WriteLine($"UpdateUserImpactRadius [] {request.Id} {request.Radius}");
        
        m_userRadius[request.Id] = request.Radius;

        return Task.FromResult(new Response
        {
            Code = "OK"
        });
    }
    
    public override async Task SubscribeForImpact(SubscribeRequest request, IServerStreamWriter<Response> responseStream, ServerCallContext context)
    {
        var interval = TimeSpan.FromMilliseconds(100);

        while (!context.CancellationToken.IsCancellationRequested)
        {
            if (m_userLocations.ContainsKey(request.Id))
            {
                Console.WriteLine($"SubscribeForImpact [] got key {request.Id} ");
                break;
            }
            await Task.Delay(interval, context.CancellationToken);
        }
        
        if (m_userLocations.ContainsKey(request.Id))
        {
            if (false == m_userEvents1.ContainsKey(request.Id))
            { m_userEvents1[request.Id] = new AutoResetEvent(false); }
            
            m_userLocations[request.Id].ResponseStream = responseStream;

            while (!context.CancellationToken.IsCancellationRequested)
            {
                m_userEvents1[request.Id].WaitOne();
                if (context.CancellationToken.IsCancellationRequested)
                { break; }
                
                await responseStream.WriteAsync(new Response { Code = "IMPACT" });
                m_userLocations[request.Id].DistanceFromClosestTrain = Double.MaxValue;
                Console.WriteLine($"SubscribeForImpact [] {request.Id} will impact");
            }
        }
        else
        {
            Console.WriteLine($"SubscribeForImpact [] no key for {request.Id} ");
        }
        
        // m_userEvents1.Remove(request.Id);

        Console.WriteLine($"SubscribeForImpact [] {request.Id} ended");

    }
    
    
    public override async Task SubscribeForTrainLocationUpdates(SubscribeRequest request, IServerStreamWriter<TrainLocationUpdatesResponse> responseStream, ServerCallContext context)
    {
        Console.WriteLine($"SubscribeForTrainLocationUpdates [] {request.Id} ");

        if (false == m_userEvents2.ContainsKey(request.Id))
        { m_userEvents2[request.Id] = new AutoResetEvent(false); }
        
        while (!context.CancellationToken.IsCancellationRequested)
        {
            m_userEvents2[request.Id].WaitOne();
            if (context.CancellationToken.IsCancellationRequested)
            { break; }

            // tell user to remove orphaned trains
            if (m_userActiveTrains.ContainsKey(request.Id))
            {
                foreach (var it in m_userActiveTrains[request.Id]
                             .Where(x => false == m_trainLocations.Keys.Contains(x)))
                {
                    await responseStream.WriteAsync(new TrainLocationUpdatesResponse
                    {
                        TrainId = it,
                        Latitude = 0.0,
                        Longitude = 0.0,
                        IsAlive = false
                    });

                }
            }
            

            // and set active locations and store cache to compare for orphaned
            var lastTrainIds = new List<string>();
            var keyList = m_trainLocations.Keys.ToList(); // to be thread safe
            
            for (var i = 0; i < keyList.Count; i++)
            {
                var key = keyList[i];
                if (m_trainLocations.ContainsKey(key))
                {
                    lastTrainIds.Add(key);
                    var it = m_trainLocations[key];
                
                    await responseStream.WriteAsync(new TrainLocationUpdatesResponse
                    {
                        TrainId = key,
                        Latitude = it.Latitude,
                        Longitude = it.Longitude,
                        IsAlive = true
                    });
                }
            }

            m_userActiveTrains[request.Id] = lastTrainIds;

        }

        // m_userEvents2.Remove(request.Id);
        // m_userActiveTrains.Remove(request.Id);
        
        Console.WriteLine($"SubscribeForTrainLocationUpdates [] {request.Id} ended");

    }
    
    
    
    //
    // DATA COLLECTING
    //
    
    public override async Task SubscribeForDataCollector(SubscribeRequest request, IServerStreamWriter<DataResponse> responseStream, ServerCallContext context)
    {
        Console.WriteLine($"SubscribeForDataCollector [] {request.Id} ");

        if (false == m_userEvents3.ContainsKey(request.Id))
        { m_userEvents3[request.Id] = new AutoResetEvent(false); }

        m_userCache[request.Id] = new List<string>();
        
        while (!context.CancellationToken.IsCancellationRequested)
        {
            m_userEvents3[request.Id].WaitOne();
            if (context.CancellationToken.IsCancellationRequested)
            { break; }

            Console.WriteLine($"SubscribeForTrainLocationUpdates [] {request.Id} send");

            // send user the json
            if (m_userCache.ContainsKey(request.Id))
            {
                while (m_userCache[request.Id].Count > 0)
                {
                    try
                    {
                        await responseStream.WriteAsync(new DataResponse
                        {
                            Json = m_userCache[request.Id][0]
                        });
                    }
                    catch (Exception ex)
                    {
                        break;
                    }

                    m_userCache[request.Id].RemoveAt(0);
                }
            }
        }

        m_userEvents3.Remove(request.Id);
        m_userCache.Remove(request.Id);
        
        Console.WriteLine($"SubscribeForTrainLocationUpdates [] {request.Id} ended");

    }
    
    
    
    //
    // STATIC FUNCTIONS
    //
    
    public static async void GetData()
    {
        Console.WriteLine($"GetData [] begin");

        // var predictor = new TrainLocationPredictor();

        var interval = TimeSpan.FromMilliseconds(2000);
        while (true)
        {
            if (m_userCache.Keys.Count > 0)
            {
                using var client = new HttpClient();
                // var response = await client.GetAsync("https://backend-omega-seven.vercel.app/api/getjoke");
                var response = await client.GetAsync("http://api1.elron.ee/index.php/aktiivsedreisid");
                var content = await response.Content.ReadAsStringAsync();

                var isValidJson = IsValidJson(content);
                if (isValidJson)
                {
                    // update train locations
                    var jsonArray = JArray.Parse(content);
                    foreach (JObject jsonObject in jsonArray)
                    {
                        var trainId = (string)jsonObject["reis"];
                        var latitude = (double)jsonObject["latitude"];
                        var longitude = (double)jsonObject["longitude"];
                        var strDt = (string)jsonObject["asukoha_uuendus"];
                        
                        // Convert string to DateTimeOffset object
                        DateTimeOffset dateTimeOffset = DateTimeOffset.ParseExact(strDt, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

                        // Convert DateTimeOffset to Unix timestamp
                        long unixTimestamp = dateTimeOffset.ToUnixTimeMilliseconds();
                        double timestamp = unixTimestamp / 1000.0;

                        var tLoc = new TrainLocation
                        {
                            TrainId = trainId,
                            Latitude = latitude,
                            Longitude = longitude,
                            Timestamp = timestamp,
                        };

                        if (m_trainLocationsCache.ContainsKey(trainId))
                        {
                            if (m_trainLocationsCache[trainId].Count > 6)
                            {
                                m_trainLocationsCache[trainId].RemoveAt(0);
                            }
                            m_trainLocationsCache[trainId].Add(tLoc);
                            
                            // try to predict 
                            if (m_trainLocationsCache[trainId].Count > 4)
                            {
                                // var tLocsMapped = m_trainLocationsCache[trainId]
                                    // .Select(x => Tuple.Create(x.Latitude, x.Longitude, x.Timestamp)).ToList();

                                // Get the time zone info for Estonia
                                TimeZoneInfo estoniaTimeZone =
                                    TimeZoneInfo.FindSystemTimeZoneById("E. Europe Standard Time");

                                // Get the current date and time in the Estonian time zone
                                DateTimeOffset estoniaTime =
                                    TimeZoneInfo.ConvertTime(DateTimeOffset.Now, estoniaTimeZone);

                                // Convert DateTimeOffset to Unix timestamp
                                long ee_unixTimestamp = estoniaTime.ToUnixTimeMilliseconds();
                                double ee_timestamp = ee_unixTimestamp / 1000.0;

                                var lpm = new LocationPredictionModel(m_trainLocationsCache[trainId]);
                                var res = lpm.PredictNextLocation(ee_timestamp);
                                // var res = TrainLocationPredictor.PredictTrainLocationAtTimestamp(tLocsMapped, ee_timestamp);

                                var tLocU = new TrainLocation
                                {
                                    TrainId = trainId + "_pres",
                                    Latitude = res.Latitude,
                                    Longitude = res.Longitude,
                                    Timestamp = ee_timestamp,
                                };
                                
                                UpdateTrainLocationRaw(tLocU);
                            }

                            UpdateTrainLocationRaw(tLoc);

                        }
                        else
                        {
                            m_trainLocationsCache[trainId] = new List<TrainLocation>() { tLoc };
                            UpdateTrainLocationRaw(tLoc);
                        }
                    }
                    
                    // tell users to update locations
                    foreach (var it in m_userEvents2)
                    { it.Value.Set(); }
                
                    // write to users cache
                    foreach (var key in m_userCache.Keys)
                    {
                        if (m_userCache[key].Count > maxCacheSize)
                        {
                            m_userCache[key].RemoveAt(0);
                        }
                
                        m_userCache[key].Add(content);
                    }
            
                    // notify to stream
                    foreach (var key in m_userEvents3.Keys)
                    {
                        m_userEvents3[key].Set();
                    }
                }
            }
            
            await Task.Delay(interval);
        }

    }
    
    private static bool IsValidJson(string strInput)
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

}