using System.Numerics;

namespace PCM.Core.Geom3d
{
    public class Vertex : Utils.Copy.ICopyable<Vertex>
    {
        public static readonly Vertex zero = new(0, 0, 0);
        public static Vertex Zero() { return new Vertex(0, 0, 0); }
        private static readonly double Epsilon = 1e-13;
        public double X;
        public double Y;
        public double Z;
        public Vertex(double X, double Y, double Z)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
            if (!IsValid())
                throw new Exception("Invalid Vertex");
        }

        public Vertex(System.Numerics.Vector3 vector){
            X = vector.X;
            Y = vector.Y;
            Z = vector.Z;
        }

        public bool IsValid() => !double.IsNaN(X) && !double.IsNaN(Y) && !double.IsNaN(Z);

        public bool Equals(Vertex v) => Math.Round(X, 6) == Math.Round(v.X, 6) && Math.Round(Y, 6) == Math.Round(v.Y, 6) && Math.Round(Z, 6) == Math.Round(v.Z, 6);
        public bool IsZero() => Math.Abs(X) <= Epsilon && Math.Abs(Y) <= Epsilon && Math.Abs(Z) <= Epsilon;
        public Vertex Add_inplace(Vertex v)
        {
            X += v.X;
            Y += v.Y;
            Z += v.Z;
            return this;
        }
        public Vertex Sub_inplace(Vertex v)
        {
            X -= v.X;
            Y -= v.Y;
            Z -= v.Z;
            return this;
        }
        public Vertex Multiply_inplace(double v)
        {
            X *= v;
            Y *= v;
            Z *= v;
            return this;
        }

        public Vertex Divide_inplace(double v)
        {
            return Multiply_inplace(1.0 / v);
        }
        public Vertex Add(Vertex v) => new(X + v.X, Y + v.Y, Z + v.Z);
        public Vertex Sub(Vertex v) => new(X - v.X, Y - v.Y, Z - v.Z);
        public Vertex Multiply(double v) => new(X * v, Y * v, Z * v);
        public Vertex Divide(double v) => Multiply(1 / v);
        public double Magnitude() => Math.Sqrt(X * X + Y * Y + Z * Z);
        public Vertex Cross(Vertex v) => new(Y * v.Z - Z * v.Y, Z * v.X - X * v.Z, X * v.Y - Y * v.X);
        public Vertex Unit()
        {
            var m = Magnitude();
            if (m <= 0)
            {
                // throw new Exception
                // Console.WriteLine("Can't create a unit vertex with a magnitude <= 0 " + this);
                return Zero();
            }
            return new Vertex(X / m, Y / m, Z / m);
        }
        public double Distance(Vertex v) => v.Sub(this).Magnitude();

        public double DistanceXZ(Vertex v)
        {
            return Distance(new Vertex(v.X, Y, v.Z));
        }
        public Vertex RotateY(double theta) => new(
                    Math.Round(X * Math.Cos(theta) + Z * Math.Sin(theta), 5),
                    Y,
                    Math.Round(-X * Math.Sin(theta) + Z * Math.Cos(theta), 5));

        public Vertex RotateY_inplace(double theta)
        {
            var tx = X;
            var tz = Z;
            X = Math.Round(tx * Math.Cos(theta) + tz * Math.Sin(theta), 5);
            // Y = Y;
            Z = Math.Round(-tx * Math.Sin(theta) + tz * Math.Cos(theta), 5);
            return this;
        }


        public double Dot(Vertex v) => X * v.X + Y * v.Y + Z * v.Z;

        public override string ToString() => "(" + X + " " + Y + " " + Z + ")";
        public Vertex Copy() => new(X, Y, Z);

        /// <summary>
        /// Project the vertex (p) on a line between 2 points (a,b)
        /// </summary>
        /// <param name="p">point to project</param>
        /// <param name="a">first point on the line</param>
        /// <param name="b">second point on the line</param>
        /// <returns></returns>
        public Vertex Project(Vertex a, Vertex b)
        {
            var ap = Sub(a);
            var ab = b.Sub(a);
            var factor = ap.Dot(ab) / ab.Dot(ab);
            var abmult = ab.Multiply(factor);
            return a.Add(abmult);
        }

        //Simplified, we consider X and Z
        /// <summary>
        /// Change coordinates of the vector in the new system with the lookAt as the default (0,0,1)
        ///      Y+
        ///      ^   
        ///      |   
        ///      |  
        ///      | 
        ///      0----------> X+
        ///     /
        ///    / 
        ///   / 
        ///  Z+
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="lookAt"></param>
        /// <returns></returns>
        public Vertex ChangeCoordinateSystem(Vertex pos, Vertex lookAt)
        {
            var point = new Vertex(lookAt.X, 0, lookAt.Z).Unit();
            var defaultA = new Vertex(0, 0, 1);
            var cross = defaultA.Cross(point);
            var theta = Math.Acos(
                 (defaultA.X * point.X + defaultA.Z * point.Z) /
            (Math.Sqrt(defaultA.X * defaultA.X + defaultA.Z * defaultA.Z) * Math.Sqrt(point.X * point.X + point.Z * point.Z)));
            theta = cross.Y > 0 ? -theta : theta;
            return Sub(pos).RotateY(theta);
        }

        public static Vertex[] ChangeCoordinateSystem(Vertex origin, Vertex lookAt, Vertex[] targetVertices){
            var transfVertices = new Vertex[targetVertices.Length];
            for (int j = 0; j < targetVertices.Length; j++)
                transfVertices[j] = targetVertices[j].ChangeCoordinateSystem(origin, lookAt);
            return transfVertices;
        }

        public System.Numerics.Vector3 ToVector3()
        {
            return new System.Numerics.Vector3((float)X, (float)Y, (float)Z);
        }
    }
}