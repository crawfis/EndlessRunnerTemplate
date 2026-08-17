using CrawfisSoftware.AssetManagement;
using CrawfisSoftware.TempleRun.GameConfig;

using UnityEngine;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Voxel track visuals built from ONE stretched voxel per lane, rather than a run of unit
    /// voxels down the centre line. A three-lane segment is three objects instead of
    /// length/_lengthScale objects, and the track is as wide as the lanes the player can actually
    /// occupy.
    ///
    /// It also draws the turn arms itself. That is the part the spline-driven spawners cannot do
    /// for a T-junction: an Either segment publishes only its approach span, and the exit span is
    /// not built until the player commits a direction, so neither branch is ever visible while the
    /// choice is still open. Here an Either draws BOTH arms up front.
    ///
    ///    Dependencies: Blackboard.LaneConfig (lane count/width), _prefab
    ///    Subscribes: (via PrefabSpawnerAbstract) SplineSegmentCreated, SegmentGeometryReady,
    ///                ActiveTrackChanged
    /// </summary>
    /// <remarks>
    /// The prefab convention is the same as VoxelPrefabSpawner: origin at bottom-centre, +Z
    /// forward, natural size _widthScale across by _lengthScale along. Stretching one voxel will
    /// stretch its texture too — that is the trade for "a single voxel per lane".
    /// </remarks>
    public class VoxelLaneTrackSpawner : PrefabSpawnerAbstract
    {
        [Tooltip("Also draw an arm for ordinary Left/Right segments. Off by default: those already " +
                 "get their exit strip from a second SplineSegmentCreated, so an arm would double it. " +
                 "An Either junction always gets both arms regardless, because nothing else draws them.")]
        [SerializeField] private bool _drawArmsOnSingleTurns = false;

        [Tooltip("Arm length. 0 uses the segment's ExitDistance, which is where the real exit will " +
                 "be built once a direction is committed.")]
        [SerializeField] private float _armLengthOverride = 0f;

        [Tooltip("The prefab's own width in world units before scaling - 1 for a 1x1 voxel. This is " +
                 "NOT _widthScale: the base class uses that as the TOTAL track width (VoxelPrefabSpawner " +
                 "derives TrackWidthOffset from half of it), so scaling a lane by it shrinks each lane " +
                 "to one-third and leaves gaps between them.")]
        [SerializeField] private float _prefabWidth = 1f;

        private LaneConfig Lanes => Blackboard.Instance.LaneConfig;

        protected override void Awake()
        {
            base.Awake();

            // Half the visual track width — the corner-centring offset AxisAligned90Builder reads
            // when placing turn exit strips. VoxelPrefabSpawner uses half of ONE voxel because its
            // track is one voxel wide; this track is the full lane span, so the offset has to match
            // that instead or the exit strips will not butt flush against the approach.
            Blackboard.Instance.TrackWidthOffset = 0.5f * Lanes.LaneCount * Lanes.LaneWidth;
        }

        protected override void CreateTrack(SplineSegmentData spline, Transform trackTransform)
        {
            float span = SpanLanes(spline.SegmentLength, trackTransform);

            Direction end = spline.EndDirection;
            bool both = end == Direction.Either;
            bool single = end == Direction.Left || end == Direction.Right;
            if (!both && !(single && _drawArmsOnSingleTurns)) return;

            float armLength = _armLengthOverride > 0f
                ? _armLengthOverride
                : spline.Definition.ExitDistance;

            if (both || end == Direction.Left) CreateArm(trackTransform, span, armLength, Direction.Left);
            if (both || end == Direction.Right) CreateArm(trackTransform, span, armLength, Direction.Right);
        }

        // Kept because the base class declares it; this spawner works from the full spline instead.
        protected override void CreateTrack(float length, Transform trackTransform, Direction endCapDirection)
        {
            SpanLanes(length, trackTransform);
        }

        /// <summary>
        /// One stretched voxel per lane, covering exactly the extent a run of unit voxels would
        /// have covered. Returns that extent so the caller can put the arms at the far end.
        /// </summary>
        private float SpanLanes(float length, Transform parent)
        {
            // Same voxel count the per-voxel spawner would use, so the two produce the same extent.
            int count = Mathf.Max(1, Mathf.FloorToInt(length / _lengthScale + 0.2f));
            float span = count * _lengthScale;

            // n unit voxels sit at z = 0, L, ... (n-1)L, so together they span nL centred at
            // (n-1)L/2. One voxel placed there and scaled by n in z covers the identical extent,
            // whatever the prefab's own pivot convention is.
            float centreZ = (count - 1) * 0.5f * _lengthScale;

            int   laneCount = Lanes.LaneCount;
            float laneWidth = Lanes.LaneWidth;
            float widthScale = laneWidth / _prefabWidth;   // fill the lane, whatever the prefab's width

            for (int lane = 0; lane < laneCount; lane++)
            {
                float x = (lane - (laneCount - 1) * 0.5f) * laneWidth;

                GameObject voxel = InstantiationSingleton.CreateNewInstance(_prefab, true);
                voxel.name = $"Lane_{lane}";
                voxel.transform.SetParent(parent, worldPositionStays: false);
                voxel.transform.localPosition = new Vector3(x, 0f, centreZ);
                voxel.transform.localScale = new Vector3(widthScale, 1f, count);
            }

            return span;
        }

        /// <summary>
        /// An arm perpendicular to the span, starting at its end. Rotating a child by ±90° lets the
        /// arm reuse the lane spanning above — in that frame the arm runs along +Z like any segment.
        /// </summary>
        private void CreateArm(Transform parent, float span, float armLength, Direction side)
        {
            var arm = new GameObject($"Arm_{DirectionSuffix(side)}");
            arm.transform.SetParent(parent, worldPositionStays: false);

            // Sit exactly where the real branch will be built. AxisAligned90Builder nudges the exit
            // to pivot - offset*forward + offset*newForward, with offset = TrackWidthOffset = half
            // the track width - so in this container's frame that is (±halfWidth, 0, span-halfWidth).
            // Starting there butts the arm flush against the side of the approach strip, which
            // already covers the corner square, instead of overlapping it and stopping short.
            float halfWidth = 0.5f * Lanes.LaneCount * Lanes.LaneWidth;
            float sign = side == Direction.Left ? -1f : 1f;
            arm.transform.localPosition = new Vector3(sign * halfWidth, 0f, span - halfWidth);
            arm.transform.localRotation = Quaternion.Euler(0f, sign * 90f, 0f);

            SpanLanes(armLength, arm.transform);
        }
    }
}
