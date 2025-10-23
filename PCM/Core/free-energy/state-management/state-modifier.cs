using PCM.Core.Types;
using PCM.Core.SceneObjects;
using PCM.Core.FreeEnergy.InverseInferences;

namespace PCM.Core.FreeEnergy.State
{
    public static class StateModifier
    {
        public static AgentState ComputeStateFe(AgentState currentState)
        {
            currentState.stateFE = null;
            currentState.GetCurrentFreeEnergy();
            return currentState;
        }
        /// <summary>
        /// Compute the certainty and uncertainty for each agent towards each agent/object
        /// </summary>
        /// <param name="currentState"></param>
        /// <returns>a new state</returns>
        public static AgentState ComputeCertaintyUncertainty(AgentState currentState)
        {
            var resultState = currentState.ShallowCopy();
            (double certainty, double uncertainty)[][] certTable = new (double certainty, double uncertainty)[currentState.agentsIds.Length][];
            foreach (int agentIndex in currentState.agentsIds)
            {
                certTable[agentIndex] = new (double certainty, double uncertainty)[currentState.objectBodies.Length];
                ObjectBody agent = currentState.objectBodies[agentIndex];
                for (int i = 0; i < currentState.objectBodies.Length; i++)
                    certTable[agentIndex][i] = Utils.ComputeSensoryUncertainity(agent.BodyPosition.Center, agent.LookAt, currentState.objectBodies[i].BodyPosition.Center, i == agentIndex);
            }
            resultState.certTable = certTable;
            return resultState;
        }

        /// <summary>
        /// Compute the expected updated preferences for other agents based on
        /// their shown valence and the certainty table
        /// </summary>
        /// <param name="currentState"></param>
        /// <returns></returns>
        /*public static AgentState UpdateOtherpref(AgentState currentState)
        {
            var resultState = currentState.ShallowCopy();
            Dictionary<int, List<double>> updatedOthersPrefs = new();
            var agentId = currentState.currentAgentId;
            //double[] emoval = currentState.emotions.Select(e => e.val).ToArray();
            double[] emoval = new double[currentState.emotions.Length];
            for (var i = 0; i < emoval.Length; i++)
                emoval[i] = currentState.emotions[i].Physiological.Val;

            foreach (int agentIndex in currentState.agentsIds)
            {
                //TODO:                       [PCM VR DEMO 2021 AD HOCK MODIF]
                if (agentIndex != agentId && agentIndex != State.AgentState.playerId)
                {
                    double cert = currentState.certTable[agentId][agentIndex].certainty;
                    double[] certainty = new double[currentState.certTable[agentIndex].Length];
                    for (var i = 0; i < certainty.Length; i++)
                        certainty[i] = cert * currentState.certTable[agentIndex][i].certainty;
                    
                    // certainty[agentIndex] = (1  - cert) * certainty[agentIndex];
                    
                    double[] state = currentState.preferences[agentIndex];

                    var res = Core.Utils.Misc.UpdatePriorsV2(state, emoval[agentIndex] * currentState.emogain[agentIndex], certainty, currentState.sensoryEvidenceUpdateWeight);
                    //Fix: Self preference should not be updated
                    res[agentIndex] = currentState.preferences[agentIndex][agentIndex];
                    updatedOthersPrefs.Add(agentIndex, res.ToList());
                }
            }
            double[][] updatedPrefs = new double[currentState.agentsIds.Length][];
            updatedPrefs[agentId] = resultState.preferences[agentId].ToArray();
            foreach (var el in updatedOthersPrefs)
                updatedPrefs[el.Key] = el.Value.ToArray();
            resultState.postPreferences = updatedPrefs;
            return resultState;
        }*/

