namespace gRPC_Service.Utils;

public class LatLng
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public LatLng(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }
}