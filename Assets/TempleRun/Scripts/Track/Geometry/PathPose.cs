using UnityEngine;

namespace CrawfisSoftware.TempleRun.Track.Geometry
{
    /// <summary>
    /// Position + heading on the track. The heading is carried as a unit vector
    /// (not a 0..3 direction index), so builders are free to leave the 4-direction
    /// grid. The default <see cref="AxisAligned90Builder"/> keeps <see cref="Forward"/>
    /// snapped to a cardinal axis, reproducing the old <c>_directionIndex</c> exactly.
    ///    <see cref="Up"/> is carried for future banking/ramp builders; today it stays +Y.
    /// </summary>
    public readonly struct PathPose
    {
        /// <summary>World position of the pose.</summary>
        public readonly Vector3 Position;

        /// <summary>Unit heading vector. XZ-planar today, but 3D-ready for ramps.</summary>
        public readonly Vector3 Forward;

        /// <summary>Unit up vector. Enables banked turns later; +Y today.</summary>
        public readonly Vector3 Up;

        public PathPose(Vector3 position, Vector3 forward, Vector3 up)
        {
            Position = position;
            Forward  = forward;
            Up       = up;
        }

        public override string ToString() => $"PathPose(pos={Position}, fwd={Forward}, up={Up})";
    }
}
