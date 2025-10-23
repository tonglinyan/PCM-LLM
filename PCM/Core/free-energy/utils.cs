using System.Diagnostics;
using PCM.Core.Geom3d;

namespace PCM.Core.FreeEnergy
{
    public static class Utils
    {

        public static double SigmaDistanceFactor;
        public static double SigmaSharpnessFactor;
        public static double UpdateUncertFactor;
        /*
         *  Static values used/set when precomputing
         */
        private static (double[][] mvnpd_psi_s_j_m, double[][] mvnpd_psi_mu_j)[] _precomputedJoints;
        public static (double fe, double energ, double entrop)[][] _precomputedFe;
        private static double _maxSigma;

        //============================================================================
        // MANAGEMENT FUNCTIONS : Functions used to precompute, save, load etc. free
        // energy and/or joint probabilities
        //============================================================================

        /// <summary>
        /// Precompute the joint probabilities
        /// </summary>
        /// <param name="maxSigma">max value of sigma (0-maxSigma)</param>
        /// <param name="nSigma">number of sigma in the 0-m
        /// axSigma range</param>
        public static void PrecomputeJoints(double maxSigma, int nSigma)
        {
            /*
             * Expected memory size: 
             * npoints_in_dist (default 100x100) * size * 2 (2 distributions) * 8 bytes
             * => 100*100 * 10000 *2 * 8 / 1'048'576 = 1'525MB = 1.5GB
             * (equivalent to 1000*1000 distribution points * 100 sigmas)
             * Approximating that way gives far better perf; fe precision to  4-5 decimals
             */
            Console.WriteLine("Precomputing joints..." + maxSigma + " / " + nSigma);
            var sw = new Stopwatch();
            sw.Start();
            double minSigma = 0;
            _maxSigma = maxSigma;
            var sigmas = MathUtils.Misc.LinSpace(minSigma, maxSigma, nSigma);
            var result = new (double[][] mvnpd_psi_s_j_m, double[][] mvnpd_psi_mu_j)[nSigma];
            for (int i = 0; i < nSigma; i++)
            {
                Console.Write("\r{0}%", (int)((double)i / (sigmas.Length - 1) * 100.0));
                result[i] = ComputeJoints(sigmas[i]);
            }
            sw.Stop();
            Console.WriteLine("... done ! [" + sw.Elapsed.TotalSeconds + "s]");
            _precomputedJoints = result;
        }

        /// <summary>
        /// Retrieve the precomputed joint probabilities for a given sigma
        /// </summary>
        /// <param name="sigma"></param>
        /// <returns></returns>
        public static (double[][] mvnpd_psi_s_j_m, double[][] mvnpd_psi_mu_j) GetPrecomputedJoint(double sigma)
        {
            int index = (int)Math.Round(sigma * ((_precomputedJoints.Length - 1) / _maxSigma));
            return _precomputedJoints[index];
        }

        /// <summary>
        /// Precompute the free energy (free energy, energy and entropy)
        /// </summary>
        public static void PrecomputeFE()
        {
            if (_precomputedJoints == null)
            {
                throw new Exception("Precomputing joint probabilities is required to precompute free energy");
            }
            Console.WriteLine("Precomputing Fe...");
            var sw = new Stopwatch();
            sw.Start();
            var size = _precomputedJoints[0].mvnpd_psi_mu_j.Length;
            //Here, we assume that s = mu, that's why the result is a 1d array
            (double fe, double energ, double entrop)[][] fe = new (double fe, double energ, double entrop)[_precomputedJoints.Length][];
            for (int sigma = 0; sigma < _precomputedJoints.Length; sigma++)
            {
                Console.Write("\r{0}%", (int)(sigma / (double)(_precomputedJoints.Length - 1) * 100.0));
                (double fe, double energ, double entrop)[] tmp = new (double fe, double energ, double entrop)[size];
                var (mvnpd_psi_s_j_m, mvnpd_psi_mu_j) = _precomputedJoints[sigma];
                for (int i = 0; i < size; i++)
                {
                    tmp[i] = ComputeFEWithParams(i, i, mvnpd_psi_s_j_m, mvnpd_psi_mu_j);
                }
                fe[sigma] = tmp;
            }

            _precomputedFe = fe;
            sw.Stop();
            Console.WriteLine("... done ! [" + sw.Elapsed.TotalSeconds + "s]");
            _precomputedJoints = null;
        }

