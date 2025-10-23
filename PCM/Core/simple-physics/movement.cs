using PCM.Core.SceneObjects;
using PCM.Core.Utils;

namespace PCM.Core.SimplePhysics
{
    public static class Movement
    {
        public static ObjectBody[] MoveSimultaneously(ObjectBody[] objectBodies, List<ObjectBody> destinations, List<double> agentSpeeds, Dictionary<int,int> interactions, double nbOfSteps = 3)
        {
            //TODO: Optimize that with a quadtree or smthng else if multiple objects
            //TODO: Support actions that move objects
            var finalObjectPositions = Copy.CopyArray<ObjectBody>(objectBodies);
            // System.Console.WriteLine(objectBodies[0].BodyPosition.Center + " > " + destinations[0].BodyPosition.Center);
            var agentSpeedsDelta = agentSpeeds.Select(s => s / nbOfSteps).ToList();
            for (int i = 0; i < nbOfSteps; i++)
            {
                //move
                var positions = Copy.CopyArray(finalObjectPositions);
                for (int aindex = 0; aindex < agentSpeeds.Count; aindex++)
                {
                    positions[aindex].RotateTowardsDirection(destinations[aindex].LookAt);
                    positions[aindex].MoveTowards(destinations[aindex].BodyPosition.Center, agentSpeedsDelta[aindex]);

                }
                //check for collision
                for (int aindex = 0; aindex < agentSpeeds.Count; aindex++)
                {
                    bool collides = false;
                    for (int oindex = 0; oindex < positions.Length; oindex++)
                    // {
                    //     if (oindex != aindex)
                    //     {
                    //         int target = -1;
                    //         if (!(interactions.TryGetValue(aindex, out target) && target == oindex))
                    //         {
                    //             if (positions[aindex].collider.collides(positions[oindex].collider))
                    //             {
                    //                 collides = true;
                    //                 break;
                    //             }
                    //         }
                    //     }
                    // }
                    if (!collides)
                        finalObjectPositions[aindex] = positions[aindex];
                }
            }
            return finalObjectPositions;

        }

        public static ObjectBody[] RemoveCollisions(ObjectBody[] positions, Dictionary<int,int> interactions){
            //TODO: Temp (not corrections, collisions deactivated)
            return positions;
            // bool colliding = true;
            // int loops = 0;
            // while (colliding)
            // {
            //     colliding = false;
            //     for (var objectIndex = 0; objectIndex < positions.Length; objectIndex++)
            //     {
            //         var object1 = positions[objectIndex];
            //         var collider1 = new SimplePhysics.Collider.Convex2DPolygonCollider(new Geom3d.Polygon(object1.BodyPosition.GetBottomFaceVertices().ToArray()));
            //         if (!interactions.ContainsValue(objectIndex))
            //         {
            //             for (var objectIndex2 = 0; objectIndex2 < positions.Length; objectIndex2++)
            //             {
            //                 loops++;
            //                 if(loops > 1000){
            //                     throw new System.Exception("Could not remove collisions in time");
            //                 }
            //                 if (objectIndex != objectIndex2 && !interactions.ContainsValue(objectIndex))
            //                 {
            //                     var object2 = positions[objectIndex2];
            //                     var collider2 = new SimplePhysics.Collider.Convex2DPolygonCollider(new Geom3d.Polygon(object2.BodyPosition.GetBottomFaceVertices().ToArray()));
            //                     if (collider1.Collides(collider2))
            //                     {
            //                         //push object2
            //                         var direction = object2.BodyPosition.Center.Sub(object1.BodyPosition.Center);
            //                         direction.Y = 0;
            //                         direction = direction.Unit();
            //                         var mov = direction.Multiply_inplace(2.5);
            //                         object2.BodyPosition.Translate_inplace(mov);
            //                         if(interactions.ContainsKey(objectIndex2)){
            //                             positions[interactions[objectIndex2]].BodyPosition.Translate_inplace(mov);
            //                         }
            //                         colliding = true;
            //                     }
            //                 }

            //             }
            //         }

            //     }
            // }
            // return positions;
        }
    }
}