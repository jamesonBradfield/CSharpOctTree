using Godot;

namespace OctTreeNamespace
{
    /// <summary>
    /// Extension methods for Vector3I operations
    /// </summary>
    public static class Vector3IExtensions
    {
        /// <summary>
        /// Returns a vector with each component halved (bit-shifted right)
        /// </summary>
        public static Vector3I HalfSize(this Vector3I size) =>
            new Vector3I(
                size.X >> 1,
                size.Y >> 1,
                size.Z >> 1
            );
            
        /// <summary>
        /// Calculate center point of a box
        /// </summary>
        public static Vector3I CalculateCenter(this Vector3I min, Vector3I size) =>
            new Vector3I(
                min.X + (size.X >> 1),
                min.Y + (size.Y >> 1),
                min.Z + (size.Z >> 1)
            );
            
        /// <summary>
        /// Calculate minimum bounds for a child node at specified octant
        /// </summary>
        public static Vector3I CalculateChildMin(this Vector3I nodeMin, Vector3I halfSize, int octant) =>
            new Vector3I(
                nodeMin.X + (octant & 1) * halfSize.X,
                nodeMin.Y + ((~octant >> 2) & 1) * halfSize.Y,
                nodeMin.Z + ((octant >> 1) & 1) * halfSize.Z
            );
            
        /// <summary>
        /// Check if two boxes intersect
        /// </summary>
        public static bool Intersects(
            this Vector3I min1, Vector3I max1,
            Vector3I min2, Vector3I max2) =>
            !(max1.X < min2.X || min1.X > max2.X ||
              max1.Y < min2.Y || min1.Y > max2.Y ||
              max1.Z < min2.Z || min1.Z > max2.Z);
              
        /// <summary>
        /// Check if a point is inside a box (inclusive)
        /// </summary>
        public static bool ContainsPoint(
            this Vector3I min, Vector3I max, 
            Vector3I point) =>
            point.X >= min.X && point.X <= max.X &&
            point.Y >= min.Y && point.Y <= max.Y &&
            point.Z >= min.Z && point.Z <= max.Z;
            
        /// <summary>
        /// Calculate node's maximum bounds
        /// </summary>
        public static Vector3I CalculateNodeMax(this Vector3I min, Vector3I size) =>
            new Vector3I(
                min.X + size.X,
                min.Y + size.Y, 
                min.Z + size.Z
            );
    }
}
