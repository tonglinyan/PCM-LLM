namespace PCM.Core.SimplePhysics
{
    /// <summary>
    /// Used for 2d collisions. In our case, the thrid dimension is not used to detect collisions (we are on a plane)
    /// </summary>
    public static class Collider
    {
        public interface ICollidable<T>
        {
            public bool Collides(T other);
        }
        /// <summary>
        /// 2D convex polygon collision using SAT method
        /// </summary>
        /// see https://medium.com/@edu.js.o/test-collision-with-separating-axis-theorem-in-javascript-8456d1c92b6c
        public class Convex2DPolygonCollider : ICollidable<Convex2DPolygonCollider>
        {
            readonly Geom3d.Polygon polygon;
            public Convex2DPolygonCollider(Geom3d.Polygon polygon)
            {
                this.polygon = polygon;
            }
            public override string ToString()
            {
                return $"Collider {polygon}";
            }
            public bool Collides(Convex2DPolygonCollider other)
            {
                var edges = GetEdges();
                edges.AddRange(other.GetEdges());
                foreach (var edge in edges)
                {
                    double magn = Math.Sqrt(edge.x * edge.x + edge.y * edge.y);
                    double x = -edge.y / magn;
                    double y = edge.x / magn;
                    var a = ProjectOnAxis(x, y);
                    var b = other.ProjectOnAxis(x, y);
                    //find gaps
                    if ((a.min < b.min ? b.min - a.max : a.min - b.max) > 0)
                        return false;
                }
                return true;

            }
            List<(double x, double y)> GetEdges()
            {
                var edges = new List<(double x, double y)>();
                for (int i = 0; i < polygon.Vertices.Length; i++)
                {
                    var a = polygon.Vertices[i];
                    var b = polygon.Vertices[(i + 1) % polygon.Vertices.Length];
                    edges.Add((b.X - a.X, b.Z - a.Z));
                }
                return edges;
            }
            
            static double IntervalDistance(double minA, double maxA, double minB, double maxB) => minA < minB ? minB - maxA : minA - maxB;

            (double min, double max) ProjectOnAxis(double x, double y)
            {
                double min = double.MaxValue;
                double max = double.MinValue;
                foreach (Geom3d.Vertex v in polygon.Vertices)
                {
                    double px = v.X;
                    //In 3d, y is the height, we don't consider height for 2 collisions
                    double py = v.Z;
                    double projection = (px * x + py * y) / Math.Sqrt(x * x + y * y);
                    min = Math.Min(projection, min);
                    max = Math.Max(projection, max);
                }
                return (min, max);
            }
        }

        /// <summary>
        /// 2D convex hull of points, ignore the Y coordinate
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        /// see https://github.com/indy256/convexhull-js/blob/master/convexhull.js
        static public Geom3d.Polygon ConvexHull2D(List<Geom3d.Vertex> points)
        {
            points.Sort(OrderPoints);
            var n = points.Count;
            var hull = new List<Geom3d.Vertex>();
            for (var i = 0; i < 2 * n; i++)
            {
                var j = i < n ? i : 2 * n - 1 - i;
                while (hull.Count >= 2 && RemoveMiddle(hull[hull.Count - 2], hull[hull.Count - 1], points[j]))
                    hull.RemoveAt(hull.Count - 1);
                hull.Add(points[j]);
            }
            hull.RemoveAt(hull.Count - 1);
            return new Geom3d.Polygon(hull.ToArray());
        }
        static int OrderPoints(Geom3d.Vertex a, Geom3d.Vertex b) => (int)(a.X != b.X ? a.X - b.X : a.Z - b.Z);
        static bool RemoveMiddle(Geom3d.Vertex a, Geom3d.Vertex b, Geom3d.Vertex c)
        {
            var cross = (a.X - b.X) * (c.Z - b.Z) - (a.Z - b.Z) * (c.X - b.X);
            var dot = (a.X - b.X) * (c.X - b.X) + (a.Z - b.Z) * (c.Z - b.Z);
            return cross < 0 || cross == 0 && dot <= 0;
        }
    }
}