namespace PCM.Core.MathUtils{
    public static class MVN{
        /*
         *  Could use Accord package for that, but it is not fully compatible
         *  with unity. Also, we are in a simplified case and this code runs
         *  faster.
         */
        public class MultivariateNormalDistributionCustom
        {
            //static readonly double sqrt2PI = Math.Sqrt(Math.PI * 2);
            static readonly double PITimes2 = Math.PI * 2.0;
            readonly double[] mu;
            readonly double[][] sigmaInv;
            readonly int k;
            readonly double coeff;
            public MultivariateNormalDistributionCustom(double[] mu, double[][] sigma)
            {
                this.mu = mu;
                k = this.mu.Length;
                try
                {
                    var det = Mat2x2Det(sigma);
                    if (det <= 0)
                    {
                        throw new Exception();
                    }
                    sigmaInv = Mat2x2Inv(sigma, det);
                    //this.coeff = 1 / (Math.Pow(sqrt2PI, this.k) * Math.Sqrt(det));

                    //Since k is always eq 2 in our case, we can simplify the formula
                    coeff = 1 / (PITimes2 * Math.Sqrt(det));
                }
                catch (Exception)
                {
                    coeff = 0.0;
                    sigmaInv = new double[k][];
                    for (var i = 0; i < sigmaInv.Length; i++)
                        sigmaInv[i] = new double[k];
                    //this.sigmaInv = new double[k][].Select(v => new double[k]).ToArray();
                }
            }
            public double ProbabilityDensityFunction(double[] x)
            {

                /*
                double P = 0;
                for (var i = 0; i < this.k; i++)
                {
                    double sum = 0;
                    for (var j = 0; j < this.k; j++)
                    {
                        sum += this.sigmaInv[i][j] * (x[j]-mu[j]);
                    }
                    P += (x[i] - mu[i]) * sum;
                }
                return this.coeff*Math.Exp(P/-2);
                */
                //We know that k is always 2 so we can optimize the code to run faster by removing the loop
                double d0 = x[0] - mu[0];
                double d1 = x[1] - mu[1];
                return coeff * Math.Exp(((d0 * sigmaInv[0][0] + d1 * sigmaInv[0][1]) * d0 +
                    (d0 * sigmaInv[1][0] + d1 * sigmaInv[1][1]) * d1) / -2.0);
            }
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
            var mvnd = new MultivariateNormalDistributionCustom(mu, sigma);
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

        public static double Mat2x2Det(double[][] mat)
        {
            if (mat.Length != 2 || mat[0].Length != 2)
                throw new Exception("Matrix has to be 2 by 2");
            return mat[0][0] * mat[1][1] - mat[0][1] * mat[1][0];
        }
        public static double[][] Mat2x2Inv(double[][] mat, double det)
        {
            if (mat.Length != 2 || mat[0].Length != 2)
                throw new Exception("Matrix has to be 2 by 2");
            var d = 1 / det;
            return new double[][]
            {
                new double[]{mat[1][1]*d,-mat[0][1]*d},
                new double[]{-mat[1][0] * d, mat[0][0] * d }
            };
        }

    }
}