        public static AgentState UpdateOtherprefV2(AgentState currentState)
        {
            var resultState = currentState.ShallowCopy();
            Dictionary<int, List<double>> updatedOthersPrefs = new();
            var agentId = currentState.currentAgentId;
            //double[] emoval = currentState.emotions.Select(e => e.val).ToArray();

            foreach (int agentIndex in currentState.agentsIds)
            {
                if (agentIndex != agentId)
                {
                    double[] certainty = new double[currentState.certTable[agentIndex].Length];
                    var targetAgent = currentState.objectBodies[agentIndex];
                    for (var i = 0; i < certainty.Length; i++)
                    {
                        //certainty[i] = currentState.certTable[agentIndex][i].certainty;
                        certainty[i] = Utils.ComputeSensoryUncertainitySimplified(targetAgent.BodyPosition.Center,
                        targetAgent.LookAt, currentState.objectBodies[i].BodyPosition.Center, Utils.UpdateUncertFactor,
                        agentIndex == i).certainty;
                    }
                    double[] state = currentState.preferences[agentIndex];
                    var res = Core.Utils.Misc.UpdatePriorsV2(state, currentState.emotions[agentIndex].Felt.Val * currentState.emogain[agentIndex], certainty, currentState.sensoryEvidenceUpdateWeight);
                    //Fix: Self preference should not be updated
                    res[agentIndex] = currentState.preferences[agentIndex][agentIndex];
                    //TODO:[PCM VR DEMO 2021 AD HOCK MODIF]
                    //res = UpdateOtherprefS2(res, certainty, agentIndex);
                    res = NormalizePref(res);
                    updatedOthersPrefs.Add(agentIndex, res.ToList());
                }
            }
            double[][] updatedPrefs = new double[currentState.agentsIds.Length][];
            updatedPrefs[agentId] = resultState.preferences[agentId].ToArray();
            foreach (var el in updatedOthersPrefs)
                updatedPrefs[el.Key] = el.Value.ToArray();
            resultState.postPreferences = updatedPrefs;
            if (resultState.canInfer)
                return InverseInference.UpdateAgentBeliefs(resultState);
            return resultState;
        }

        public static double[] NormalizePref(double[] prior){
            double[] updatedPriors = new double[prior.Length];
            double sum = prior[2] + prior[3];
            updatedPriors[0] = prior[0];
            updatedPriors[1] = prior[1];
            updatedPriors[2] = prior[2] / sum;
            updatedPriors[3] = prior[3] / sum;
            return updatedPriors;
        }

        public static double[] UpdateOtherprefS2(double[] priors, double[] certainty, int agentIndex){
            const double cst = 0.5;
            double[] updatedPriors = new double[priors.Length];
            for (var i = 0; i < priors.Length; i++) {
                if ((i != agentIndex) && (priors[i] > 0.5 )){
                    updatedPriors[i] = priors[i] - (priors[i] - 0.5) * (1 - certainty[i]) * cst;
                }
                else{
                    updatedPriors[i] = priors[i];
                }
            }
            return updatedPriors;
        }


        /// <summary>
        /// Update the agent's own preferences based on the others updated preferences
        /// and balanced by a theory of mind factor
        /// </summary>
        /// <param name="currentState"></param>
        /// <returns></returns>
        public static AgentState UpdateSelfPref(AgentState currentState)
        {
            var resultState = currentState.ShallowCopy();

            //TODO: [PCM VR DEMO 2021 AD HOCK MODIF]
            // if(currentState.currentAgentId == State.AgentState.playerId){
            //     return resultState;
            // }
            var agentId = currentState.currentAgentId;
            double[] updatedPrefs = new double[currentState.preferences[0].Length];
            foreach (int agentIndex in currentState.agentsIds)
            {
                //Maybe TOM should be balanced with certainty ?
                updatedPrefs = updatedPrefs.Select((pref, index) => pref + currentState.tomUpdate[agentId][agentIndex] * currentState.postPreferences[agentIndex][index]).ToArray();
                
            }
            // Fix: self preference should not be updated
            //updatedPrefs = NormalisePref(updatedPrefs);
            updatedPrefs[agentId] = currentState.postPreferences[agentId][agentId];
            var copiedPostPrefs = new double[resultState.postPreferences.Length][];
            for (var i = 0; i < copiedPostPrefs.Length; i++)
            {
                copiedPostPrefs[i] = resultState.postPreferences[i];
            }
            copiedPostPrefs[agentId] = updatedPrefs;

            resultState.postPreferences = copiedPostPrefs;

            //Maybe it is not correct to update the preferences here:
            //Do the preferences really change when we project ourselves or not ?
            //If it is not the case, the state.preferences should be updated after
            //the decision of action
            //var copiedPrefs = new double[resultState.preferences.Length][];
            //for (var i = 0; i < copiedPrefs.Length; i++)
            //    copiedPrefs[i] = resultState.postPreferences[i];

            //copiedPrefs[agentId] = updatedPrefs;
            //resultState.preferences = copiedPrefs;

            return resultState;
        }


