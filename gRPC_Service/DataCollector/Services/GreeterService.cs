using Grpc.Core;
using DataCollector;

namespace DataCollector.Services;

public class GreeterService : Greeter.GreeterBase
{
    public static Dictionary<string, AutoResetEvent> m_userEvents1 = new Dictionary<string, AutoResetEvent>();
    public static Dictionary<string, List<string>> m_userCache = new Dictionary<string, List<string>>();
    public static int maxCacheSize = 100;

    private static async void GetData()
    {
        var interval = TimeSpan.FromSeconds(2000);
        while (true)
        {
            if (m_userCache.Keys.Count > 0)
            {
                using var client = new HttpClient();
                var response = await client.GetAsync("https://backend-omega-seven.vercel.app/api/getjoke");
                var content = await response.Content.ReadAsStringAsync();

                foreach (var key in m_userCache.Keys)
                {
                    if (m_userCache[key].Count > maxCacheSize)
                    {
                        m_userCache[key].RemoveAt(0);
                    }
                
                    m_userCache[key].Add(content);
                }
            
                // notify to stream
                foreach (var key in m_userEvents1.Keys)
                {
                    m_userEvents1[key].Set();
                }
            }
            
            await Task.Delay(interval);
        }

    }
    
    private readonly ILogger<GreeterService> _logger;
    

    public GreeterService(ILogger<GreeterService> logger)
    {
        _logger = logger;
    }

    public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
        return Task.FromResult(new HelloReply
        {
            Message = "Hello " + request.Name
        });
    }
    
    public override async Task SubscribeForDataCollector(SubscribeRequest request, IServerStreamWriter<DataResponse> responseStream, ServerCallContext context)
    {
        Console.WriteLine($"SubscribeForDataCollector [] {request.Id} ");

        if (false == m_userEvents1.ContainsKey(request.Id))
        { m_userEvents1[request.Id] = new AutoResetEvent(false); }

        m_userCache[request.Id] = new List<string>();
        
        while (!context.CancellationToken.IsCancellationRequested)
        {
            m_userEvents1[request.Id].WaitOne();
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

        m_userEvents1.Remove(request.Id);
        m_userCache.Remove(request.Id);
        
        Console.WriteLine($"SubscribeForTrainLocationUpdates [] {request.Id} ended");

    }
}