namespace PCM.Core.Geom3d
{
    public class Polyhedron : Utils.Copy.ICopyable<Polyhedron>
    {
        public Vertex[] Vertices;
        public int[][] Faces;
        public static readonly Polyhedron EmptyPolyhedron = new(Array.Empty<Vertex>(), Array.Empty<int[]>(), Vertex.Zero());
        public Vertex Center;
        public Polyhedron(Vertex[] vertices, int[][] faces, Vertex center)
        {
            //TODO: Should check that we have at least 3 faces and are fully closed
            Vertices = vertices;
            Faces = faces;
            Center = center;
            // Faces = faces.Select(i => new Polygon(i.Select(j => Vertices[j]).ToArray())).ToArray();
        }
        public bool Equals(Polyhedron p)
        {
            if (Vertices.Length != p.Vertices.Length)
                return false;
            if (Faces.Length != p.Faces.Length)
                return false;
            for (int i = 0; i < Faces.Length; i++)
            {
                if (Faces[i].Length != p.Faces[i].Length)
                    return false;
                for (int j = 0; j < Faces[i].Length; j++)
                    if (Faces[i][j] != p.Faces[i][j])
                        return false;
            }
            for (int i = 0; i < Vertices.Length; i++)
            {
                if (!Vertices[i].Equals(p.Vertices[i]))
                    return false;
            }
            return true;
        }

        public Polyhedron Translate_inplace(Vertex speed)
        {
            Center.Add_inplace(speed);
            foreach (Vertex v in Vertices)
                v.Add_inplace(speed);
            return this;
        }

        public Polyhedron RotateY_inplace(double theta)
        {
            //translate to 0,0
            var origin = Center.Copy();
            Translate_inplace(Center.Multiply(-1));
            //rotate
            for (int i = 0; i < Vertices.Length; i++)
                Vertices[i].RotateY_inplace(theta);
            // Vertices[i] = Vertices[i].RotateY(theta);
            //translate to origin
            Translate_inplace(origin);
            return this;
        }
        
        private double FacePyramidVolume(Vertex point, Polygon face)
        {
            var norm = face.Normal();
            if (norm == null)
                return 0;
            var vtop = face.Vertices[0].Sub(point);
            var height = Math.Abs(norm.Dot(vtop));
            return height * face.Area() * 1 / 3;
        }

        public double Volume()
        {
            var volume = 0.0;
            var baseVertex = Vertices[0];
            for(var fi = 0; fi < Faces.Length; fi++)
            {
                volume += FacePyramidVolume(baseVertex, GetFace(fi));
            }
            return volume;
        }

        /// Warning: Faces are not copied
        public Polyhedron Copy()
        {
            var verticesCopy = Utils.Copy.CopyArray(Vertices);
            var facesCopy = new int[Faces.Length][];
            Array.Copy(Faces, facesCopy, Faces.Length);
            //Faces are not copied since we don't expect changes
            return new Polyhedron(verticesCopy, facesCopy, Center.Copy());
        }

        //TODO: add faces
        public override string ToString()
        {
            string r = "";
            foreach (var v in Vertices)
                r += v.ToString() + "\n";
            return r;
        }

        public bool IsVisibleBy(Vertex position, Vertex lookAt)
        {
            for (int j = 0; j < Vertices.Length; j++)
            {
                if (Vertices[j].ChangeCoordinateSystem(position, lookAt).Z > 0)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsFullyVisibleBy(Vertex position, Vertex lookAt)
        {
            for (int j = 0; j < Vertices.Length; j++)
            {
                if (Vertices[j].ChangeCoordinateSystem(position, lookAt).Z <= 0)
                {
                    return false;
                }
            }
            return true;
        }

        public Polyhedron GetPolyhedronInOtherCoordinateSystemIfFullyVisible(Vertex position, Vertex lookAt)
        {
            var verts = new Vertex[Vertices.Length];
            for (int j = 0; j < Vertices.Length; j++)
            {
                verts[j] = Vertices[j].ChangeCoordinateSystem(position, lookAt);
                if (verts[j].Z <= 0)
                {
                    return null;
                }
            }

            return new Polyhedron(verts, Faces, Center.ChangeCoordinateSystem(position, lookAt));
        }

        //Dirty, faces handling should be different, probably pointers
        public Polygon GetFace(int faceIndex) => new(Faces[faceIndex].Select(i => Vertices[i]).ToArray());
        public IEnumerable<Vertex> GetFaceVertices(int faceNumber) => Faces[faceNumber].Select(i => Vertices[i]);
        public IEnumerable<Vertex> GetBottomFaceVertices() => Faces[^1].Select(i => Vertices[i]);


        //https://stackoverflow.com/questions/8877872/determining-if-a-point-is-inside-a-polyhedron
        public bool ContainsPoint(Vertex point){
            for(var fi = 0; fi < Faces.Length; fi++){
                Polygon face = GetFace(fi);
                Vertex p2f = face.Vertices[0].Sub(point);
                double d = p2f.Dot(face.Normal());
                d /= p2f.Magnitude();
                const double bound = -1e-15;
                if(d < bound)
                    return false;
            }
            return true;
        }


        public static Polyhedron RectangularPrism(Vertex center, double width, double height, double depth)
        {
            var c = center;
            double w = width / 2; double h = height / 2; double d = depth / 2;
            var rectPts = new Vertex[]{
            new(c.X-w,c.Y-h,c.Z+d),
            new(c.X+w,c.Y-h,c.Z+d),
            new(c.X-w,c.Y+h,c.Z+d),
            new(c.X+w,c.Y+h,c.Z+d),

            new(c.X-w,c.Y-h,c.Z-d),
            new(c.X+w,c.Y-h,c.Z-d),
            new(c.X-w,c.Y+h,c.Z-d),
            new(c.X+w,c.Y+h,c.Z-d),
            };
            return new Polyhedron(rectPts, new int[][]{
                new int[]{0,1,3,2},
                new int[]{1,5,7,3},
                new int[]{5,4,6,7},
                new int[]{4,0,2,6},
                new int[]{2,3,7,6},
                new int[]{4,5,1,0},
            }, center.Copy());
        }

    }

}