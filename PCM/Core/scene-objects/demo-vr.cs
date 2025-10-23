using PCM.Core.Geom3d;

namespace PCM.Core.SceneObjects
{
    public enum ObjectType
    {
        ArtificialAgent,
        Ball,
        Player,
    }
    public static class DemoVR
    {
        // Unit: centimeters
        // Robots are 38(w) x 90(h) x 38(l) 
        // Balls are 20(radius)
        // Player is

        public static (double width, double height, double depth) Robot = (width: 38, height: 90, depth: 38);
        public static (double width, double height, double depth) Ball = (width: 20, height: 20, depth: 20);
        public static (double width, double height, double depth) Player = (width: 50, height: 180, depth: 50);



        public static Polyhedron DefaultRobotBoundingBox() => Polyhedron.RectangularPrism(Vertex.Zero(), Robot.width, Robot.height, Robot.depth);

        public static Polyhedron DefaultBallBoundingBox() => Polyhedron.RectangularPrism(Vertex.Zero(), Ball.width, Ball.height, Ball.depth);


        public static Polyhedron DefaultPlayerBoundingBox() => Polyhedron.RectangularPrism(Vertex.Zero(), Player.width, Player.height, Player.depth);

        public static Polyhedron GetBoundingBox(ObjectType type)
        {
            var dimensions = GetDimensions(type);
            return Polyhedron.RectangularPrism(Vertex.Zero(), dimensions.width, dimensions.height, dimensions.depth);
        }
        public static (double width, double height, double depth) GetDimensions(ObjectType type)
        {
            return type switch
            {
                ObjectType.ArtificialAgent => Robot,
                ObjectType.Ball => Ball,
                ObjectType.Player => Player,
                _ => throw new Exception("Unknown body type"),
            };
        }
    }
}
