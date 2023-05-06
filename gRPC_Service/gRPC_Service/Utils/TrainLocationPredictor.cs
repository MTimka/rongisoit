using System.Globalization;

namespace gRPC_Service.Utils;

using System;


public class TrainLocationPredictor
{
    private List<List<Tuple<double, double>>> railways;
    private List<List<dynamic>> tracks;
    
    public TrainLocationPredictor()
    {
        railways = new List<List<Tuple<double, double>>>();
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
        
        tracks = new List<List<dynamic>>();
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
    }

    public Tuple<double, double> Predict(List<TrainLocation> trainLocations, long timestamp)
    {
        var lastLoc = new
        {
            Latitude = trainLocations.Last().Latitude,
            Longitude = trainLocations.Last().Longitude
        };
        
        var (closestTrack, closest_track_index) = PointUtils.GetClosestTract(lastLoc, railways);
        
        var trainLocationsC = new List<dynamic>();
        foreach (var item in trainLocations)
        {
            var res = new
            {
                Latitude = item.Latitude,
                Longitude = item.Longitude,
                timestamp = Convert.ToInt64(item.Timestamp)
            };
            
            trainLocationsC.Add(res);
        }

        var estimatedLocation =
            PointUtils.TrackWalker(tracks, trainLocationsC, closest_track_index, timestamp);

        return estimatedLocation;
    }
    
}

