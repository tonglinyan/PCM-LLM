using PCM.Core.FreeEnergy.State;
namespace PCM.Core.FreeEnergy.Prediction
{
    public static class Base
    {
        public static FreeEnergy Fe = new();

        public static int[] AdjustPredictionLevel(int[] tomPredict, int agentIndex) => tomPredict.Select(val => Math.Min(tomPredict[agentIndex] - 1, val)).ToArray();

        public static AgentState GetNextExpectedMove(List<AgentState> prediction)
        {
            return prediction[1];
        }

        public static AgentState DoActionIfPossibleImproved(int agentIndex, AgentState s, Actions.Action action)
        {
            if (action.GetActionType() == Actions.ActionType.LetGo)
            {
                if (s.targetIds[s.currentAgentId] == -1 || s.targetIds[s.currentAgentId] != action.GetTargetId())
                    return null;
            }
            if (action.GetActionType() == Actions.ActionType.Grab)
            {
                var targetId = action.GetTargetId();
                var dist = s.objectBodies[agentIndex].BodyPosition.Center.Distance(s.objectBodies[targetId].BodyPosition.Center);
                var grabbedBySomeone = s.targetIds.Contains(targetId);

                if (dist > Constants.minDist || grabbedBySomeone || s.targetIds[s.currentAgentId] != -1)
                {
                    return null;
                }
            }
            var outstate = action.Execute(agentIndex, s);
            if (outstate == null)
                return null;
            var outpos = outstate.objectBodies[agentIndex];
            var inpos = s.objectBodies[agentIndex];
            if (action.GetActionType() == Actions.ActionType.Rotate && inpos.LookAt.Equals(outpos.LookAt))
            {
                return outstate;
            }
            var colliderPoints = new List<Geom3d.Vertex>();
            colliderPoints.AddRange(inpos.BodyPosition.GetBottomFaceVertices());
            colliderPoints.AddRange(outpos.BodyPosition.GetBottomFaceVertices());
            var agentCollider = new SimplePhysics.Collider.Convex2DPolygonCollider(SimplePhysics.Collider.ConvexHull2D(colliderPoints));
            //Check collider against other colliders
            //TODO: Temp (removed collisions)
            // for (var objectIndex = 0; objectIndex < s.objectBodies.Length; objectIndex++)
            //     if (objectIndex != agentIndex && objectIndex != outstate.grabbedEntityId)
            //     {
            //         var otherCollider = new SimplePhysics.Collider.Convex2DPolygonCollider(new Geom3d.Polygon(s.objectBodies[objectIndex].BodyPosition.GetBottomFaceVertices().ToArray()));
            //         if (agentCollider.collides(otherCollider))
            //         {
            //             return null;
            //         }
            //     }
            return outstate;
        }
    }
}