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

    private static double USER_FORWARD_SPEED_MULLER = 20;
    private static double USER_BACKWARD_SPEED_MULLER = 10;
    private static double USER_SIDE_SPEED_MULLER = 10;
    
    public static Dictionary<string, UserLocationResponse> m_userLocations = new Dictionary<string, UserLocationResponse>();
    // public static Dictionary<string, double> m_userRotations = new Dictionary<string, double>();
    public static Dictionary<string, double> m_userRadius = new Dictionary<string, double>();
    public static Dictionary<string, AutoResetEvent> m_userEvents1 = new Dictionary<string, AutoResetEvent>();
    public static Dictionary<string, AutoResetEvent> m_userEvents2 = new Dictionary<string, AutoResetEvent>();
    public static Dictionary<string, TrainLocation> m_trainLocations = new Dictionary<string, TrainLocation>();
    public static Dictionary<string, List<string>> m_userActiveTrains = new Dictionary<string, List<string>>();

    public static Dictionary<string, AutoResetEvent> m_userEvents3 = new Dictionary<string, AutoResetEvent>();
    public static Dictionary<string, List<string>> m_userCache = new Dictionary<string, List<string>>();
    public static int maxCacheSize = 100;
    
    public static Dictionary<string, List<TrainLocation>> m_trainLocationsCache = new Dictionary<string, List<TrainLocation>>();
    
    private static bool m_bRunTrainThread = false;

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
            //  skip those user that haven't shared rotation with us yet
            // if (m_userRotations.ContainsKey(key)  == false)
            // {
            //     continue;
            // }
            
            var lastLocation = new Utils.LatLng(request.Latitude, request.Longitude);
            var polygon = new List<Utils.LatLng>()
            {
                new Utils.LatLng(m_userLocations[key].Forward.Latitude, m_userLocations[key].Forward.Longitude),
                new Utils.LatLng(m_userLocations[key].Sp1.Latitude, m_userLocations[key].Sp1.Longitude),
                new Utils.LatLng(m_userLocations[key].Backward.Latitude, m_userLocations[key].Backward.Longitude),
                new Utils.LatLng(m_userLocations[key].Sp2.Latitude, m_userLocations[key].Sp2.Longitude),
            };
            
            foreach (var prediction in request.Predictions)
            {
                var currentLocation = new Utils.LatLng(prediction.Latitude, prediction.Longitude);

                var doesIntersect1 = LineIntersectionChecker.DoLinesIntersect(
                    new Utils.LatLng(m_userLocations[key].Location.Latitude, m_userLocations[key].Location.Longitude),
                    new Utils.LatLng(m_userLocations[key].Forward.Latitude, m_userLocations[key].Forward.Longitude),
                    lastLocation,
                    currentLocation
                );
                
                var doesIntersect2 = LineIntersectionChecker.DoLinesIntersect(
                    new Utils.LatLng(m_userLocations[key].Location.Latitude, m_userLocations[key].Location.Longitude),
                    new Utils.LatLng(m_userLocations[key].Backward.Latitude, m_userLocations[key].Backward.Longitude),
                    lastLocation,
                    currentLocation
                );
                
                var doesIntersectTopToLeft = LineIntersectionChecker.DoLinesIntersect(
                    new Utils.LatLng(m_userLocations[key].Forward.Latitude, m_userLocations[key].Forward.Longitude),
                    new Utils.LatLng(m_userLocations[key].Sp1.Latitude, m_userLocations[key].Sp1.Longitude),
                    lastLocation,
                    currentLocation
                );
                
                var doesIntersectLeftToBottom = LineIntersectionChecker.DoLinesIntersect(
                    new Utils.LatLng(m_userLocations[key].Sp1.Latitude, m_userLocations[key].Sp1.Longitude),
                    new Utils.LatLng(m_userLocations[key].Backward.Latitude, m_userLocations[key].Backward.Longitude),
                    lastLocation,
                    currentLocation
                );
                
                var doesIntersectBottomToRight = LineIntersectionChecker.DoLinesIntersect(
                    new Utils.LatLng(m_userLocations[key].Backward.Latitude, m_userLocations[key].Backward.Longitude),
                    new Utils.LatLng(m_userLocations[key].Sp2.Latitude, m_userLocations[key].Sp2.Longitude),
                    lastLocation,
                    currentLocation
                );
                
                var doesIntersectRightToTop = LineIntersectionChecker.DoLinesIntersect(
                    new Utils.LatLng(m_userLocations[key].Sp2.Latitude, m_userLocations[key].Sp2.Longitude),
                    new Utils.LatLng(m_userLocations[key].Forward.Latitude, m_userLocations[key].Forward.Longitude),
                    lastLocation,
                    currentLocation
                );

                var isPointInPolygon = LineIntersectionChecker.IsPointInPolygon(currentLocation, polygon);
                
                if (doesIntersect1 || doesIntersect2 || doesIntersectTopToLeft || doesIntersectLeftToBottom || doesIntersectBottomToRight || doesIntersectRightToTop || isPointInPolygon)
                {
                    m_userEvents1[key].Set();
                    break;
                }

                lastLocation = currentLocation;
            }
            

            // var dist = m_userLocations[key].HaversineDistance(new LatLng
            //     { Latitude = request.Latitude, Longitude = request.Longitude });
            //
            // if (dist < m_userLocations[key].DistanceFromClosestTrain)
            // {
            //     m_userLocations[key].DistanceFromClosestTrain = dist;
            //
            //     var radiusToCheck = 0.010; // in km
            //     if (m_userRadius.ContainsKey(key))
            //     {
            //         radiusToCheck = m_userRadius[key] / 1000.0; // convert meters to km
            //     }
            //     
            //     if (m_userLocations[key].DistanceFromClosestTrain < radiusToCheck) // in km
            //     {
            //         m_userEvents1[key].Set();
            //     }
            // }

            // Console.WriteLine($"UpdateTrainLocation [] {key} dist: {dist} ");

        }

        m_trainLocations[request.TrainId] = request.Clone();
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

    public override Task<UserLocationResponse> UpdateUserLocation(UserLocation request, ServerCallContext context)
    {
        Console.WriteLine($"UpdateUserLocation [] {request.Id} {request.Latitude} {request.Longitude}");

        if (request.AvgSpeed < 0.002)
        {
            request.AvgSpeed = 0.002;
        }
        
        var pointToForward = DestinationPointCalculator.CalculateDestinationPoint(
            new Utils.LatLng(request.Latitude, request.Longitude),
                request.AvgBearing, 
                request.AvgSpeed * (USER_FORWARD_SPEED_MULLER * 1000)
            );
                
        var pointToBackward = DestinationPointCalculator.CalculateDestinationPoint(
                new Utils.LatLng(request.Latitude, request.Longitude),
                request.AvgBearing + 180, 
                request.AvgSpeed * (USER_BACKWARD_SPEED_MULLER * 1000)
            );
        
        var pointSp1 = DestinationPointCalculator.CalculateDestinationPoint(
            new Utils.LatLng(request.Latitude, request.Longitude),
            request.AvgBearing + 90, 
            request.AvgSpeed * (USER_SIDE_SPEED_MULLER * 1000)
        );
        
        var pointSp2 = DestinationPointCalculator.CalculateDestinationPoint(
            new Utils.LatLng(request.Latitude, request.Longitude),
            request.AvgBearing - 90, 
            request.AvgSpeed * (USER_SIDE_SPEED_MULLER * 1000)
        );
        
        var res = new UserLocationResponse
        {
            Location = new PLatLng { Latitude = request.Latitude, Longitude = request.Longitude },
            Forward = new PLatLng { Latitude = pointToForward.Latitude, Longitude = pointToForward.Longitude },
            Backward = new PLatLng { Latitude = pointToBackward.Latitude, Longitude = pointToBackward.Longitude },
            Sp1 = new PLatLng { Latitude = pointSp1.Latitude, Longitude = pointSp1.Longitude },
            Sp2 = new PLatLng { Latitude = pointSp2.Latitude, Longitude = pointSp2.Longitude },
        };

        m_userLocations[request.Id] = res;
        
        return Task.FromResult(res);
    }
    
    public override Task<Response> UpdateUserRotation(UserRotation request, ServerCallContext context)
    {
        Console.WriteLine($"UpdateUserRotation [] {request.Id} {request.Bearing} ignored");
        
        // m_userRotations[request.Id] = request.Bearing;

        // just to test if we get same values as phone app that we use to dbg visually formulas
        // var key = request.Id;
        // if (m_userLocations.ContainsKey(key))
        // {
        //     var pointToForward = DestinationPointCalculator.CalculateDestinationPoint(
        //         new Utils.LatLng(m_userLocations[key].Latitude, m_userLocations[key].Longitude),
        //         m_userRotations[key], 
        //         30
        //     );
        //         
        //     var pointToBackward = DestinationPointCalculator.CalculateDestinationPoint(
        //         new Utils.LatLng(m_userLocations[key].Latitude, m_userLocations[key].Longitude),
        //         m_userRotations[key] + 180, 
        //         10
        //     );
        //
        //     var (sp1, sp2) = TriangleHelper.FindTriangleSidePoints(
        //         new Utils.LatLng(m_userLocations[key].Latitude, m_userLocations[key].Longitude),
        //         pointToForward
        //     );
        //     
        //     Console.WriteLine("pointToForward " + pointToForward.Latitude + " " + pointToForward.Longitude);
        //     Console.WriteLine("pointToBackward " + pointToBackward.Latitude + " " + pointToBackward.Longitude);
        //     Console.WriteLine("sp1 " + sp1.Latitude + " " + sp1.Longitude);
        //     Console.WriteLine("sp2 " + sp2.Latitude + " " + sp2.Longitude);
        // }

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
            
            // m_userLocations[request.Id].ResponseStream = responseStream;

            while (!context.CancellationToken.IsCancellationRequested)
            {
                m_userEvents1[request.Id].WaitOne();
                if (context.CancellationToken.IsCancellationRequested)
                { break; }
                
                await responseStream.WriteAsync(new Response { Code = "IMPACT" });
                // m_userLocations[request.Id].DistanceFromClosestTrain = Double.MaxValue;
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
                        IsAlive = true,
                        Predictions = { it.Predictions }
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
                            // Json = m_userCache[request.Id][0]
                            Json = ""
                        });
                    }
                    catch (Exception ex)
                    {
                        break;
                    }

                    // m_userCache[request.Id].RemoveAt(0);
                    m_userCache[request.Id].Clear();
                }
            }
        }

        m_userEvents3.Remove(request.Id);
        m_userCache.Remove(request.Id);
        
        Console.WriteLine($"SubscribeForTrainLocationUpdates [] {request.Id} ended");

    }
    
    public override Task<Response> IsTrainThreadActive(NoneRequest request, ServerCallContext context)
    {
        Console.WriteLine($"IsTrainThreadActive [] ");
        
        return Task.FromResult(new Response
        {
            Code = m_bRunTrainThread ? "True" : "False"
        });
    }
    
    public override Task<Response> ActivateTrainThread(NoneRequest request, ServerCallContext context)
    {
        Console.WriteLine($"ActivateTrainThread [] ");
        m_bRunTrainThread = true;
        
        return Task.FromResult(new Response
        {
            Code = "OK"
        });
    }
    
    public override Task<Response> DeActivateTrainThread(NoneRequest request, ServerCallContext context)
    {
        Console.WriteLine($"DeActivateTrainThread [] ");
        m_bRunTrainThread = false;

        return Task.FromResult(new Response
        {
            Code = "OK"
        });
    }
    
    
    
    //
    // STATIC FUNCTIONS
    //
    
    public static async void GetData()
    {
        Console.WriteLine($"GetData [] begin");

        var predictor = new TrainLocationPredictor();
        TrainLocationPredictor.g_bDebug = false;
        PointUtils.g_bDebug = false;

        var interval = TimeSpan.FromMilliseconds(2000);
        while (true)
        {
            if (m_userCache.Keys.Count > 0 || m_bRunTrainThread)
            {
                using var client = new HttpClient();
                // var response = await client.GetAsync("https://backend-omega-seven.vercel.app/api/getjoke");
                var response = await client.GetAsync("http://api1.elron.ee/index.php/aktiivsedreisid");
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine("got elron data");

                var isValidJson = IsValidJson(content);
                if (isValidJson)
                {
                    // update train locations
                    var jsonArray = JArray.Parse(content);

                    var timeCheck = DateTime.Now;
                    foreach (JObject jsonObject in jsonArray)
                    {
                        var trainId = (string)jsonObject["reis"];
                        var latitude = (double)jsonObject["latitude"];
                        var longitude = (double)jsonObject["longitude"];
                        var strDt = (string)jsonObject["asukoha_uuendus"];
                        
                        // Convert string to DateTimeOffset object
                        DateTimeOffset? dateTimeOffset = null;
                        try
                        {
                            dateTimeOffset = DateTimeOffset.ParseExact(strDt, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                        }
                        catch (Exception) { }

                        if (dateTimeOffset == null)
                        {
                            try
                            {
                                dateTimeOffset = DateTimeOffset.ParseExact(strDt, "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                            }
                            catch (Exception) { }
                        }

                        if (dateTimeOffset == null)
                        {
                            Console.WriteLine("cannot convert '" + strDt +
                                              "' with  yyyy-MM-dd HH:mm:ss nor yyyy-MM-dd HH:mm:ss.fff");
                            continue;
                            
                            // throw new Exception("cannot convert '" + strDt + "' with  yyyy-MM-dd HH:mm:ss nor yyyy-MM-dd HH:mm:ss.fff");
                        }
                        
                        // Convert DateTimeOffset to Unix timestamp
                        long unixTimestamp = dateTimeOffset?.ToUnixTimeMilliseconds() ?? 0;
                        double timestamp = unixTimestamp;

                        var tLoc = new TrainLocation
                        {
                            TrainId = trainId,
                            Latitude = latitude,
                            Longitude = longitude,
                            Timestamp = timestamp,
                            Predictions = {  }
                        };

                        if (m_trainLocationsCache.ContainsKey(trainId))
                        // if (false)
                        {
                            if (m_trainLocationsCache[trainId].Count > 4)
                            {
                                m_trainLocationsCache[trainId].RemoveAt(0);
                            }
                            m_trainLocationsCache[trainId].Add(tLoc);
                            
                            // try to predict 
                            // if (false)
                            if (m_trainLocationsCache[trainId].Count > 3)
                            {
                                for (double milliseconds = 0; milliseconds < 30000; milliseconds += 3000)
                                {
                                    var (r_lat, r_lon, res) = predictor.PredictLocation2(m_trainLocationsCache[trainId], milliseconds);
                                    
                                    // check  distance if  its  unreal
                                    var distCheck = PointUtils.CalculateDistance(m_trainLocationsCache[trainId].Last().Latitude,
                                        m_trainLocationsCache[trainId].Last().Longitude, r_lat, r_lon);

                                    // distCheck is over 20km something failed
                                    if (distCheck > 20.0 || res == false)
                                    {
                                        break;
                                    }
                                    
                                    tLoc.Predictions.Add(new PLatLng
                                    {
                                        Latitude = r_lat,
                                        Longitude = r_lon,
                                    });
                                }

                                // var trainLocationsToPredictOn = new List<TrainLocation>();
                                // trainLocationsToPredictOn.AddRange(m_trainLocationsCache[trainId]);
                                //
                                // double timeToPredict = 30 * 1000;
                                // var (r_lat2, r_lon2) = predictor.PredictLocation(
                                //     m_trainLocationsCache[trainId], 
                                //     m_trainLocationsCache[trainId].Last().Timestamp + timeToPredict,
                                //     deep: false);
                                // var distanceToTravel = PointUtils.CalculateDistance(r_lat2, r_lon2, m_trainLocationsCache[trainId].Last().Latitude, m_trainLocationsCache[trainId].Last().Longitude);
                                // double totalDistanceTraveledInPrediction = 0;
                                //
                                // double timestampStep = 1000.0;
                                // double ee_timestamp = m_trainLocationsCache[trainId].Last().Timestamp + timestampStep;
                                // double end_timestamp = ee_timestamp + timeToPredict;
                                // for (; ee_timestamp < end_timestamp; ee_timestamp += timestampStep)
                                // {
                                //     // var (r_lat, r_lon) = SimplePredictor.PredictLocation(m_trainLocationsCache[trainId], Convert.ToInt64(ee_timestamp));
                                //     var (r_lat, r_lon) = predictor.PredictLocation(trainLocationsToPredictOn, ee_timestamp);
                                //     
                                //     tLoc.Predictions.Add(new PLatLng
                                //     {
                                //         Latitude = r_lat,
                                //         Longitude = r_lon,
                                //     });
                                //     
                                //     //  check  if prediction can travel that far
                                //     var distancePredicted = PointUtils.CalculateDistance(r_lat, r_lon, trainLocationsToPredictOn.Last().Latitude, trainLocationsToPredictOn.Last().Longitude);
                                //     totalDistanceTraveledInPrediction += distancePredicted;
                                //     if (totalDistanceTraveledInPrediction > distanceToTravel)
                                //     {
                                //         break;
                                //     }
                                //     
                                //     trainLocationsToPredictOn.Add(new TrainLocation
                                //     {
                                //         Latitude = r_lat,
                                //         Longitude = r_lon,
                                //         Timestamp = ee_timestamp
                                //     });
                                // }
                            }
                            
                            UpdateTrainLocationRaw(tLoc);

                        }
                        else
                        {
                            m_trainLocationsCache[trainId] = new List<TrainLocation>() { tLoc };
                            UpdateTrainLocationRaw(tLoc);
                        }
                    }

                    Console.WriteLine("time spent on prediction " + (DateTime.Now - timeCheck).Milliseconds + " ms");
                    
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