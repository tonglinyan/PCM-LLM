namespace PCM.Core.Utils
{
    public static class Misc
    {
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

        private static double preferenceValenceStrengthRelation(double valenceStrength, double preference)
        {
            return valenceStrength - Math.Pow(preference - 0.5, 2) * 3.6;
        }

        public static double[] UpdatePriors(double[] priorValues, double valence, double[] certainty, double updateWeight)
        {
            const double neutralRef = 0.5;
            const double gain = 0.2;
            //const double siggain = 0.55;
            const double priorWeight = 1;
            const double a = 2.75;//siggain * 10 / 2; => sigmoid slope

            //Re-interpret the valence -> we can play with the sigmoid to
            //model the reaction to strong or low valences
            double valenceStrength = Sigmoid(valence, a, 0) - neutralRef;

            double[] updatedPriors = new double[priorValues.Length];
            for (var i = 0; i < updatedPriors.Length; i++)
            {

                //now we balance the valenceStrength with the actual preference:
                //the higher a preference, the higher a valenceStrength needs to be
                //to increment it
                //Add that magnitude to the prior balanced by the certainty
                //TODO This model is still not good enough. If the expressed emotion (valence) is positive, but just a little
                //it will still improve an already high preference when, maybe, it should decrease it a little bit
                double updateStrength = preferenceValenceStrengthRelation(valenceStrength, priorValues[i]);
                double updatedExpectedPrior = priorValues[i] + updateStrength * certainty[i] * gain;

                updatedExpectedPrior = updatedExpectedPrior > 1.0 ? 1.0 : updatedExpectedPrior < 0 ? 0.0 : updatedExpectedPrior;
                //Average using prior weight and update weight
                updatedPriors[i] = (priorValues[i] * priorWeight + updatedExpectedPrior * updateWeight) / (priorWeight + updateWeight);
            }
            return updatedPriors;
        }
        
        /// <summary>
        /// Update priors according to a valence strength (f(val) = val^3) and a modulated sigmoid
        /// </summary>
        /// <param name="priorValues"></param>
        /// <param name="valence"></param>
        /// <param name="certainty"></param>
        /// <param name="updateWeight">Weight of the updte compared to the current value</param>
        /// <returns></returns>
        public static double[] UpdatePriorsV2(double[] priorValues, double valence, double[] certainty, double updateWeight)
        {
            //gain applied to the updatedPrior
            const double updateGain = 0.5;

            const double priorWeight = 1;

            //first, compute the valence strength, we can use f(x) = x or f(x) = x^3 for example
            double valenceStrength = valence * valence * valence;

            const double α = 1;
            const double λ = 25;

            double[] updatedPriors = new double[priorValues.Length];
            for (var i = 0; i < updatedPriors.Length; i++)
            {
                double p = priorValues[i];
                double newPrior = updateGain * (-0.5 + (0.5 - p) * α + 1 / (1 + Math.Exp(-λ * valenceStrength)));

                double updatedExpectedPrior = p + newPrior * certainty[i];

                updatedExpectedPrior = updatedExpectedPrior > 1.0 ? 1.0 : updatedExpectedPrior < 0 ? 0.0 : updatedExpectedPrior;

                //Average using prior weight and update weight
                updatedPriors[i] = (p * priorWeight + updatedExpectedPrior * updateWeight) / (priorWeight + updateWeight);
            }
            return updatedPriors;
        }

        public static double[] UpdatePriorsForPlayer(double[] priorValues,double valence, int playerId, double[] valenceFactor){
            double λ = 0.1;
            double a = 17;
            var newPreferences = new double[priorValues.Length];
            for (var i = 0; i < priorValues.Length; i++){
                if(valenceFactor[i]  > 0 && i != playerId){
                    newPreferences[i] = 0.5 + a * (-0.5 + 1 / (1 + Math.Exp(-λ * valence * valenceFactor[i])));
                }else{
                    newPreferences[i] = priorValues[i];
                }
            }
            return newPreferences;
        }

        /// <summary>
        /// Norm of a vector
        /// </summary>
        /// <param name="v">vector (array of double)</param>
        /// <returns></returns>
        public static double Norm(double[] v)
        {
            return Math.Sqrt(v.Sum(val => val * val));
        }

        /// <summary>
        /// Computes a normlized look at vector
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static double[] NormalizedLookAt(double[] origin, double[] target)
        {
            double[] toTargetVect = Arrays.SubArrays(target, origin);
            double targetDistance = Norm(toTargetVect);
            //return toTargetVect.Select(v => v / (Math.Abs(targetDistance) < float.Epsilon ? 1 : targetDistance)).ToArray();
            for (var i = 0; i < toTargetVect.Length; i++)
            {
                toTargetVect[i] *= 1 / (Math.Abs(targetDistance) < float.Epsilon ? 1 : targetDistance);
            }
            return toTargetVect;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="rows"></param>
        /// <param name="columns"></param>
        /// <param name="mu"></param>
        /// <param name="sigma"></param>
        /// <returns></returns>
        public static (double[][] mvnpd, double sum) MultivariateNormalProbabilityDensityFunction(double[] rows, double[] columns, double[] mu, double[][] sigma)
        {
            double sum = 0;
            var mvnpd = new double[rows.Length][];
            var mvnd = new MathUtils.MVN.MultivariateNormalDistributionCustom(mu, sigma);
            var point = new double[2];
            for (int row = 0; row < rows.Length; row++)
            {
                mvnpd[row] = new double[columns.Length];
                point[0] = rows[row];

                for (int col = 0; col < columns.Length; col++)
                {
                    point[1] = columns[col];
                    mvnpd[row][col] = mvnd.ProbabilityDensityFunction(point);
                    sum += mvnpd[row][col];
                }
            }
            return (mvnpd, sum);
        }

        /// <summary>
        /// Dot product between two vectors (arrays)
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public static double Dot(double[] v1, double[] v2)
        {
            if (v1.Length != v2.Length)
                throw new Exception($"Vectors have to be the same size. {v1.Length} != {v2.Length}");
            double res = 0;
            for (int i = 0; i < v1.Length; i++)
                res += v1[i] * v2[i];
            return res;
        }

        /// <summary>
        /// Project a point (p) on a line between 2 points (a,b)
        /// </summary>
        /// <param name="p">point to project</param>
        /// <param name="a">first point on the line</param>
        /// <param name="b">second point on the line</param>
        /// <returns></returns>
        public static double[] Project(double[] p, double[] a, double[] b)
        {
            var ap = Arrays.SubArrays(p, a);
            var ab = Arrays.SubArrays(b, a);
            var factor = Dot(ap, ab) / Dot(ab, ab);
            var abmult = new double[ab.Length];
            for (var i = 0; i < abmult.Length; i++) abmult[i] = ab[i] * factor;
            //return AddArrays(a, ab.Select(v => v * factor).ToArray());
            return Arrays.AddArrays(a, abmult);
        }

        public static double DistSegPoint(double[] p, double[] sp1, double[] sp2)
        {

            var x = p[0];
            var y = p[1];
            var x1 = sp1[0];
            var y1 = sp1[1];
            var x2 = sp2[0];
            var y2 = sp2[1];
            var A = x - x1;
            var B = y - y1;
            var C = x2 - x1;
            var D = y2 - y1;

            var dot = A * C + B * D;
            var len_sq = C * C + D * D;
            double param = -1.0;
            if (len_sq != 0) //in case of 0 length line
                param = dot / len_sq;

            double xx, yy;

            if (param < 0)
            {
                xx = x1;
                yy = y1;
            }
            else if (param > 1)
            {
                xx = x2;
                yy = y2;
            }
            else
            {
                xx = x1 + param * C;
                yy = y1 + param * D;
            }

            var dx = x - xx;
            var dy = y - yy;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Determine if a point p is in front of an object located
        /// at origin and looking at direction lookAt
        /// </summary>
        /// <param name="point">point to check</param>
        /// <param name="origin">position of observer</param>
        /// <param name="lookAt">direction of observation</param>
        /// <returns></returns>
        public static bool IsInFront(double[] point, double[] origin, double[] lookAt) => Dot(Arrays.SubArrays(lookAt, origin), Arrays.SubArrays(point, origin)) > 0;

        public static bool IsInFrontCircle(double[] point, double[] origin, double[] lookAt, double radius)
        {
            if (IsInFront(point, origin, lookAt))
                return true;
            else
            {
                for (var i = 0; i < 7; i++)
                {
                    double alpha = Math.PI / 4;
                    var curpoint = new double[]
                    {
                        point[0]+Math.Cos(alpha)*radius,
                        point[1]+Math.Sin(alpha)*radius,
                        0
                    };
                    if (IsInFront(curpoint, origin, lookAt))
                        return true;
                }
                return false;
            }

        }
        
        public static List<double[]> GetOrientations(int sample = 16, double baseAngleRad = 0)
        {
            //Look directions
            double d = Math.PI * 2 / sample;
            var orientations = new List<double[]>();
            for (int i = 0; i < sample; i++)
            {
                orientations.Add(new double[]{
                    Math.Cos(baseAngleRad+(sample-i-1)*d),
                    Math.Sin(baseAngleRad+(sample-i-1)*d),
                    0
                });
            }
            return orientations;
        }

        public static double AngleBetween(double[] v1, double[] v2)
        {
            return Math.Acos(Dot(v1, v2) / (Norm(v1) * Norm(v2)));
        }

        static readonly Random r = new();

        public static double Unif(double a, double b)
        {
            return a + r.NextDouble() * (b - a);
        }
        public static List<int> RankBasedSelection(List<int> orderedIndexList, int numberOfPicks)
        {
            if (numberOfPicks > orderedIndexList.Count)
                throw new Exception("Can't select more items than available items !");
            var l = orderedIndexList.Select(v => v).ToList();
            var result = new List<int>();
            for (int i = 0; i < numberOfPicks; i++)
            {
                int mu = l.Count;
                int selected = mu - Convert.ToInt32(Math.Ceiling(Math.Sqrt(0.25 + 2 * Unif(1, mu * (mu + 1) / 2)) - 0.5));
                result.Add(l[selected]);
            }
            return result;
        }

        //Shuffle a list, stolen from https://stackoverflow.com/questions/273313/randomize-a-listt
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = r.Next(n + 1);
                (list[n], list[k]) = (list[k], list[n]);
            }
        }
        
        public static Exception Error(string func, string message)
        {
            return new Exception($@"[{func}]: " + message);
        }
    }

}