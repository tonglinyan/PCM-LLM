using PCM.Core.FreeEnergy.State;
using PCM.Core.SceneObjects;
using static PCM.Core.Utils.Tree;
using Newtonsoft.Json;
using System.Numerics;

namespace PCM.Core.Actions
{
    public class ActionDirectory
    {
        readonly int entityCount;
        string[] actionTags;
        public List<Node<Action[]>> _dfsList;
        List<Action[]> combinedActionList;
        readonly List<int> objectIds;
        public readonly int depth;
        public List<Action[]> CombinedActionList => combinedActionList;

        public ActionDirectory(string[] actionsTags, List<int> objectIds, int entityCount, int depth)
        {
            this.objectIds = objectIds;
            this.entityCount = entityCount;
            this.depth = depth;
            Update(actionsTags);
        }

        public void Update(string[] actionsTags)
        {
            actionTags = actionsTags;
            combinedActionList = (from facialAction in GetFacialActionList()
                                  from bodyAction in GetBodyActionList(objectIds)
                                  select new Action[] { bodyAction, facialAction }).ToList();
            _dfsList = InitDfsList(depth);
        }

        public static ObjectBody MoveObjectInFront(ObjectBody[] positions, int aIndex, int objid)
        {
            var dist = /* SceneObjects.DemoVR.Robot.depth / 2 */ +SceneObjects.DemoVR.Ball.depth / 2 + 1;
            var objectTargetPosition = positions[aIndex].BodyPosition.Center.Add(positions[aIndex].LookAt.Multiply(dist));
            ObjectBody obj = positions[objid].Copy();
            // Console.WriteLine(positions[aIndex].BodyPosition.Center+" "+ objectTargetPosition + " " + positions[objid].BodyPosition.Center);
            obj.BodyPosition.Translate_inplace(objectTargetPosition.Sub(positions[objid].BodyPosition.Center));
            obj.RotateTowardsDirection(positions[aIndex].LookAt);
            return obj;
        }
        public List<Action> GetFacialActionList()
        {
            List<Action> _facialActionList = new();
            var facialExpress = actionTags.Contains("facial_express");
            if (facialExpress || actionTags.Contains("spontaneous"))
                _facialActionList.Add(new Action((int aIndex, AgentState s) =>
                {
                    var newState = s.ShallowCopy();
                    newState.expressionSpontaneous = true;
                    newState.UpdateFacialEmotion();
                    return newState;
                }, ActionType.Spontaneous));
            if (facialExpress || actionTags.Contains("smile"))
                _facialActionList.Add(new Action((int aIndex, AgentState s) =>
                {
                    var newState = s.ShallowCopy();
                    newState.emotions[aIndex].VoluntaryFacial = new Types.Emotion()
                    {
                        Pos = 1,
                        Neg = 0,
                        Val = 1, 
                        Arousal = 0,
                        Surprise = 0,
                        Uncertainty = 0
                    };
                    newState.expressionSpontaneous = false;
                    newState.UpdateFacialEmotion();
                    return newState;
                }, ActionType.Smile));
            if (facialExpress || actionTags.Contains("grimace"))
                _facialActionList.Add(new Action((int aIndex, AgentState s) =>
                {
                    var newState = s.ShallowCopy();
                    newState.emotions[aIndex].VoluntaryFacial = new Types.Emotion()
                    {
                        Pos = 0,
                        Neg = 1,
                        Val = -1,
                        Arousal = 0,
                        Surprise = 0,
                        Uncertainty = 0
                    };
                    newState.expressionSpontaneous = false;
                    newState.UpdateFacialEmotion();
                    return newState;
                }, ActionType.Grimace));
            _facialActionList.Add(new Action((int aIndex, AgentState s) =>
            {
                var newState = s.ShallowCopy();
                newState.emotions[aIndex].VoluntaryFacial = new Types.Emotion()
                {
                    Pos = 0,
                    Neg = 0,
                    Arousal = 0,
                    Surprise = 0,
                    Uncertainty = 0
                };
                newState.expressionSpontaneous = false;
                newState.UpdateFacialEmotion();
                return newState;
            }, ActionType.Idle));
            return _facialActionList;
        }
        public List<Action> GetBodyActionList(List<int> objectIds)
        {
            List<Action> _bodyActionList = new();
            //stay still action - Useless since it already exist in look around actions
            //_bodyActionList.Add((int aIndex, AgentState s) => s.Copy());
            //Look directions. Very influential on the prediction time. Seek a good compromise
            int sample = 8;
            double d = Math.PI * 2 / sample;
            var orientations = new List<Geom3d.Vertex>();
            for (int i = 0; i < sample; i++)
            {
                 orientations.Add(new Geom3d.Vertex(
                     Math.Cos((sample - i - 1) * d),
                     0,
                     Math.Sin((sample - i - 1) * d)
                 ));
            }
            foreach (var orientation in orientations)
            {
                TryAddAction(_bodyActionList, new Action((int aIndex, AgentState s) =>
                {
                s.targetIds[aIndex] = -1;
                return AgentMovesTowardsDirection2(aIndex, s, orientation);
                }, ActionType.Walk), "walk");
                /*TryAddAction(_bodyActionList, new Action((int aIndex, AgentState s) =>
                {
                s.targetIds[aIndex] = -1;
                return AgentRotatesTowardsDirection(aIndex, s, orientation);
                }, ActionType.Rotate), "rotate");*/
            }
            for (int entityId = 0; entityId < entityCount; entityId++)
            {
                var localEntityId = entityId;
                /*TryAddAction(_bodyActionList, new Action((int aIndex, AgentState s) =>
                {
                    if (aIndex == localEntityId)
                        return null;
                    s.targetIds[aIndex] = localEntityId;
                    s = AgentMovesTowardsPoint(aIndex, s, s.objectBodies[localEntityId].BodyPosition.Center);
                    return s;
                }, ActionType.Walk, localEntityId), "walk");*/
                TryAddAction(_bodyActionList, new Action((int aIndex, AgentState s) =>
                {
                    if (aIndex == localEntityId)
                        return null;
                    s.targetIds[aIndex] = localEntityId;
                    s = AgentRotatesTowardsPoint(aIndex, s, s.objectBodies[localEntityId].BodyPosition.Center);
                    return s;
                }, ActionType.Rotate, localEntityId), "rotate");
                /*TryAddAction(_bodyActionList, new Action((int aIndex, AgentState s) =>
                {
                    if (aIndex == localEntityId)
                        return null;
                    s.targetIds[aIndex] = localEntityId;
                    var point = s.objectBodies[localEntityId].BodyPosition.Center;
                    s = AgentRotatesTowardsPoint(aIndex, s, point);
                    s.objectBodies[aIndex].LookAt = s.objectBodies[aIndex].PointToDir(point).Unit();
                    return s;
                }, ActionType.Stare, localEntityId), "stare");*/
            }
            if (actionTags.Contains("grab"))
                foreach (var objid in objectIds)
                {
                    _bodyActionList.Add(new Action((int aIndex, AgentState s) =>
                    {
                        var newState = s.ShallowCopy();
                        var grabbedIds = Utils.Copy.Copy1DArray(newState.interactObjectIds);
                        var positions = new ObjectBody[s.objectBodies.Length];
                        for (var i = 0; i < s.objectBodies.Length; i++)
                        {
                            positions[i] = s.objectBodies[i].Copy();
                        }
                        grabbedIds[s.currentAgentId] = objid;
                        positions[objid] = MoveObjectInFront(positions, aIndex, objid);
                        newState.objectBodies = positions;
                        newState.interactObjectIds = grabbedIds;
                        //Console.WriteLine($"{s.currentAgentId} can grab {objid}");
                        return newState;
                    }, ActionType.Grab, objid));
                }
            if (actionTags.Contains("let_go"))
                foreach (var objid in objectIds)
                {
                    _bodyActionList.Add(new Action((int aIndex, AgentState s) =>
                    {
                        if (s.targetIds[s.currentAgentId] == -1)
                            return s;

                        var newState = s.ShallowCopy();
                        var grabbedIds = Utils.Copy.Copy1DArray(newState.targetIds);
                        var positions = new ObjectBody[s.objectBodies.Length];
                        for (var i = 0; i < s.objectBodies.Length; i++)
                        {
                            positions[i] = s.objectBodies[i].Copy();
                        }
                        grabbedIds[s.currentAgentId] = -1;
                        positions[aIndex].BodyPosition.Center.Y = DemoVR.Ball.height / 2;
                        newState.objectBodies[objid] = MoveObjectInFront(positions, aIndex, objid);
                        newState.targetIds = grabbedIds;
                        return newState;
                    }, ActionType.LetGo, objid));
                };
            if (!actionTags.Contains("dynamic") || _bodyActionList.Count() == 0)
                _bodyActionList.Add(new Action((int aIndex, AgentState s) =>
                {
                    return s;
                }, ActionType.Idle));
            return _bodyActionList;
        }


