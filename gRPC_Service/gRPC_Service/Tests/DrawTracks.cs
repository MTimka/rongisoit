using System.Globalization;
using gRPC_Service.Utils;

namespace gRPC_Service.Tests;

using System;
using System.Drawing;
using ScottPlot;

public class DrawTracks
{
    public static void Test1()
    {
        var railways = new List<List<Tuple<double, double>>>();
        string filePath = "train_tracks.data";
        
        // Open the file for reading
        using (StreamReader reader = new StreamReader(filePath))
        {
            string line;
            int lineCount = 0;
            
            // Read and process each line until the end of the file is reached
            while ((line = reader.ReadLine()) != null)
            {
                lineCount += 1;

                // Process the line
                var splits = line.Split(" ");
                
                if (splits.Length < 4)
                {
                    continue;
                }

                var track = new List<Tuple<double, double>>();
                // Console.WriteLine("line " + lineCount);
                for (var i = 0; i < splits.Length; i += 2)
                {
                    var item = Tuple.Create(
                        Convert.ToDouble(splits[i], CultureInfo.InvariantCulture), 
                        Convert.ToDouble(splits[i+1], CultureInfo.InvariantCulture)
                    );
                    track.Add(item);
                }
                
                railways.Add(track);
            }
        }

        Console.WriteLine("railways len " + railways.Count);

        var locations = new List<Dictionary<string, object>>()
        {
            new Dictionary<string, object>()
            {
                { "latitude", 59.42015800 },
                { "longitude", 24.76765300 },
                { "timestamp", "2022-04-27T10:00:00.000Z" }
            },
            new Dictionary<string, object>()
            {
                { "latitude", 59.42194400 },
                { "longitude", 24.77431600 },
                { "timestamp", "2022-04-27T10:00:02.000Z" }
            },
            new Dictionary<string, object>()
            {
                { "latitude", 59.42261700 },
                { "longitude", 24.77685800 },
                { "timestamp", "2022-04-27T10:00:04.000Z" }
            }
        };

        var loc = new
        {
            Latitude = locations.Last()["latitude"],
            Longitude = locations.Last()["longitude"]
        };
        var (closestTrack, closest_track_index) = PointUtils.GetClosestTract(loc, railways);
        Console.WriteLine("closest_track_index " + closest_track_index);

        var tracks = new List<List<dynamic>>();
        foreach (List<Tuple<double, double>> track in railways)
        {
            var res = new List<dynamic>();
            foreach (Tuple<double, double> point in track)
            {
                var pointDict = new 
                {
                    Latitude = point.Item1,
                    Longitude = point.Item2
                };
                res.Add(pointDict);
            }
            tracks.Add(res);
        }
        
        var trainLocations = new List<dynamic>();
        foreach (Dictionary<string, object> item in locations)
        {
            var res = new
            {
                Latitude = item["latitude"],
                Longitude = item["longitude"],
                timestamp = DateTimeOffset.ParseExact(item["timestamp"] as string, "yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture).ToUnixTimeSeconds()
            };
            
            trainLocations.Add(res);
        }

        long lTimestamp = DateTimeOffset
            .ParseExact("2022-04-27T10:00:18.000Z", "yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture)
            .ToUnixTimeSeconds();
        
        var estimatedLocation =
            PointUtils.TrackWalker(tracks, trainLocations, closest_track_index, lTimestamp);
        Console.WriteLine("estimatedLocation " + estimatedLocation.Item1 + " " + estimatedLocation.Item2);


        // // Generate random colors for each track
        // string[] colors = new string[railways.Count];
        // Random random = new Random();
        // for (int i = 0; i < railways.Count; i++)
        // {
        //     colors[i] = "#" + random.Next(0x1000000).ToString("X6");
        // }
        //
        // var plt = new Plot(600, 400);
        //
        // for (int i = 0; i < railways.Count; i++)
        // {
        //     string color = colors[i];
        //     double[] xData = railways[i].Select(x => x.Item2).ToArray();
        //     double[] yData = railways[i].Select(x => x.Item1).ToArray();
        //     plt.PlotScatter(xData, yData, markerSize: 0, color: ColorTranslator.FromHtml(color));
        //     // plt.PlotLines(xData, yData, lineWidth: 1, color: ColorTranslator.FromHtml(color));
        // }
        //
        // plt.Title("Rail Tracks");
        // plt.XLabel("Longitude");
        // plt.YLabel("Latitude");
        // plt.SaveFig("rail_tracks.png");

    }
}