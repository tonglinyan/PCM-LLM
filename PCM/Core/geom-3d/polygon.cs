namespace PCM.Core.Geom3d
{
    public class Polygon
    {
        public Vertex[] Vertices;
        public Polygon(Vertex[] vertices)
        {
            if (vertices.Length < 3)
                throw new Exception("Can't create a polygon with less than 3 vertices");
            Vertices = vertices;
        }
        public double Area()
        {
            Vertex a = Vertex.Zero();
            int j = 0;
            for (int i = 0; i < Vertices.Length; i++)
            {
                j = (i + 1) % Vertices.Length;
                a = a.Add(Vertices[i].Cross(Vertices[j]));
            }
            a = a.Divide(2);
            return Vertex.Zero().Distance(a);
        }
        public Vertex Normal()
        {
            for (var i = 0; i < Vertices.Length - 2; i++)
            {
                var edge1 = Vertices[i + 1].Sub(Vertices[i]);
                var edge2 = Vertices[i + 2].Sub(Vertices[i + 1]);
                var cross = edge1.Cross(edge2);
                if (!cross.Equals(Vertex.zero))
                    return cross.Unit();
            }
            return null;
            //throw Objects.Utils.Error("Polygon","Impossible to find a vector normal to the plane\n"+String.Join("\n",Vertices.ToList()));
        }

        public override string ToString()
        {
            return $"Polygon vertices: {string.Join(",", Vertices.Select(v => v.ToString()))}";
        }
    }
}