        public AgentState AgentMovesTowardsDirection2(int aIndex, AgentState s, Geom3d.Vertex orientation){
            var newState = s.ShallowCopy();

            var positions = new ObjectBody[s.objectBodies.Length];
            for (var i = 0; i < s.objectBodies.Length; i++)
            {
                positions[i] = s.objectBodies[i].Copy();
            }
            var curPos = positions[aIndex];
            curPos.RotateTowardsDirection(orientation);

            var speed = s.speed;
            if (Math.Abs(Math.Abs(orientation.X)-Math.Abs(orientation.Z)) < 0.5)
                speed = speed * Math.Sqrt(2);
            curPos.MoveForward(speed);
            if (s.interactObjectIds[s.currentAgentId] != -1)
                positions[s.interactObjectIds[s.currentAgentId]] = MoveObjectInFront(positions, aIndex, s.interactObjectIds[s.currentAgentId]);
            for (int entityId = 0; entityId < s.objectBodies.Length; entityId++)
            {
                if (entityId != aIndex)
                {
                    var dist = curPos.BodyPosition.Center.DistanceXZ(positions[entityId].BodyPosition.Center);
                    if (dist <= FreeEnergy.Constants.minDist)
                        return s;
                }
            }
            newState.objectBodies = positions;
            Geom3d.Vertex lookAt = aIndex == 0 ? new Geom3d.Vertex (0, 0, 1): new Geom3d.Vertex (0, 0, -1); 
            curPos.RotateTowardsDirection(lookAt);//s.objectBodies[aIndex].LookAt);
            return newState;
        }