        public static AgentState UpdatePreferencesWithMutualLove(AgentState currentState){
            var resultState = currentState.ShallowCopy();
            Dictionary<int, Dictionary<int, double>> updatedPrefs = new();
            foreach (var agentId in currentState.agentsIds)
            {
                Dictionary<int, double> agentPrefs = new();
                foreach (var targetAgentId in currentState.agentsIds)
                {
                    if (targetAgentId != agentId)
                    {
                        var mutualLove = currentState.mutualLoveStep[agentId][targetAgentId];
                        if (mutualLove == 0)
                            continue;
                        var target = mutualLove < 0 ? 1 - currentState.preferences[targetAgentId][agentId] : currentState.preferences[targetAgentId][agentId];
                        var step = Math.Abs(mutualLove);
                        if (agentId == currentState.currentAgentId && agentId == 1 && targetAgentId == 0){
                            Console.WriteLine($"{currentState.preferences[agentId][targetAgentId]} > {target} | {currentState.preferences[targetAgentId][agentId]} || {mutualLove}");
                        }
                        if (target < currentState.preferences[agentId][targetAgentId])
                        {
                            var modif = -step + currentState.preferences[agentId][targetAgentId];
                            if (modif >= target)
                            {
                                agentPrefs.Add(targetAgentId, modif);
                            }else{
                                agentPrefs.Add(targetAgentId, target);
                            }
                        }
                        else if (target > currentState.preferences[agentId][targetAgentId])
                        {
                            var modif = step + currentState.preferences[agentId][targetAgentId];
                            if (modif <= target)
                            {
                                agentPrefs.Add(targetAgentId, modif);
                            }else{
                                agentPrefs.Add(targetAgentId, target);

                            }
                        }
                    }
                }
                updatedPrefs.Add(agentId, agentPrefs);
            }
            foreach(var kv in updatedPrefs){
                var agentId = kv.Key;
                foreach(var kv2 in kv.Value){
                    var targetId = kv2.Key;
                    resultState.postPreferences[agentId][targetId] = kv2.Value;
                }
            }
            return resultState;
        }


        
        /// <summary>
        /// SpatialStatAround v1 > weigthed mean on mu
        /// </summary>
        /// <param name="currentState"></param>
        /// <returns></returns>
        public static AgentState SpatialStatAround(AgentState currentState)
        {
            var resultState = currentState.ShallowCopy();
            (double fe, double energy, double entropy)[][] res = new (double fe, double energy, double entropy)[currentState.agentsIds.Length][];
            resultState.mu = new double[currentState.agentsIds.Length][];
            resultState.spatialStats = new double[currentState.agentsIds.Length][];
            var orientationsWeights = Core.Utils.Arrays.NormalizeArray(new double[]{
                    1.0,
                    0.0,
                    0.0,
                    0.0
                    // 1,1,1,1
            }).ToArray();
            //TODO: recompute uncertainty
            var selfSpatialStatCoef = 0.00001f;
            foreach (int agentIndex in currentState.agentsIds)
            {
                ObjectBody agent = currentState.objectBodies[agentIndex];
                (double fe, double energy, double entropy)[] energies = new (double fe, double energy, double entropy)[currentState.objectBodies.Length];

                var orientations = new Geom3d.Vertex[]{
                    agent.LookAt,
                    agent.LookAt.RotateY(Math.PI/2),
                    agent.LookAt.RotateY(-Math.PI/2),
                    agent.LookAt.RotateY(Math.PI)
                };

                double[] objectMus = new double[currentState.objectBodies.Length];
                double[] spatialStats= new double[currentState.objectBodies.Length];
                var agentPos = agent.GetEye(); //We always use the position of the real eye (we don't "turn" the body)
                for (var orientationIndex = 0; orientationIndex < orientations.Length; orientationIndex++)
                {
                    var orientation = orientations[orientationIndex];
                    for (var i = 0; i < currentState.objectBodies.Length; i++)
                    {
                        ObjectBody obj = currentState.objectBodies[i];
                        // double uncertainty = currentState.certTable[agentIndex][i].uncertainty;
                        // double uncertainty = Utils.ComputeSensoryUncertainity(agent.BodyPosition.Center, agent.LookAt, obj.BodyPosition.Center, i == agentIndex).uncertainty;
                        //double sigma = currentState.sevuncertaingain * uncertainty;
                        //Check if object is in front > convert target to agent coordinates
                        var target = obj.BodyPosition;
                        var agentLookAt = orientation;//agent.LookAt;
                        var transfVertices = new Geom3d.Vertex[target.Vertices.Length];
                        var visible = false;
                        var partHidden = false;
                        double delta = 0.1;
                        for (int j = 0; j < target.Vertices.Length; j++)
                        {
                            transfVertices[j] = target.Vertices[j].ChangeCoordinateSystem(agentPos, agentLookAt);
                            if (transfVertices[j].Z > 0)
                            {
                                visible = true;
                            }
                            if (transfVertices[j].Z < 0)
                            {
                                partHidden = true;
                                transfVertices[j].Z = 0;
                            }
                        }
                        if (visible)
                        {
                            if (partHidden)
                                foreach (var vert in transfVertices)
                                {
                                    vert.Z += delta;
                                }
                            var tTarget = new Geom3d.Polyhedron(transfVertices, target.Faces, target.Center.ChangeCoordinateSystem(agentPos, agentLookAt));
                            var spatialStat = SpatialStat.GetSpatialStat(tTarget) * orientationsWeights[orientationIndex];
                            if(agentIndex == i)
                            {
                                spatialStat *= selfSpatialStatCoef;
                            }
                            spatialStats[i] += spatialStat;
                            var mu = SpatialStat.GetMuWithSpatialStat(spatialStat, resultState.postPreferences[agentIndex][i], resultState.interactObjectIds[agentIndex] == i ? 1.1 : 1);
                            objectMus[i] = objectMus[i] + mu * orientationsWeights[orientationIndex];
                        }
                        else
                        {
                            objectMus[i] = objectMus[i] + 0.5 * orientationsWeights[orientationIndex]; //neutral mu.
                        }
      
                    }
                }

                resultState.mu[agentIndex] = objectMus;
                resultState.spatialStats[agentIndex] = spatialStats;

                //Now compute fe with the mu
                for (var i = 0; i < currentState.objectBodies.Length; i++)
                {
                    double uncertainty = currentState.certTable[agentIndex][i].uncertainty;
                    double sigma = currentState.sevuncertaingain * uncertainty;
                    var mu = objectMus[i];
                    //objectMus[i] = mu;
                    // energies[i] = PCM.FreeEnergy.Utils.GetPrecomputedFE(s, mu, sigma);
                    // Console.WriteLine(sigma+" "+i);
                    //sigma = 0.1;
                    energies[i] = (fe: MathUtils.DKL.FakeFe(mu, Math.Max(0.1, sigma)), energy: 0.0, entropy: 0.0);
                    energies[i] = (fe: MathUtils.DKL.FakeFe(mu, 0.1), energy: 0.0, entropy: 0.0);
                    if (agentIndex == currentState.currentAgentId)
                    {
                        energies[agentIndex] = (fe: 0, energy: 0, entropy: 0);
                    }
                }
                res[agentIndex] = energies;
                
            }
            resultState.fe = res;
            return resultState;
        }

