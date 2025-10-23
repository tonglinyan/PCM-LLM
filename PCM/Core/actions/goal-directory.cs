using PCM.Core.FreeEnergy.State;
using PCM.Core.SceneObjects;

namespace PCM.Core.Actions
{
    //replaces action directory, when using goal predictions
    public class GoalDirectory
    {
        public List<Action> facialActions = new();
        public List<Action> bodyActions = new();
        public List<Goal> goals = new();
        private readonly int entityCount;
        private readonly int agentId;
        private readonly string[] actionTags;

        public GoalDirectory(string[] actionsTags, List<int> objectIds, int entityCount, int agentId)
        {
            this.entityCount = entityCount;
            this.agentId = agentId;
            actionTags = actionsTags;
            var actionDirectory = new ActionDirectory(actionsTags, objectIds, entityCount, 0);
            facialActions = actionDirectory.GetFacialActionList();
            UpdateGoals();
        }

        private void InitGoals()
        {
            goals = new();
            if (actionTags.Contains("walk"))
            {
                if (!actionTags.Contains("dynamic"))
                {
                    Goal idle = new();
                    Action idleAction = new((int aIndex, AgentState s) =>
                    {
                        return s;
                    }, ActionType.Idle);
                    idle.simulatedAction = idleAction;
                    idle.stepAction = idleAction;
                    goals.Add(idle);
                }
                for (int entityId = 0; entityId < entityCount; entityId++)
                {
                    var localEntityId = entityId;
                    if (agentId != localEntityId)
                    {
                        Goal approach = new()
                        {
                            simulatedAction = new Action((int aIndex, AgentState s) =>
                            {
                                var newState = s.ShallowCopy();

                                var positions = new ObjectBody[s.objectBodies.Length];
                                for (var i = 0; i < s.objectBodies.Length; i++)
                                {
                                    positions[i] = s.objectBodies[i].Copy();
                                }
                                var curPos = positions[aIndex];
                                curPos.RotateTowardsPoint(s.objectBodies[localEntityId].BodyPosition.Center);
                                double distance = s.objectBodies[s.currentAgentId].BodyPosition.Center.Distance(s.objectBodies[localEntityId].BodyPosition.Center) - 60;
                                curPos.MoveForward(distance);
                                if (s.targetIds[s.currentAgentId] != -1)
                                    positions[s.targetIds[s.currentAgentId]] = ActionDirectory.MoveObjectInFront(positions, aIndex, s.targetIds[s.currentAgentId]);
                                newState.objectBodies = positions;
                                newState.stepCountToReachGoal = (int)(distance / s.speed);
                                return newState;
                            }, ActionType.Walk, localEntityId),
                            stepAction = new Action((int aIndex, AgentState s) =>
                            {
                                var newState = s.ShallowCopy();
                                var positions = new ObjectBody[s.objectBodies.Length];
                                for (var i = 0; i < s.objectBodies.Length; i++)
                                {
                                    positions[i] = s.objectBodies[i].Copy();
                                }
                                var curPos = positions[aIndex];
                                curPos.RotateTowardsPoint(s.objectBodies[localEntityId].BodyPosition.Center);
                                double distance = s.objectBodies[s.currentAgentId].BodyPosition.Center.Distance(s.objectBodies[localEntityId].BodyPosition.Center);
                                double safeDist = distance - s.speed;
                                if (safeDist >= FreeEnergy.Constants.minDist)
                                    curPos.MoveForward(s.speed);
                                else
                                    curPos.MoveForward(distance - FreeEnergy.Constants.minDist);
                                if (s.targetIds[s.currentAgentId] != -1)
                                    positions[s.targetIds[s.currentAgentId]] = ActionDirectory.MoveObjectInFront(positions, aIndex, s.targetIds[s.currentAgentId]);
                                newState.objectBodies = positions;
                                return newState;
                            }, ActionType.Walk, localEntityId)
                        };
                        goals.Add(approach);
                        Goal moveAway = new()
                        {
                            simulatedAction = new Action((int aIndex, AgentState s) =>
                            {
                                var newState = s.ShallowCopy();

                                var positions = new ObjectBody[s.objectBodies.Length];
                                for (var i = 0; i < s.objectBodies.Length; i++)
                                {
                                    positions[i] = s.objectBodies[i].Copy();
                                }
                                var curPos = positions[aIndex];
                                curPos.RotateAwayFromPoint(s.objectBodies[localEntityId].BodyPosition.Center);
                                double distance = s.objectBodies[s.currentAgentId].BodyPosition.Center.Distance(s.objectBodies[localEntityId].BodyPosition.Center);
                                curPos.MoveForward(s.speed * 2);
                                if (s.targetIds[s.currentAgentId] != -1)
                                    positions[s.targetIds[s.currentAgentId]] = ActionDirectory.MoveObjectInFront(positions, aIndex, s.targetIds[s.currentAgentId]);
                                newState.objectBodies = positions;
                                newState.stepCountToReachGoal = 2;
                                return newState;
                            }, ActionType.Walk, localEntityId),
                            stepAction = new Action((int aIndex, AgentState s) =>
                            {
                                var newState = s.ShallowCopy();

                                var positions = new ObjectBody[s.objectBodies.Length];
                                for (var i = 0; i < s.objectBodies.Length; i++)
                                {
                                    positions[i] = s.objectBodies[i].Copy();
                                }
                                var curPos = positions[aIndex];
                                curPos.RotateAwayFromPoint(s.objectBodies[localEntityId].BodyPosition.Center);
                                double distance = s.objectBodies[s.currentAgentId].BodyPosition.Center.Distance(s.objectBodies[localEntityId].BodyPosition.Center);
                                curPos.MoveForward(s.speed);
                                if (s.targetIds[s.currentAgentId] != -1)
                                    positions[s.targetIds[s.currentAgentId]] = ActionDirectory.MoveObjectInFront(positions, aIndex, s.targetIds[s.currentAgentId]);
                                newState.objectBodies = positions;
                                return newState;
                            }, ActionType.Walk, localEntityId)
                        };
                        goals.Add(moveAway);
                    }
                }
            }
        }

        public void FacialUpdateGoals()
        {
            if (facialActions.Count == 0)
                return;
            List<Goal> newGoals = new();
            if (!actionTags.Contains("dynamic"))
                newGoals = goals.ToList();
            if (goals.Count == 0)
                goals.Add(new Goal());
            foreach (var goal in goals)
            {
                var (simulated, step) = (goal.simulatedAction, goal.stepAction);
                foreach (var facialAction in facialActions)
                {
                    var newGoal = goal.Copy();
                    newGoal.simulatedAction = new Action((int aIndex, AgentState s) =>
                    {
                        int stepCountToReachGoal = 1;
                        if (simulated != null)
                        {
                            s = simulated.Execute(aIndex, s);
                            stepCountToReachGoal = s.stepCountToReachGoal;
                        }
                        s = facialAction.Execute(aIndex, s);
                        if (s == null)
                            return null;
                        s.stepCountToReachGoal = stepCountToReachGoal;
                        return s;
                    }, (simulated == null) ? facialAction.GetActionType() : simulated.GetActionType());
                    newGoal.stepAction = new Action((int aIndex, AgentState s) =>
                    {
                        if (step != null)
                            s = step.Execute(aIndex, s);
                        return facialAction.Execute(aIndex, s);
                    }, (step == null) ? facialAction.GetActionType() : step.GetActionType());
                    newGoals.Add(newGoal);
                }
            }
            goals = newGoals;
        }
        public void UpdateGoals()
        {
            InitGoals();
            FacialUpdateGoals();
        }
    }
}