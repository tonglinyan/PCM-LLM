namespace PCM.Core.MathUtils{
    public static class Misc{
        /// <summary>
        /// Create a gaussian function
        /// </summary>
        /// <param name="mu">mu</param>
        /// <param name="sigma">sigma</param>
        /// <returns>Gaussian function</returns>
        public static Func<double, double> Gaussian(double mu, double sigma) => (x) => 1 / (sigma * Math.Sqrt(2 * Math.PI)) * Math.Exp(-(Math.Pow((x - mu) / sigma, 2) / 2));

        /// <summary>
        /// Create a linear space between min and max of length
        /// </summary>
        /// <param name="min">minimum value</param>
        /// <param name="max">maximum value</param>
        /// <param name="length">number of values</param>
        /// <returns>linear space (array of double)</returns>
        public static double[] LinSpace(double min, double max, int length)
        {
            //return new double[length].Select((v, i) => min + i * (max - min) / (length - 1.0)).ToArray();
            var r = new double[length];
            for (int i = 0; i < length; i++)
            {
                r[i] = min + i * (max - min) / (length - 1.0);
            }
            return r;
        }
        /// <summary>
        /// Sigmoid function
        /// </summary>
        /// <param name="x"></param>
        /// <param name="a"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static double Sigmoid(double x, double a, double c)
        {
            double k = Math.Exp(-a * (x - c));
            return 1.0 / (1.0 + k);
        }
        public static (double[] emu, double[] uemu) ApplySigmoid(double[] mu, double[] dmu, double we)
        {
            const double siggain = 0.55;

            //double[] umu = Utils.AddArrays(mu, dmu);
            const double wp = 1;
            const double a = siggain * 10;
            const double c = 0.5;
            //double[] emu = umu.Select(x => Sigmoid(x, a, c)).ToArray();
            //double[] uemu = mu.Zip(emu, (x1, x2) => (x1 * wp + we * x2) / (wp + we)).ToArray();
            double[] emu = new double[mu.Length];
            double[] uemu = new double[mu.Length];
            for (var i = 0; i < emu.Length; i++)
            {
                emu[i] = Sigmoid(mu[i] + dmu[i], a, c);
                uemu[i] = (mu[i] * wp + we * emu[i]) / (wp + we);
            }
            return (emu, uemu);
        }

        public static double AngleBetween(Geom3d.Vertex a, Geom3d.Vertex b){
            var theta = Math.Acos((a.X * b.X + a.Z * b.Z) / (Math.Sqrt(a.X * a.X + a.Z * a.Z) * Math.Sqrt(b.X * b.X + b.Z * b.Z)));
            return double.IsNaN(theta) ? 0 : theta;
        }
        
        public static double AngleBetween(Geom3d.Vertex position, Geom3d.Vertex lookAt, Geom3d.Vertex target) => AngleBetween(lookAt, target.Sub(position));
    }
}