using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

#nullable disable

namespace Vintagestory.GameContent.Mechanics
{
    public class BlockSpurGear : BlockMPBase
    {
        protected BlockFacing Orientation; 

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            Orientation = BlockFacing.FromFirstLetter(Variant["orientation"]);
        }

        public override bool HasMechPowerConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face, BlockMPBase forBlock)
        {
            BEBehaviorMPSpurGear gear = world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorMPSpurGear>();
            return gear?.HasConnector(face) == true || (forBlock == this && face != Orientation.Opposite);
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            return new ItemStack(world.GetBlock(CodeWithVariant("orientation", "s")));
        }

        public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            return GetModuleBoxes(blockAccessor, pos, base.GetCollisionBoxes(blockAccessor, pos));
        }

        public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            return GetModuleBoxes(blockAccessor, pos, base.GetSelectionBoxes(blockAccessor, pos));
        }

        public override Cuboidf[] GetParticleCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            return GetCollisionBoxes(blockAccessor, pos);
        }

        private Cuboidf[] GetModuleBoxes(IBlockAccessor blockAccessor, BlockPos pos, Cuboidf[] baseBoxes)
        {
            BEBehaviorMPSpurGear gear = blockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorMPSpurGear>();
            if (gear == null) return baseBoxes;

            List<Cuboidf> boxes = new(baseBoxes ?? System.Array.Empty<Cuboidf>());

            if (gear.HasCenterAxle)
            {
                boxes.Add(GetAxleBox(gear.Facing.Axis));
            }

            foreach (BlockFacing face in gear.SideCogFaces)
            {
                boxes.Add(GetGearBox(face));
            }

            return boxes.ToArray();
        }

        private Cuboidf GetAxleBox(EnumAxis axis)
        {
            return axis switch
            {
                EnumAxis.X => new Cuboidf(0f, 0.375f, 0.375f, 1f, 0.625f, 0.625f),
                EnumAxis.Y => new Cuboidf(0.375f, 0f, 0.375f, 0.625f, 1f, 0.625f),
                _ => new Cuboidf(0.375f, 0.375f, 0f, 0.625f, 0.625f, 1f),
            };
        }

        private Cuboidf GetGearBox(BlockFacing face)
        {
            return face.Index switch
            {
                0 => new Cuboidf(0.125f, 0f, 0f, 0.85f, 1f, 0.375f),       // North
                1 => new Cuboidf(0.625f, 0f, 0.125f, 1f, 1f, 0.85f),       // East
                2 => new Cuboidf(0.125f, 0f, 0.625f, 0.85f, 1f, 1f),       // South
                3 => new Cuboidf(0f, 0f, 0.125f, 0.375f, 1f, 0.85f),       // West
                4 => new Cuboidf(0.125f, 0.625f, 0.125f, 0.875f, 1f, 0.875f), // Up
                _ => new Cuboidf(0.125f, 0f, 0.125f, 0.875f, 0.375f, 0.875f), // Down
            };
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
        {
            List<ItemStack> drops = new(base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier) ?? System.Array.Empty<ItemStack>());
            BEBehaviorMPSpurGear gear = world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorMPSpurGear>();
            if (gear == null) return drops.ToArray();

            if (gear.HasCenterAxle)
            {
                Block axleBlock = world.GetBlock(new AssetLocation("woodenaxle-ud"));
                if (axleBlock != null) drops.Add(new ItemStack(axleBlock));
            }

            Block spurBlock = world.GetBlock(CodeWithVariant("orientation", "s"));
            if (spurBlock != null)
            {
                for (int i = 0; i < gear.SideCogFaces.Count(); i++)
                {
                    drops.Add(new ItemStack(spurBlock));
                }
            }

            return drops.ToArray();
        }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            if (TryAddSideCogFromAdjacentAxle(world, blockSel))
            {
                return true;
            }

            if (!CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
            {
                return false;
            }

            BlockFacing targetFace = blockSel.Face.Opposite;
            BlockPos pos = blockSel.Position.AddCopy(targetFace);
            BlockEntity be = world.BlockAccessor.GetBlockEntity(pos);
            IMechanicalPowerBlock neighbour = be?.Block as IMechanicalPowerBlock;

            BEBehaviorMPAxle bempaxle = be?.GetBehavior<BEBehaviorMPAxle>();
            if (bempaxle == null || !(bempaxle.Block as BlockMPBase).HasMechPowerConnectorAt(world, pos, targetFace, this))
            {
                failureCode = "requiresaxle";
                return false;
            }
            if (bempaxle != null && !BEBehaviorMPAxle.IsAttachedToBlock(world.BlockAccessor, neighbour as Block, pos))
            {
                failureCode = "axlemusthavesupport";
                return false;
            }

            BlockSpurGear toPlaceBlock = world.GetBlock(CodeWithVariant("orientation", targetFace.Code[0] + "")) as BlockSpurGear;
            world.BlockAccessor.SetBlock(toPlaceBlock.BlockId, blockSel.Position);

            BEBehaviorMPBase selfBeh = GetBEBehavior<BEBehaviorMPBase>(blockSel.Position);
            if (selfBeh != null)
            {
                MechPowerPath[] exits = selfBeh.GetMechPowerExits(new MechPowerPath() { OutFacing = targetFace });

                List<BlockFacing> possiblyNetworklessCandidates = new();
                foreach (MechPowerPath exit in exits)
                {
                    BlockPos npos = blockSel.Position.AddCopy(exit.OutFacing);
                    IMechanicalPowerBlock neibBlock = world.BlockAccessor.GetBlock(npos) as IMechanicalPowerBlock;
                    neibBlock?.DidConnectAt(world, npos, exit.OutFacing.Opposite);
                    if (neibBlock != null && !selfBeh.tryConnect(exit.OutFacing))
                    {
                        // A side cog may not have a network yet; retry it after the first network connection succeeds.
                        possiblyNetworklessCandidates.Add(exit.OutFacing);
                    }
                }

                if (selfBeh.Network != null)
                {
                    foreach (BlockFacing face in possiblyNetworklessCandidates) selfBeh.tryConnect(face);
                }
            }

            return true;
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BEBehaviorMPSpurGear gear = world.BlockAccessor.GetBlockEntity(blockSel.Position)?.GetBehavior<BEBehaviorMPSpurGear>();
            ItemSlot slot = byPlayer?.InventoryManager?.ActiveHotbarSlot;
            Block heldBlock = slot?.Itemstack?.Block;

            if (gear == null || heldBlock == null)
            {
                return base.OnBlockInteractStart(world, byPlayer, blockSel);
            }

            if (heldBlock is BlockAxle && blockSel.Face == Orientation.Opposite)
            {
                if (world.Side == EnumAppSide.Server && gear.TryAddCenterAxle())
                {
                    ConsumeOne(world, byPlayer, slot);
                    TryConnectModule(world, blockSel.Position, Orientation);
                    TryConnectModule(world, blockSel.Position, Orientation.Opposite);
                }
                return true;
            }

            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        private bool TryAddSideCogFromAdjacentAxle(IWorldAccessor world, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlock(blockSel.Position) is not BlockSpurGear existingSpur) return false;
            return existingSpur.TryAddSideCogFromAdjacentAxle(world, blockSel.Position, blockSel.Face.Opposite);
        }

        internal bool TryAddSideCogFromAdjacentAxle(IWorldAccessor world, BlockPos spurPos, BlockFacing sideFace)
        {
            BEBehaviorMPSpurGear gear = world.BlockAccessor.GetBlockEntity(spurPos)?.GetBehavior<BEBehaviorMPSpurGear>();
            if (gear == null) return false;

            if (sideFace.Axis == gear.Facing.Axis || gear.HasSideCog(sideFace)) return false;

            BlockPos axlePos = spurPos.AddCopy(sideFace);
            BlockEntity be = world.BlockAccessor.GetBlockEntity(axlePos);
            IMechanicalPowerBlock neighbour = be?.Block as IMechanicalPowerBlock;
            if (be?.GetBehavior<BEBehaviorMPAxle>() == null || neighbour == null || !neighbour.HasMechPowerConnectorAt(world, axlePos, sideFace.Opposite, this))
            {
                return false;
            }

            if (world.Side == EnumAppSide.Server)
            {
                gear.TryAddSideCog(sideFace);
                TryConnectModule(world, spurPos, gear.Facing);
                TryConnectModule(world, spurPos, sideFace);
            }

            return true;
        }

        private void TryConnectModule(IWorldAccessor world, BlockPos pos, BlockFacing face)
        {
            BlockPos npos = pos.AddCopy(face);
            IMechanicalPowerBlock neighbour = world.BlockAccessor.GetBlock(npos) as IMechanicalPowerBlock;
            if (neighbour == null || !neighbour.HasMechPowerConnectorAt(world, npos, face.Opposite, this)) return;

            neighbour.DidConnectAt(world, npos, face.Opposite);
            world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorMPBase>()?.tryConnect(face);
        }

        private void ConsumeOne(IWorldAccessor world, IPlayer byPlayer, ItemSlot slot)
        {
            if (byPlayer?.WorldData.CurrentGameMode == EnumGameMode.Creative) return;

            slot.TakeOut(1);
            slot.MarkDirty();
        }

        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            Block nblock = world.BlockAccessor.GetBlock(pos.AddCopy(Orientation));
            BEBehaviorMPSpurGear gear = world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorMPSpurGear>();
            bool hasModules = gear?.HasCenterAxle == true || gear?.SideCogMask != 0;

            if (!hasModules && (nblock is not BlockMPBase || nblock.SideIsSolid(world.BlockAccessor, pos, Orientation.Opposite.Index)))
            {
                world.BlockAccessor.BreakBlock(pos, null);
                return;
            }

            base.OnNeighbourBlockChange(world, pos, neibpos);
        }

        public override void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face) { }
    }
}
