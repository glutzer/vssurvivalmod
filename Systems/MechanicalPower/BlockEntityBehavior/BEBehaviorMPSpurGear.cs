using System.Collections.Generic;
using System.Diagnostics;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

#nullable disable

namespace Vintagestory.GameContent.Mechanics
{
    public class BEBehaviorMPSpurGear : BEBehaviorMPBase
    {
        public BlockFacing Facing => BlockFacing.FromFirstLetter(Block.Variant["orientation"]);

        float angleOffset;
        int sideCogMask;

        public bool HasCenterAxle { get; private set; }
        public int SideCogMask => sideCogMask;
        public IEnumerable<BlockFacing> SideCogFaces => BlockFacing.ALLFACES.Where(HasSideCog);
        public float GearAngleRad => base.AngleRad + angleOffset;

        public BEBehaviorMPSpurGear(BlockEntity blockentity) : base(blockentity)
        {
        }

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);

            // Makes it correct in most cases. Whoever reads this - feel free to make it perfect
            angleOffset = 11.25f * GameMath.DEG2RAD * (Pos.X % 32 + Pos.Y % 32 + Pos.Z % 32);

            AxisSign = AxisSignForFacing(Facing);
        }

        public static int[] AxisSignForFacing(BlockFacing facing)
        {
            int[] axisSign = new int[3] { 0, 0, 0 };
            switch (facing.Index)
            {
                case 0: // N
                case 2: // S
                    axisSign[2] = -1;
                    break;
                case 1: // E
                case 3: // W
                    axisSign[0] = -1;
                    break;
                case 4: // U
                case 5: // D
                    axisSign[1] = 1;
                    break;
            }
            return axisSign;
        }

        public static bool IsRotationReversedFor(BlockFacing turnDir)
        {
            return turnDir == BlockFacing.DOWN || turnDir == BlockFacing.EAST || turnDir == BlockFacing.SOUTH;
        }

        public bool HasSideCog(BlockFacing face)
        {
            return face != null && face.Axis != Facing.Axis && (sideCogMask & (1 << face.Index)) != 0;
        }

        public bool HasConnector(BlockFacing face)
        {
            return face == Facing || (HasCenterAxle && face == Facing.Opposite) || HasSideCog(face);
        }

        public bool TryAddCenterAxle()
        {
            if (HasCenterAxle || sideCogMask != 0) return false;

            HasCenterAxle = true;
            MarkModulesDirty();
            return true;
        }

        public bool TryAddSideCog(BlockFacing face)
        {
            if (HasCenterAxle || face == null || face.Axis == Facing.Axis || HasSideCog(face)) return false;

            sideCogMask |= 1 << face.Index;
            MarkModulesDirty();
            return true;
        }

        public IEnumerable<BlockFacing> GetConnectorFacings()
        {
            yield return Facing;

            if (HasCenterAxle) yield return Facing.Opposite;

            foreach (BlockFacing face in BlockFacing.ALLFACES)
            {
                if (HasSideCog(face)) yield return face;
            }
        }

        public BlockFacing GetSideCogTurnDir(BlockFacing face)
        {
            if (propagationDir == Facing) return face.Opposite;
            if (propagationDir == Facing.Opposite) return face;

            return face;
        }

        public bool IsSideCogRotationInverted(BlockFacing face)
        {
            return IsRotationReversedFor(propagationDir) != IsRotationReversedFor(GetSideCogTurnDir(face));
        }

        public void MarkModulesDirty()
        {
            SetOrientations();
            Api?.World.BlockAccessor.MarkBlockDirty(Position);
            Blockentity.MarkDirty(true);
        }

        public override MechPowerPath[] GetMechPowerExits(MechPowerPath entryDir)
        {
            List<MechPowerPath> paths = new();

            void AddPath(MechPowerPath path)
            {
                for (int i = 0; i < paths.Count; i++)
                {
                    if (paths[i].OutFacing == path.OutFacing) return;
                }

                paths.Add(path);
            }

            BlockFacing left, right, above, below;

            // Get the directions of potential neighbour connectable spur gears from this block's Facing
            if (Facing.IsHorizontal)
            {
                left = entryDir.OutFacing.Opposite == Facing ? entryDir.OutFacing.GetCW() : Facing.GetCW();
                right = entryDir.OutFacing.Opposite == Facing ? entryDir.OutFacing.GetCCW() : Facing.GetCCW();
                above = BlockFacing.UP;
                below = BlockFacing.DOWN;
            }
            else
            {
                left = BlockFacing.WEST;
                right = BlockFacing.EAST;
                above = BlockFacing.NORTH;
                below = BlockFacing.SOUTH;
            }

            // The axial path is the original spur-gear path. It keeps old placements driving parallel cogs as before.
            AddPath(entryDir.OutFacing.Opposite == Facing ? entryDir : entryDir.PropagatedClone(Facing, entryDir.invert, propagationDir));

            if (HasCenterAxle)
            {
                AddPath(entryDir.PropagatedClone(Facing.Opposite, entryDir.invert, propagationDir));
            }

            AddSidePathIfPresent(left, entryDir);
            AddSidePathIfPresent(right, entryDir);
            AddSidePathIfPresent(above, entryDir);
            AddSidePathIfPresent(below, entryDir);

            return paths.ToArray();

            void AddSidePathIfPresent(BlockFacing face, MechPowerPath entry)
            {
                if (HasSideCog(face))
                {
                    AddPath(entry.PropagatedClone(face, false, GetSideCogTurnDir(face)));
                    return;
                }

                if (Api.World.BlockAccessor.GetBlock(Pos.AddCopy(face)) == Block)
                {
                    AddPath(entry.PropagatedClone(face, !entry.invert, propagationDir.Opposite));
                }
            }
        }


        public override BlockFacing GetPropagatingTurnDir(BlockFacing toFacing)
        {
            if (toFacing?.Axis == Facing.Axis) return propagationDir;
            if (HasSideCog(toFacing)) return GetSideCogTurnDir(toFacing);

            return propagationDir.Opposite;
        }

        public override bool IsPropagationDirection(BlockPos fromPos, BlockFacing test)
        {
            BlockFacing connectorFace = null;
            if (fromPos != null)
            {
                connectorFace = BlockFacing.FromNormal(new Vec3i(
                    fromPos.X - Position.X,
                    fromPos.Y - Position.Y,
                    fromPos.Z - Position.Z
                ));
            }

            if (connectorFace?.Axis == Facing.Axis) return propagationDir == test;
            if (HasSideCog(connectorFace)) return GetSideCogTurnDir(connectorFace) == test;

            return propagationDir == test;
        }

        public override void SetPropagationDirection(MechPowerPath path)
        {
            BlockFacing turnDir = path.NetworkDir();

            if (turnDir?.Axis != Facing.Axis)
            {
                BlockFacing sideFace = HasSideCog(turnDir) ? turnDir : HasSideCog(turnDir?.Opposite) ? turnDir.Opposite : null;
                if (sideFace != null)
                {
                    turnDir = turnDir == sideFace ? Facing.Opposite : Facing;
                    path = new MechPowerPath(turnDir, path.gearingRatio, null, false);
                }
            }

            base.SetPropagationDirection(path);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            HasCenterAxle = tree.GetBool("centerAxle");
            sideCogMask = tree.GetInt("sideCogMask");
            base.FromTreeAttributes(tree, worldAccessForResolve);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            tree.SetBool("centerAxle", HasCenterAxle);
            tree.SetInt("sideCogMask", sideCogMask);
            base.ToTreeAttributes(tree);
        }

        public override float GetResistance()
        {
            return 0.0005f;
        }
    }
}
