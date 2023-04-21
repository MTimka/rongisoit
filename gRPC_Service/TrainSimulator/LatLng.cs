namespace TrainSimulator;

public class LatLng
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public double GetDistFrom(LatLng from)
    {
        return Math.Sqrt(
            Math.Pow(Latitude - from.Latitude, 2) + Math.Pow(Longitude - from.Longitude, 2)
        );
    }
    
    public double HaversineDistance(LatLng pos2)
    {
        LatLng pos1 = this;
        
        var R = 6371.0; // Earth's radius in kilometers
        var dLat = (pos2.Latitude - pos1.Latitude) * Math.PI / 180;
        var dLon = (pos2.Longitude - pos1.Longitude) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(pos1.Latitude * Math.PI / 180) * Math.Cos(pos2.Latitude * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        var distance = R * c;

        return distance;
    }
}