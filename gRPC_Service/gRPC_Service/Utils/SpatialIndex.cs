
namespace gRPC_Service.Utils;

using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;

public class SpatialIndex<T>
{
    private STRtree<T> spatialIndex;

    public SpatialIndex()
    {
        spatialIndex = new STRtree<T>();
    }

    public void Insert(Geometry geometry, T item)
    {
        spatialIndex.Insert(geometry.EnvelopeInternal, item);
    }

    public List<T> Query(Geometry queryGeometry)
    {
        return spatialIndex.Query(queryGeometry.EnvelopeInternal)
            .OfType<T>()
            .ToList();
    }
}