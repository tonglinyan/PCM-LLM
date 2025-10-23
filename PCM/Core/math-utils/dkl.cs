namespace PCM.Core.MathUtils
{
    public static class DKL
    {

        private static readonly double Sqrt2 = Math.Sqrt(2.0);
        private static readonly int N = 1000;
        public static double Epsilon = 10e-6;
        private static readonly double CustomInfinity = 1e200;
        public static (double[] gaussian, double sum) _refGaussian = TruncatedGaussian(0.95, 0.1, N, Epsilon);
        public static double[] refGaussian = Utils.Arrays.MultArray(_refGaussian.gaussian, 1 / _refGaussian.sum);

        public static double FakeFe(double mu, double sigma)
        {
            if (_precomputedDKL != null){
                int sigmaInd = (int)(sigma / FreeEnergy.Constants.sevuncertaingain * _precomputedDKL.Length);
                int muInd = (int)(mu * _precomputedDKL[0].Length);
                return _precomputedDKL[sigmaInd][muInd];
            }
            var (gaussian, sum) = TruncatedGaussian(mu, sigma, N, Epsilon);
            var p = Utils.Arrays.MultArray(gaussian, 1 / sum);
            var dkl = RelativeEntropy(p, refGaussian);
            if (double.IsNaN(dkl))
            {
                Console.WriteLine($"{dkl} - {mu} {sigma}");
                Console.WriteLine(string.Join(",", p));
                Console.WriteLine(string.Join(",", refGaussian));
                throw new Exception("DKL - Vecotrs size mismatch");
            }
            return dkl;
        }
        public static double RelativeEntropy(double[] p, double[] q)
        {
            if (p.Length != q.Length)
                throw new Exception("DKL - Vecotrs size mismatch");

            // var annulP = annul(p);
            // var annulQ = annul(q);
            // if (compareAnnul(annulQ, annulP) == p.Length)
            // {
            double dkl = 0;
            int j = 0;
            for (var i = 0; i < q.Length; i++)
            {
                // if (!anah nulP[i])
                // {
                var lg = Math.Log(p[i] / q[i])/Math.Log(2.0);
                dkl += p[i] * lg;
                j++;
                // }
            }
            return dkl;
            // }
            // else
            // {
            //     return 1e200;
            // }
        }

        public static (double[] gaussian, double sum) TruncatedGaussian(double mu, double sigma, int n, double epsilon = 0)
        {
            //Epsilon could be a problem if n is a big number
            var result = new double[n];
            double a = 0;
            double sum = 0;
            for (int i = 1; i <= n; i++)
            {
                var trunc = Ptruncnorm(i / (double)n, 0, 1, mu, sigma);
                result[i - 1] = trunc - a + epsilon;
                sum += result[i - 1];
                a = trunc;
            }
            return (result, sum);
        }

        //Truncated normal distribution (https://en.wikipedia.org/wiki/Truncated_normal_distributionf)
        //See aswell https://github.com/olafmersmann/truncnorm/blob/master/src/truncnorm.c
        public static double Pdfn(double x) => 1 / Math.Sqrt(Math.PI * 2) * Math.Exp(-0.5 * (x * x));
        private static double Φ(double x) => 0.5 + Erf.erf(x / Sqrt2) / 2.0;

        private static double Z(double a, double b, double mean, double sd) => Φ((b - mean) / sd) - Φ((a - mean) / sd);
        private static double Dtruncnorm(double x, double a = double.NegativeInfinity, double b = double.PositiveInfinity, double mean = 0, double sd = 1.0) => sd == 0 ? throw new Exception("dtruncnorm: sd can't be 0") : 1 / sd * (Pdfn((x - mean) / sd) / Z(a, b, mean, sd));

        private static double Fact(double x, double mean, double sd) => (x - mean) / sd;
        public static double Ptruncnorm(double x, double a = double.NegativeInfinity, double b = double.PositiveInfinity, double mean = 0, double sd = 1.0)
        {
            if (x < a)
                return 0.0;
            else if (x > b)
                return 1.0;
            else
            {
                double c1 = Φ(Fact(x, mean, sd));
                double c2 = Φ(Fact(a, mean, sd));
                double c3 = Φ(Fact(b, mean, sd));
                return (c1 - c2) / (c3 - c2);
                // return (Φ((x - mean) / sd) - Φ((a - mean) / sd)) / Z(a, b, mean, sd);
            }
        }

        //Precomputing DKL
        private static double[][] _precomputedDKL = null;
        public static void PrecomputeDKL(int size)
        {
            var sigmas = Misc.LinSpace(0.01, FreeEnergy.Constants.sevuncertaingain, size);
            var prefs = Misc.LinSpace(0, 1, size);
            
            double[][] precomputedDKL  = new double[sigmas.Length][];
            for (var isigma = 0; isigma < sigmas.Length; isigma++){
                var sigma = sigmas[isigma];
                precomputedDKL[isigma] = new double[prefs.Length];
                for (var ipref = 0; ipref < prefs.Length; ipref++)
                {
                    precomputedDKL[isigma][ipref] = FakeFe(prefs[ipref], sigma);
                    // Console.WriteLine($"{ipref} {prefs.Length} {precomputedDKL[isigma].Length} > {prefs[ipref]}");
                }
            }
            _precomputedDKL = precomputedDKL;
        }

        public static void TestError()
        {
            double[] Rvalues = new double[]{
                1.925318e-07 ,
                3.141451e-07 ,
                5.074796e-07 ,
                8.116477e-07 ,
                1.285219e-06 ,
                2.014872e-06 ,
                3.127363e-06 ,
                4.805846e-06 ,
                7.311762e-06 ,
                1.101374e-05 ,
                1.642511e-05 ,
                2.425171e-05 ,
                3.545171e-05 ,
                5.130889e-05 ,
                7.352053e-05 ,
                0.0001043002 ,
                0.000146495 ,
                0.0002037139 ,
                0.0002804654 ,
                0.000382295 ,
                0.0005159156 ,
                0.0006893174 ,
                0.000911844 ,
                0.001194215 ,
                0.001548478 ,
                0.001987872 ,
                0.002526576 ,
                0.003179339 ,
                0.003960975 ,
                0.004885714 ,
                0.005966431 ,
                0.007213763 ,
                0.008635149 ,
                0.01023383 ,
                0.01200792 ,
                0.01394947 ,
                0.01604383 ,
                0.0182692 ,
                0.0205964 ,
                0.02298921 ,
                0.02540489 ,
                0.02779529 ,
                0.03010827 ,
                0.03228948 ,
                0.03428444 ,
                0.03604074 ,
                0.03751034 ,
                0.03865173 ,
                0.03943189 ,
                0.03982786 ,
                0.03982786 ,
                0.03943189 ,
                0.03865173 ,
                0.03751034 ,
                0.03604074 ,
                0.03428444 ,
                0.03228948 ,
                0.03010827 ,
                0.02779529 ,
                0.02540489 ,
                0.02298921 ,
                0.0205964 ,
                0.0182692 ,
                0.01604383 ,
                0.01394947 ,
                0.01200792 ,
                0.01023383 ,
                0.008635149 ,
                0.007213763 ,
                0.005966431 ,
                0.004885714 ,
                0.003960975 ,
                0.003179339 ,
                0.002526576 ,
                0.001987872 ,
                0.001548478 ,
                0.001194215 ,
                0.000911844 ,
                0.0006893174 ,
                0.0005159156 ,
                0.000382295 ,
                0.0002804654 ,
                0.0002037139 ,
                0.000146495 ,
                0.0001043002 ,
                7.352053e-05 ,
                5.130889e-05 ,
                3.545171e-05 ,
                2.425171e-05 ,
                1.642511e-05 ,
                1.101374e-05 ,
                7.311762e-06 ,
                4.805846e-06 ,
                3.127363e-06 ,
                2.014872e-06 ,
                1.285219e-06 ,
                8.116477e-07 ,
                5.074796e-07 ,
                3.141451e-07 ,
                1.925318e-07
            };

            var (gaussian, _) = TruncatedGaussian(0.5, 0.1, 100);
            Console.WriteLine($"{Rvalues.Length} {gaussian.Length}");
            var dif = 0.0;
            var min = 100.0;
            for (var i = 0; i < gaussian.Length; i++)
            {
                var v = Math.Abs(gaussian[i] - Rvalues[i]);
                Console.WriteLine(v);
                dif = Math.Max(dif, v);
                min = Math.Min(dif, v);
            }
            Console.WriteLine(dif);
            Console.WriteLine(min);

        }
    }

}
