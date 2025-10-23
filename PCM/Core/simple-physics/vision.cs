using PCM.Core.SceneObjects;

namespace PCM.Core.SimplePhysics
{
    public static class Vision
    {
        public enum VisibleType
        {
            NotVisible = 0,
            PartiallyVisible = 1,
            FullyVisible = 2
        }

        /// <summary>
        /// Check if an origin can see a target. No occlusion detection, basically check if an object is in front
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static VisibleType CanSeeTarget(ObjectBody origin, ObjectBody target)
        {
            if (target.BodyPosition.IsVisibleBy(origin.GetEye(), origin.LookAt))
                return VisibleType.PartiallyVisible;
            return VisibleType.NotVisible;
        }
    }
}