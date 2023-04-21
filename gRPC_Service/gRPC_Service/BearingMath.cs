namespace gRPC_Service;

public class BearingMath
{
    // LatLng MakeStep(LatLng from, double distance = 500.0)
    // {
    //     // Calculate bearing from startingPos to endingPos
    //     // var dLat = (endingPos.Latitude - startingPos.Latitude) * Math.PI / 180;
    //     var dLon = (endingPos.Longitude - from.Longitude) * Math.PI / 180;
    //     var y = Math.Sin(dLon) * Math.Cos(endingPos.Latitude * Math.PI / 180);
    //     var x = Math.Cos(from.Latitude * Math.PI / 180) * Math.Sin(endingPos.Latitude * Math.PI / 180) - Math.Sin(from.Latitude * Math.PI / 180) * Math.Cos(endingPos.Latitude * Math.PI / 180) * Math.Cos(dLon);
    //     var bearing = Math.Atan2(y, x) * 180 / Math.PI;
    //
    //     var R = 6371.0; // Earth's radius in kilometers
    //     var lat1 = from.Latitude * Math.PI / 180;
    //     var lon1 = from.Longitude * Math.PI / 180;
    //     var lat2 = Math.Asin(Math.Sin(lat1) * Math.Cos(distance / R) + Math.Cos(lat1) * Math.Sin(distance / R) * Math.Cos(bearing * Math.PI / 180));
    //     var lon2 = lon1 + Math.Atan2(Math.Sin(bearing * Math.PI / 180) * Math.Sin(distance / R) * Math.Cos(lat1), Math.Cos(distance / R) - Math.Sin(lat1) * Math.Sin(lat2));
    //     var newLatLng = new LatLng { Latitude = lat2 * 180 / Math.PI, Longitude = lon2 * 180 / Math.PI };
    //
    //     return newLatLng;
    // }
}