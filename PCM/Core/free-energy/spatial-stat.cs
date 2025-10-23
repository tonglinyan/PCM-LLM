using PCM.Core.Geom3d;
namespace PCM.Core.FreeEnergy
{
    //GW = 3
    //MATRIX = 0.14
    public static class SpatialStat
    {
        public static void Init(double c, double volume, double gaussianDisp, double amplificationFactor)
        {
            FieldBoxPerspVolume = volume; //  (0.9 * 1/c * d * h)^1/4
            setMatrix(c); //0.15
            GaussianDisp = gaussianDisp;
            AmplificationFactor = amplificationFactor;
        }
        private static double[][] getMatrix(double depth)
        {
            double a = 1;
            double b = 1;
            double c = 1;
            double d = 1;
            return new double[][]{
                new double[]{a,0,0,0},
                new double[]{0,b,0,0},
                new double[]{0,0,c,0},
                new double[]{0,0, depth,d}
            };
        }

        public static void setMatrix(double depth)
        {
            Matrix = getMatrix(depth);
        }
        //Constants
        public static double[][] Matrix;
        private static double FieldBoxPerspVolume;

        private static double GaussianDisp;  //3*1.17
        private static double AmplificationFactor; //30
        /// <summary>
        /// Projection of the vertices using the projection matrix (no library)
        /// (PMatrix * homogenousVert)/(PMatrix * homogenousVert).homogenousCoord 
        /// </summary>
        /// <param name="vertices"></param>
        /// <returns></returns>
        public static Vertex[] Projection(Vertex[] vertices)
        {
            var M = Matrix;//getMatrix(depth);
            var M2 = new double[4][];
            for (var i = 0; i < M2.Length; i++)
            {
                M2[i] = new double[vertices.Length];
            }
            for (var j = 0; j < vertices.Length; j++)
            {
                var v = vertices[j];
                M2[0][j] = v.X;
                M2[1][j] = v.Y;
                M2[2][j] = v.Z;
                M2[3][j] = 1;
            }
            var result = new double[M.Length][];
            for (var i = 0; i < M.Length; i++)
            {
                result[i] = new double[M2[0].Length];
                for (var j = 0; j < M2[0].Length; j++)
                {
                    double sum = 0;
                    for (var k = 0; k < M[0].Length; k++)
                    {
                        sum += M[i][k] * M2[k][j];
                    }
                    result[i][j] = sum;
                }
            }
            var outVert = new Vertex[vertices.Length];
            for (var j = 0; j < outVert.Length; j++)
            {
                outVert[j] = new Vertex(
                    result[0][j] / result[3][j],
                    result[1][j] / result[3][j],
                    result[2][j] / result[3][j]
                );
            }
            return outVert;
        }

        public static double GaussianWeight(Vertex[] p, double disp)
        {
            double a = 1;
            Vertex objCenter = Vertex.Zero();
            for (int i = 0; i < p.Length; i++)
            {
                objCenter.X += p[i].X;
                //objCenter.Y += p[i].Y;
                objCenter.Z += p[i].Z;
            }
            objCenter = objCenter.Multiply(1.0 / p.Length);
            var excent = objCenter.Magnitude();
            return a * Math.Exp(-(excent * excent) / (2.0 * (disp * disp)));
        }
        public static double GetMuWithSpatialStat(double spatialStat, double tMu, double interactionFactor = 1)
        {
            var objmu = tMu;
            var fbmu = 0.5;
            var k = AmplificationFactor; //slope around the center
            var newMu = spatialStat * objmu + (1 - spatialStat) * fbmu;
            var resultMu = 1 / (1 + Math.Exp(-k * (newMu - fbmu)));

            resultMu = Math.Max(0.0, Math.Min(1.0, resultMu));
            if (interactionFactor != 1){
                if(resultMu > 0.5)
                    resultMu += 0.1;
                else if(resultMu < 0.5)
                    resultMu -= 0.1;
            }
            return resultMu;
        }

        public static double GetSpatialStat(Polyhedron obj)
        {
            var objVertices = Projection(obj.Vertices);
            var objT = new Polyhedron(objVertices, obj.Faces, obj.Center);
            var fbvqr = FieldBoxPerspVolume;
            var ovqr = Math.Pow(objT.Volume(), 0.25);
            var disp = GaussianDisp;
            var gw = GaussianWeight(objT.Vertices, disp);
            return gw * Math.Min(1, ovqr / fbvqr);
        }
    }
}