        /// <summary>
        /// Update shown emotions and set postPref to pref
        /// </summary>
        /// <param name="currentState"></param>
        /// <returns></returns>
        public static AgentState UpdateEmotions(AgentState currentState)
        {
            var resultState = currentState.ShallowCopy();
            double neutralref = currentState.neutralref;
            var objectCount = currentState.objectBodies.Length;
            // if(currentState.currentAgentId == 2){
            //         Console.WriteLine($"{string.Join(",",currentState.mu)}");
            //     }

            var updatedEmotions = currentState.agentsIds.Select(agentIndex =>
            {
                var c = currentState.certTable[agentIndex].Select(cert => cert.certainty);
                double certSum = c.Sum();
                var certainty = c.Select(cert => cert / certSum).ToArray();
                double positivity = 0;
                double negativity = 0;
                for (int i = 0; i < currentState.mu[agentIndex].Length; i++)
                {
                    var mu = currentState.mu[agentIndex][i];
                    double v = (mu - neutralref) / (1 - neutralref) * currentState.spatialStats[agentIndex][i];
                    if (v < 0)
                        negativity -= v;
                    else if (v > 0)
                        positivity += v;
                }
                var spatialStatsSum = currentState.spatialStats[agentIndex].Sum();
                positivity /= spatialStatsSum;
                negativity /= spatialStatsSum;
                var arousal = resultState.fe[agentIndex].Average(v => v.energy);
                var surprise = Core.Utils.Arrays.MultArrays(certainty, Core.Utils.Arrays.SubArrays(currentState.postPreferences[agentIndex], currentState.preferences[agentIndex])).Average();
                var uncertainty = resultState.fe[agentIndex].Average(v => v.entropy);
                return new Emotion (positivity, negativity, arousal, surprise, uncertainty);
            }).ToArray();
            for (var i= 0; i<resultState.emotions.Length; i++)
            {
                resultState.emotions[i].Felt = updatedEmotions[i];
            }
            //resultState.emotions[resultState.currentAgentId].VoluntaryFacial.Pos = 0.8;
            //resultState.emotions[resultState.currentAgentId].VoluntaryFacial.Val = 0.8;
            resultState.UpdateFacialEmotion();
            resultState.UpdatePhysiologicalEmotion();
            // Console.WriteLine(string.Join(",",resultState.emotions.Select(emo => emo.neg)));
            resultState.preferences = resultState.postPreferences;

            return resultState;
        }
    }
}