        /// <summary>
        /// Allow to save the precomputed free energy in a file.
        /// </summary>
        /// <param name="filename"></param>
        public static void WriteFEToFile(string filename)
        {
            if (_precomputedFe == null)
            {
                throw new Exception("Precomputing free energy is required to save it to a file.");
            }
            using (StreamWriter sr = new(filename))
            {
                System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)Thread.CurrentThread.CurrentCulture.Clone();
                customCulture.NumberFormat.NumberDecimalSeparator = ".";
                Thread.CurrentThread.CurrentCulture = customCulture;
                //Write the headers
                sr.WriteLine(_precomputedFe.Length + "," + _precomputedFe[0].Length + "," + _maxSigma);
                foreach (var sigmaTable in _precomputedFe)
                {
                    sr.WriteLine("_");
                    foreach (var fe in sigmaTable)
                        sr.WriteLine(fe.fe + "," + fe.energ + "," + fe.entrop);
                }
                Console.WriteLine("{0} written", filename);
            }
        }

        /// <summary>
        /// Load free energy from a file
        /// </summary>
        /// <param name="filename"></param>
        public static void LoadFEFromFile(string filename)
        {
            using (StreamReader file = new(filename))
            {
                //Retrieve the header
                string ln = file.ReadLine();
                var options = ln.Split(',');
                int nbOfSigma = int.Parse(options[0], System.Globalization.CultureInfo.InvariantCulture);
                int nbOfValues = int.Parse(options[1], System.Globalization.CultureInfo.InvariantCulture);
                _maxSigma = int.Parse(options[2], System.Globalization.CultureInfo.InvariantCulture);

                //Retrieve the free energy values
                _precomputedFe = new (double fe, double energ, double entrop)[nbOfSigma][];
                int currentSigma = -1;
                int currentVal = 0;
                while ((ln = file.ReadLine()) != null)
                {
                    if (ln[0] == '_')
                    {
                        currentSigma++;
                        currentVal = 0;
                        _precomputedFe[currentSigma] = new (double fe, double energ, double entrop)[nbOfValues];
                    }
                    else
                    {
                        var vals = ln.Split(",").Select(v => double.Parse(v, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                        _precomputedFe[currentSigma][currentVal] = (fe: vals[0], energ: vals[1], entrop: vals[2]);
                        currentVal++;
                    }
                }
                file.Close();
                // Console.WriteLine("{0} Loaded - FE ready", filename);
            }
        }


        public static (double fe, double energ, double entrop) GetPrecomputedFE(double s, double mu, double sigma)
        {
            //Assume mu = s
            int index = (int)Math.Round(sigma * ((_precomputedFe.Length - 1) / _maxSigma));
            int n = _precomputedFe[0].Length;
            int iemu = (int)Math.Round(n * mu, MidpointRounding.AwayFromZero);
            iemu = iemu == 0 ? 1 : iemu;
            iemu -= 1;
            // Console.WriteLine(mu+" "+sigma+" "+_precomputedFe[index][iemu]);
            return _precomputedFe[index][iemu];
        }

        /// <summary>
        /// Compute joint probabilities for a given sigma
        /// </summary>
        /// <param name="sigma"></param>
        /// <returns></returns>
        public static (double[][] mvnpd_psi_s_j_m, double[][] mvnpd_psi_mu_j) ComputeJoints(double sigma)
        {
            if (_precomputedJoints != null)
                return GetPrecomputedJoint(sigma);

            double e_mu = 0.9;
            double vr_mu = 0.001 * (1 + sigma);
            double e_psi = e_mu;
            double vr_psi = 0.001 * (1 + sigma);
            double cvr_psi_mu = 0.0002;
            double e_psi_m = e_mu;
            double vr_psi_m = 0.001;// * (1 + sigma);
            double e_s_m = e_mu;
            double vr_s_m = 0.001;// * (1 + sigma);
            double cvr_psi_s_m = 0.0002;
            double[][] Mcvr_psi_s_m = {
                new double[]{vr_psi_m,cvr_psi_s_m},
                new double[]{cvr_psi_s_m,vr_s_m}
            };
            double[] Me_psi_s_m = { e_psi_m, e_s_m };
            double[][] Mcvr_psi_mu ={
                new double[]{vr_psi, cvr_psi_mu},
                new double[]{ cvr_psi_mu, vr_mu }
            };
            double[] Me_psi_mu = { e_psi, e_mu };
            int nVal = Constants.nPsyS;
            double[] vpsi = MathUtils.Misc.LinSpace(0, 1, nVal);
            double[] vmu = MathUtils.Misc.LinSpace(0, 1, nVal);
            double[] vs = MathUtils.Misc.LinSpace(0, 1, nVal);
            (double[][] mvnpd_psi_s_j_m, double sum_psi_s_j_m) = MathUtils.MVN.MultivariateNormalProbabilityDensityFunction(vpsi, vs, Me_psi_s_m, Mcvr_psi_s_m);
            Core.Utils.Arrays.IPMult2DArray(mvnpd_psi_s_j_m, 1 / sum_psi_s_j_m);
            double[][] mvnpd_psi_mu_j = new double[nVal][];
            double sum_psi_mu_j = 0;
            for (int i = 0; i < nVal; i++)
            {
                Me_psi_mu[0] = Me_psi_mu[1] = vmu[i];
                (double[][] mvnpd, double sum) = MathUtils.MVN.MultivariateNormalProbabilityDensityFunction(new double[] { vmu[i] }, vpsi, Me_psi_mu, Mcvr_psi_mu);
                mvnpd_psi_mu_j[i] = mvnpd[0];
                sum_psi_mu_j += sum;
                //TODO: this might be a mistake, maybe mvnpd has to be divided by its sum, not by the sum of the whole matrix? to check
                // mvnpd = mvnpd/sum
            }
            Core.Utils.Arrays.IPMult2DArray(mvnpd_psi_mu_j, 1 / sum_psi_mu_j);
            return (mvnpd_psi_s_j_m, mvnpd_psi_mu_j);
        }

        /// <summary>
        /// Compute the free energy for a given s and mu given the joint probabilities
        /// </summary>
        /// <param name="s"></param>
        /// <param name="mu"></param>
        /// <param name="mvnpd_psi_s_j_m"></param>
        /// <param name="mvnpd_psi_mu_j"></param>
        /// <returns></returns>
        public static (double fe, double energ, double entrop) ComputeFe(double s, double mu, double[][] mvnpd_psi_s_j_m, double[][] mvnpd_psi_mu_j)
        {
            int n = mvnpd_psi_s_j_m.Length;
            int iemu = (int)Math.Round(n * mu, MidpointRounding.AwayFromZero);
            //iemu = iemu == 0 ? 1 : iemu; <- Used in matlab, useless here
            int ies = (int)Math.Round(n * s, MidpointRounding.AwayFromZero);
            //ies = ies == 0 ? 1 : ies; <- Used in matlab, useless here



            //marginal densities
            //generative density
            double[] pgen = mvnpd_psi_s_j_m[ies];
            //variational density
            double[] pjvar = mvnpd_psi_mu_j[iemu];

            //original 3 loops version
            //double pmu = pjvar.Sum();
            //double[] pvar = pjvar.Select(v => v / pmu).ToArray();
            //double energ = -pvar.Zip(pgen, (first, second) => first * Math.Log(second)).Sum();
            //double entrop = -pvar.Zip(pvar, (first, second) => first * Math.Log(second)).Sum();
            //double fe = energ - entrop;
            //return (fe, energ, entrop);

            //1 loop version    
            double entropt1 = 0;
            double sumpjvar = 0;
            double energ = 0;
            for (var i = 0; i < pjvar.Length; i++)
            {
                var pvar_i = pjvar[i];
                sumpjvar += pjvar[i];
                var pgen_i = pgen[i];
                energ -= pvar_i * Math.Log(pgen_i);
                entropt1 -= pvar_i * Math.Log(pvar_i);

            }
            energ /= sumpjvar;
            double entrop = entropt1 / sumpjvar + Math.Log(sumpjvar);

            double fe = energ - entrop; //facteur devant entrop (learning rate)
            return (fe, energ, entrop);
        }

        /// <summary>
        /// Same as ComputeFE but used with ies/iemu as parameters => used for precomputing
        /// </summary>
        /// <param name="ies"></param>
        /// <param name="iemu"></param>
        /// <param name="mvnpd_psi_s_j_m"></param>
        /// <param name="mvnpd_psi_mu_j"></param>
        /// <returns></returns>
        public static (double fe, double energ, double entrop) ComputeFEWithParams(int ies, int iemu, double[][] mvnpd_psi_s_j_m, double[][] mvnpd_psi_mu_j)
        {
            double[] pgen = mvnpd_psi_s_j_m[ies];
            double[] pjvar = mvnpd_psi_mu_j[iemu];
            double entropt1 = 0;
            double sumpjvar = 0;
            double energ = 0;
            for (var i = 0; i < pjvar.Length; i++)
            {
                var pvar_i = pjvar[i];
                sumpjvar += pjvar[i];
                var pgen_i = pgen[i];
                energ -= pvar_i * Math.Log(pgen_i);
                entropt1 -= pvar_i * Math.Log(pvar_i);

            }
            energ /= sumpjvar;
            double entrop = entropt1 / sumpjvar + Math.Log(sumpjvar);

            double fe = energ - entrop;
            return (fe, energ, entrop);
        }

        /// <summary>
        /// Compute the sensory certainty/uncertainty for an "agent" towards a "target"
        /// </summary>
        /// <param name="agentPos">Agent position</param>
        /// <param name="agentLookAt">Agent look at vector</param>
        /// <param name="targetPos">Target position</param>
        /// <returns></returns>
        public static (double certainty, double uncertainty) ComputeSensoryUncertainity(Vertex agentPos, Vertex agentLookAt, Vertex targetPos, bool targetIsSelf)
        {
            /*
             * Compute uncertainty of sensory evidence. Uncertainty increases with the distance between the agent and the target and with
             * the excentricity between the target and the looking direction of the agent
             * The target is the expected target, not the current target            
             */
            var agent = (position: new double[] { agentPos.X, agentPos.Z }, lookAt: new double[] { agentLookAt.X, agentLookAt.Z });
            var target =  new double[] { targetPos.X, targetPos.Z };
            var _las = Core.Utils.Misc.Norm(agent.lookAt);
            var normalizedCurrentLookAt = agent.lookAt.Select(v => v / _las).ToArray();//PCM.Utils.NormalizedLookAt(agent.position, agent.lookAt);
            var targetLookAt = Core.Utils.Arrays.SubArrays(target, agent.position);
            var targetDistance = Core.Utils.Misc.Norm(targetLookAt);
            targetDistance = Math.Abs(targetDistance) < float.Epsilon ? 1 : targetDistance;
            var normalizedTargetLookAt = new double[targetLookAt.Length];
            //targetLookAt.Select(v => v / targetDistance).ToArray();
            for (var i = 0; i < targetLookAt.Length; i++)
            {
                normalizedTargetLookAt[i] = targetLookAt[i] / targetDistance;
            }

            //Project the position of the target on the perspective line
            var projectedPoint = Core.Utils.Misc.Project(target, agent.position, Core.Utils.Arrays.AddArrays(agent.position, agent.lookAt));
            var projectedVector = Core.Utils.Arrays.SubArrays(projectedPoint, agent.position);
            var projectedDistance = Core.Utils.Misc.Norm(projectedVector);

            double dot = targetIsSelf ? 1 : Core.Utils.Misc.Dot(normalizedTargetLookAt, normalizedCurrentLookAt);
            double wtar = SigmaDistanceFactor; //1;
            int w0 = 0;
            double k = SigmaSharpnessFactor;//20; //sharpness factor
            double w = wtar * projectedDistance + w0;
            //don't consider w < 1
            w = w < 1 ? 1 : w;
            double excentricity = 1 - (dot + 1) / 2;
            double acuity = Math.Exp(-k * excentricity);
            double cert = 2 * acuity / (w + 1);

            //TODO: Fix > certainty should never be 1
            // cert = Math.Min(cert, 0.999);
            double uncert = 1 - cert;
            // double lambda = 1000;

            // double uncert = 1 / (1 + Math.Exp(-lambda / cert));

            //TODO: Fix > certainty should never be 1
            // cert = Math.Min(cert, 0.999);
            if (double.IsNaN(cert))
                throw new Exception("Certitude can not be NaN");
            return (certainty: cert, uncertainty: uncert);
        }

        public static (double certainty, double uncertainty) ComputeSensoryUncertainitySimplified(Vertex agentPos, Vertex agentLookAt, Vertex targetPos, double updateFactor, bool targetIsSelf){
            if(targetIsSelf)
                return (certainty: 1, uncertainty: 0);
            var k = 1 / (Math.PI / updateFactor); //4
            var theta = MathUtils.Misc.AngleBetween(agentPos, agentLookAt, targetPos);
            var cert = Math.Exp(-theta * k);
            return (certainty: cert, uncertainty: 1 - cert);
        }
    }
}