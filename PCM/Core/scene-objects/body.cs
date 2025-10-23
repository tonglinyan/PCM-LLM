using PCM.Core.Geom3d;

namespace PCM.Core.SceneObjects {
    public class ObjectBody : Utils.Copy.ICopyable<ObjectBody>
    {
        public Polyhedron BodyPosition;
        public Vertex LookAtOrigin;
        public Vertex LookAt;
        public ObjectType Type;

        public SimplePhysics.Collider.Convex2DPolygonCollider collider;
        public ObjectBody(Polyhedron bodyPosition, Vertex lookAt, Vertex LookAtOrigin)
        {
            BodyPosition = bodyPosition;
            LookAt = lookAt.Unit();
            this.LookAtOrigin = LookAtOrigin;
            //last face is "ground" face
            var face = bodyPosition.Faces[bodyPosition.Faces.Length - 1];
            var colliderVertices = new Vertex[face.Length];
            for (int i = 0; i < face.Length; i++)
            {
                // Console.WriteLine($"{i}  {bodyPosition.Vertices[face[i]]}");
                colliderVertices[i] = bodyPosition.Vertices[face[i]];
            }
            collider = new SimplePhysics.Collider.Convex2DPolygonCollider(new Polygon(colliderVertices));
        }

        public ObjectBody Copy() => new(BodyPosition.Copy(), LookAt.Copy(), LookAtOrigin.Copy()) { Type = Type };


        //Can only move in 2d
        public void MoveForward(double speed)
        {
            var dir = LookAt.Multiply(speed);
            BodyPosition.Translate_inplace(dir);
            LookAtOrigin.Add_inplace(dir);
        }

        public bool Equals(ObjectBody b) => BodyPosition.Equals(b.BodyPosition) && LookAt.Equals(b.LookAt);


        //Simplified for 2d, we use X and Z (on the ground)
        public void RotateTowardsDirection(Vertex dir)
        {
            var point = new Vertex(dir.X, dir.Y, dir.Z).Unit();
            if (point.Sub(LookAt).IsZero())
            {
                return;
            }
            var cross = LookAt.Cross(point);
            var a =
                 (LookAt.X * point.X + LookAt.Z * point.Z) /
            (Math.Sqrt(LookAt.X * LookAt.X + LookAt.Z * LookAt.Z) * Math.Sqrt(point.X * point.X + point.Z * point.Z));
            a = a > 1.0 ? 1.0 : a < -1.0 ? -1.0 : a; //Round errors, can be e-16 > than 1 or e-16 < -1
            var theta = Math.Acos(a);
            theta = cross.Y > 0 ? theta : -theta;
            if (double.IsNaN(theta))
            {
                throw new Exception("NaN angle " + theta + " " + a + " " + Math.Acos(a)+ " "+ cross);
            }
            BodyPosition.RotateY_inplace(theta);
            LookAt = point;
        }

        public void RotateTowardsPoint(Vertex point)
        {
            var dir = point.Sub(BodyPosition.Center);
            var pointOnGround = new Vertex(dir.X, 0, dir.Z).Unit();
            if (pointOnGround.Sub(LookAt).IsZero() || pointOnGround.IsZero()) //add the second constraint
            {
                return;
            }
            var cross = LookAt.Cross(pointOnGround);
            var a =
                 (LookAt.X * pointOnGround.X + LookAt.Z * pointOnGround.Z) /
            (Math.Sqrt(LookAt.X * LookAt.X + LookAt.Z * LookAt.Z) * Math.Sqrt(pointOnGround.X * pointOnGround.X + pointOnGround.Z * pointOnGround.Z));
            a = a > 1.0 ? 1.0 : a < -1.0 ? -1.0 : a; //Round errors, can be e-16 > than 1 or e-16 < -1
            var theta = Math.Acos(a);
            theta = cross.Y > 0 ? theta : -theta;
            if (double.IsNaN(theta))
            {
                throw new Exception("NaN angle " + theta + " " + a + " " + Math.Acos(a) + " " + cross);
            }
            BodyPosition.RotateY_inplace(theta);
            LookAt = pointOnGround;
        }

        public void RotateAwayFromPoint(Vertex point)
        {
            var dir = point.Sub(BodyPosition.Center);
            var pointOnGround = new Vertex(-dir.X, 0, -dir.Z).Unit();
            if (pointOnGround.Sub(LookAt).IsZero())
            {
                return;
            }
            var cross = LookAt.Cross(pointOnGround);
            var a =
                 (LookAt.X * pointOnGround.X + LookAt.Z * pointOnGround.Z) /
            (Math.Sqrt(LookAt.X * LookAt.X + LookAt.Z * LookAt.Z) * Math.Sqrt(pointOnGround.X * pointOnGround.X + pointOnGround.Z * pointOnGround.Z));
            a = a > 1.0 ? 1.0 : a < -1.0 ? -1.0 : a; //Round errors, can be e-16 > than 1 or e-16 < -1
            var theta = Math.Acos(a);
            theta = cross.Y > 0 ? theta : -theta;
            if (double.IsNaN(theta))
            {
                throw new Exception("NaN angle " + theta + " " + a + " " + Math.Acos(a) + " " + cross);
            }
            BodyPosition.RotateY_inplace(theta);
            LookAt = pointOnGround;
        }
        /// <summary>
        /// Move the body towards a point.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="speed"></param>
        public void MoveTowards(Vertex point, double speed)
        {
            try
            {
                var sub = point.Sub(BodyPosition.Center);
                var dist = sub.Magnitude();
                if (IsPointInFront(point))
                    MoveForward(Math.Min(dist,speed));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public Vertex GetEye()
        {
            return LookAtOrigin;
        }

        //On a plane, 2d
        public bool IsPointInFront(Vertex point)
        {
            var diff2d = point.Sub(BodyPosition.Center);
            diff2d.Y = 0;
            return LookAt.Dot(diff2d) > 0;
        }

        public static ObjectBody Create(ObjectType objectType)
        {
            Polyhedron body = DemoVR.GetBoundingBox(objectType);
            var lookAt = new Vertex(0, 0, 1);
            return new ObjectBody(body, lookAt, body.Center) { Type = objectType };
        }

        public Vertex PointToDir(Vertex point) => LookAtOrigin == null ? point.Sub(BodyPosition.Center) : point.Sub(LookAtOrigin);
    }
}