        private AgentState AgentMovesTowardsDirection(int aIndex, AgentState s, Geom3d.Vertex dir)
        {
            var pos = dir.Multiply(s.speed);
            return AgentMovesTowardsPoint(aIndex, s, pos);
        }

        private AgentState AgentMovesTowardsPoint(int aIndex, AgentState s, Geom3d.Vertex point)
        {
            var newState = s.ShallowCopy();
            var positions = new ObjectBody[s.objectBodies.Length];
            for (var i = 0; i < s.objectBodies.Length; i++)
            {
                positions[i] = s.objectBodies[i].Copy();
            }
            var curPos = positions[aIndex];
            curPos.RotateTowardsPoint(point);
            var dist = curPos.BodyPosition.Center.DistanceXZ(point);
            var safeSpeed = Math.Max(dist - FreeEnergy.Constants.minDist, 0);
            var finalSpeed = Math.Min(s.speed, safeSpeed);
            if (finalSpeed == 0)
                return s;
            //for (int i = 0; i < s.objectBodies.Length; i++)
            //{
            //    if (aIndex != i && finalSpeed >= dist - PCM.FreeEnergy.Constants.minDist && finalSpeed <= dist + PCM.FreeEnergy.Constants.minDist)
            //    {
            //        return null;
            //    }
            //}
            curPos.MoveForward(finalSpeed);
            if (s.interactObjectIds[s.currentAgentId] != -1)
                positions[s.interactObjectIds[s.currentAgentId]] = MoveObjectInFront(positions, aIndex, s.interactObjectIds[s.currentAgentId]);
            newState.objectBodies = positions;
            return newState;
        }
        private AgentState AgentRotatesTowardsDirection(int aIndex, AgentState s, Geom3d.Vertex dir)
        {
            var newState = s.ShallowCopy();
            var positions = new ObjectBody[s.objectBodies.Length];
            for (var i = 0; i < s.objectBodies.Length; i++)
            {
                positions[i] = s.objectBodies[i].Copy();
            }
            var currentPos = positions[aIndex];
            currentPos.RotateTowardsDirection(dir);
            if (s.interactObjectIds[s.currentAgentId] != -1)
                positions[s.interactObjectIds[s.currentAgentId]] = MoveObjectInFront(positions, aIndex, s.interactObjectIds[s.currentAgentId]);
            newState.objectBodies = positions;
            return newState;
        }

        public AgentState AgentRotatesTowardsPoint(int aIndex, AgentState s, Geom3d.Vertex point)
        {
            var dir = s.objectBodies[aIndex].PointToDir(point);
            return AgentRotatesTowardsDirection(aIndex, s, dir);
        }

        private delegate AgentState AgentMovesFunction(int aIndex, AgentState s, Geom3d.Vertex dir);
        private class ActionMove
        {
            public readonly string configName;
            public readonly ActionType type;
            public readonly AgentMovesFunction usedByAgent;

            public ActionMove(string configName, ActionType type, AgentMovesFunction usedByAgent) {
                this.configName = configName;
                this.type = type;
                this.usedByAgent = usedByAgent;
            }
        }
        private void TryAddAction(List<Action> actionList, Action action, string tag)
        {
            if (actionTags.Contains(tag))
                actionList.Add(action);
        }

        public List<Node<Action[]>> InitDfsList(int depth)
        {
            var dfsList = new List<Node<Action[]>>();
            var nodesPerDepthLevel = new List<List<Node<Action[]>>>();
            var root = new Node<Action[]>(new Action[] { new(null, ActionType.Idle) });
            nodesPerDepthLevel.Add(new List<Node<Action[]>> { root });
            //Console.WriteLine("actions");
            //Console.WriteLine(JsonConvert.SerializeObject(nodesPerDepthLevel));
            //Generate the action tree
            for (var i = 1; i <= depth; i++)
            {
                var currentLevelNodes = new List<Node<Action[]>>();
                foreach (var parent in nodesPerDepthLevel[i - 1])
                {
                    foreach (Action[] combinedAction in combinedActionList)
                    {
                        var a = new Node<Action[]>(combinedAction);
                        parent.AddChild(a);
                        currentLevelNodes.Add(a);
                    }
                }
                nodesPerDepthLevel.Add(currentLevelNodes);
                //Console.WriteLine("actions");
                //Console.WriteLine(JsonConvert.SerializeObject(nodesPerDepthLevel));
            }
            dfsList.Add(root);
            return dfsList;
        }
    }


}