using gRPC_Service.Utils;

namespace gRPC_Service.Tests;

public class LinearTest
{
    public static Dictionary<string, double> PredictLocation(List<Dictionary<string, object>> trainLocations, string timestamp)
    {
        // Convert timestamp to DateTime
        DateTime targetTime = DateTime.Parse(timestamp);

        // Prepare data for linear regression
        List<double> timestamps = new List<double>();
        List<double> latitudes = new List<double>();
        List<double> longitudes = new List<double>();

        foreach (var location in trainLocations)
        {
            DateTime locationTime = DateTime.Parse((string)location["timestamp"]);

            // Calculate time difference in seconds
            double timeDiff = (targetTime - locationTime).TotalSeconds;
            Console.WriteLine("timeDiff "  + timeDiff);

            timestamps.Add(timeDiff);
            latitudes.Add((double)location["latitude"]);
            longitudes.Add((double)location["longitude"]);
        }

        // Perform linear regression
        double latitude = LinearRegressionModel(latitudes, timestamps);
        double longitude = LinearRegressionModel(longitudes, timestamps);

        // Create and return the predicted location
        Dictionary<string, double> predictedLocation = new Dictionary<string, double>
        {
            { "latitude", latitude },
            { "longitude", longitude }
        };

        return predictedLocation;
    }

    private static double LinearRegressionModel(List<double> y, List<double> x)
    {
        double sumX = 0.0;
        double sumY = 0.0;
        double sumXY = 0.0;
        double sumX2 = 0.0;

        int n = y.Count;

        for (int i = 0; i < n; i++)
        {
            sumX += x[i];
            sumY += y[i];
            sumXY += x[i] * y[i];
            sumX2 += x[i] * x[i];
        }

        double slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
        double intercept = (sumY - slope * sumX) / n;

        return slope * x[n - 1] + intercept; // Predict the value at the last timestamp
    }

    public static void Test1()
    {
        // Sample usage
        List<Dictionary<string, object>> trainLocations = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                { "latitude", 59.4193518 }, { "longitude", 24.7646078 }, { "timestamp", "2022-04-27T10:00:00.000Z" }
            },
            new Dictionary<string, object>
                { { "latitude", 59.4221988 }, { "longitude", 24.77523 }, { "timestamp", "2022-04-27T10:00:02.000Z" } },
            new Dictionary<string, object>
                { { "latitude", 59.4225997 }, { "longitude", 24.776713 }, { "timestamp", "2022-04-27T10:00:04.000Z" } },
            new Dictionary<string, object>
                { { "latitude", 59.4227689 }, { "longitude", 24.777453 }, { "timestamp", "2022-04-27T10:00:06.000Z" } }
        };

        string targetTimestamp = "2022-04-27T10:00:08.000Z";

        Dictionary<string, double> predictedLocation = PredictLocation(trainLocations, targetTimestamp);

        Console.WriteLine(" 1 " + predictedLocation["latitude"]);
        Console.WriteLine(" 1 " + predictedLocation["longitude"]);
    }
    
    
    static Tuple<double, double> PredictLocation(List<TrainLocation> data, double targetTimestamp)
    {
        // Extrapolate the location based on the rate of change between the last two known data points

        // Get the last two known data points
        var secondLastPoint = data[data.Count - 2];
        var lastPoint = data[data.Count - 1];

        // Calculate the time difference and the fraction of time passed
        double timeDiff = lastPoint.Timestamp - secondLastPoint.Timestamp;
        double fraction = (targetTimestamp - lastPoint.Timestamp) / timeDiff;

        // Extrapolate the latitude and longitude
        double latitudeDiff = lastPoint.Latitude - secondLastPoint.Latitude;
        double longitudeDiff = lastPoint.Longitude - secondLastPoint.Longitude;

        double extrapolatedLatitude = lastPoint.Latitude + fraction * latitudeDiff;
        double extrapolatedLongitude = lastPoint.Longitude + fraction * longitudeDiff;

        return Tuple.Create(extrapolatedLatitude, extrapolatedLongitude);
    }
    
    public static void Test2()
    {
        // Sample usage
       var trainLocations = new List<TrainLocation>
        {
            new TrainLocation { Latitude = 59.281008, Longitude = 25.624934, Timestamp = 1683384765 },
            new TrainLocation { Latitude = 59.280712, Longitude = 25.625761, Timestamp = 1683384767 },
            new TrainLocation { Latitude = 59.280427, Longitude = 25.626555, Timestamp = 1683384769 },
            new TrainLocation { Latitude = 59.280112, Longitude = 25.627435, Timestamp = 1683384771 },
            
            // new TrainLocation { Latitude = 59.4193518, Longitude = 24.7646078, Timestamp = 1683384765 },
            // new TrainLocation { Latitude = 59.4221988, Longitude = 24.77523, Timestamp = 1683384767 },
            // new TrainLocation { Latitude = 59.4225997, Longitude = 24.776713, Timestamp = 1683384769 },
            // new TrainLocation { Latitude = 59.4227689, Longitude = 24.777453, Timestamp = 1683384771 },
        };

        var targetTimestamp = trainLocations.Last().Timestamp + 2;
        var predictedLocation = PredictLocation(trainLocations, targetTimestamp);
        Console.WriteLine(" 1 " + predictedLocation.Item1);
        Console.WriteLine(" 1 " + predictedLocation.Item2);
        
        // targetTimestamp = trainLocations.Last().Timestamp + 2;
        // predictedLocation = SimplePredictor.PredictLocation(trainLocations, targetTimestamp);
        // Console.WriteLine(" 1 " + predictedLocation.Item1);
        // Console.WriteLine(" 1 " + predictedLocation.Item2);
    }
}