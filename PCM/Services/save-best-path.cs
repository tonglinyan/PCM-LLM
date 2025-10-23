using PCM.Core.FreeEnergy.State;
using static PCM.Core.Utils.Tree;
using MongoDB.Bson;
using MongoDB.Driver;
using PCM.Core.Actions;

namespace PCM.Core.Services
{
    public class SaveBestPath
    {
        // Static variable to hold the collection name across calls

        public static void SaveBestResultToMongoDB(List<AgentState> states, string filePath)
        {
            
            // Initialize the MongoDB client and get the database
            var client = new MongoClient("mongodb://root:root@172.27.10.131:7001");
            //var client = new MongoClient("mongodb://127.0.0.1:27017");
            //var client = new MongoClient(Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING"));
            var database = client.GetDatabase("trajectories");

            // TODO: if dont do the prediction, no node, generate it with the two given states
            var root = states[0].node;

            var agentId = root.value.agentState.currentAgentId;
            var collectionName = $"{filePath}_{agentId}";

            // Get the collection with the shared name
            var collection = database.GetCollection<BsonDocument>(collectionName);

            var document = SaveNodeRec(root, 0);
            //string tree = document.ToJson();

            if (!document.Contains("_id"))
            {
                var id = collectionName.Split("simulation")[1];
                document["_id"] = $"simulation{id}";
            }

            var existingDocument = collection.Find(new BsonDocument("_id", document["_id"])).FirstOrDefault();
            if (existingDocument == null)
            {
                collection.InsertOne(document);
            }
            else
            {
                Console.WriteLine("Document with the same _id already exists. Skipping insert.");
            }
        }

        private static BsonDocument SaveNodeRec(Node<AgentStateNode> node, int depth)
        {

            var state = node.value.agentState.ShallowCopy();
            BsonDocument document = new BsonDocument
            {   
                { "depth", depth },
                { "bestNode", node.value.bestNode },
                { "score", node.value.score },
                { "state", new BsonDocument
                    {
                        { "CurrentAgentId", state.currentAgentId },
                        { "Actions", new BsonArray( node.value.actions.Select(action => action.GetActionType().ToString()) ) },
                        { "Certainty", new BsonArray( state.certTable.Select(cert => new BsonArray(cert.Select( cert => cert.certainty))))},
                        { "PostPreference", new BsonArray( state.postPreferences.Select(pref => new BsonArray(pref))) },
                        { "Preference", new BsonArray( state.preferences.Select(pref => new BsonArray(pref))) },
                        { "Mu", new BsonArray(state.mu.Select(mu => new BsonArray(mu))) },
                        { "Free Energy", new BsonArray( state.fe.Select(fe => new BsonArray(fe.Select( fe => fe.freeEnergy))) )},
                        { "Positioning" , new BsonArray( state.objectBodies.Select(pos=> pos.ToBsonDocument()) )},
                        { "ToM", new BsonArray( state.tomPredict.Select(tom => tom)) },
                        { "InfluenceSocial", new BsonArray( state.tomUpdate.Select(influpref => new BsonArray(influpref))) },
                        { "Emotions", new BsonArray( state.emotions.Select(emo => emo.ToBsonDocument()) )},
                        { "SpatialStat", new BsonArray(state.spatialStats.Select(ss => new BsonArray(ss)))},
                        { "TargetId", state.targetIds[state.currentAgentId] },
                        { "InteractObjectId", state.interactObjectIds[state.currentAgentId] },
                    }
                },
                { "others", new BsonArray() },
                { "children", new BsonArray() },
            };

            /*if (node.value.otherAgents.Count > 0){
                foreach (Node<AgentStateNode> other in node.value.otherAgents)
                {
                    if (other.value.agentState != null){
                        document["others"].AsBsonArray.Add(SaveNodeRec(other, 0));
                    }
                }
            }*/

            if (node.DirectChildren > 0){
                foreach (Node<AgentStateNode> child in node.children){
                    if (child.value.bestNode){
                    //if (child.value.agentState != null){
                        document["children"].AsBsonArray.Add(SaveNodeRec(child, depth+1));
                    }
                }  
            }
            return document;
        }
    }
}
