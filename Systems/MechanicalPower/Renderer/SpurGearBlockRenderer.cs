using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

#nullable enable

namespace Vintagestory.GameContent.Mechanics
{
    public class SpurGearBlockRenderer : MechBlockRenderer
    {
        readonly MeshRef[] gearMeshes = new MeshRef[6];
        readonly CustomMeshDataPartFloat[] gearFloats = new CustomMeshDataPartFloat[6];
        readonly int[] gearCounts = new int[6];

        readonly MeshRef[] axleMeshes = new MeshRef[3];
        readonly CustomMeshDataPartFloat[] axleFloats = new CustomMeshDataPartFloat[3];
        readonly int[] axleCounts = new int[3];

        public SpurGearBlockRenderer(ICoreClientAPI capi, MechanicalPowerMod mechanicalPowerMod, Block textureSoureBlock, CompositeShape shapeLoc) : base(capi, mechanicalPowerMod)
        {
            gearMeshes[BlockFacing.NORTH.Index] = UploadShape(textureSoureBlock, "shapes/block/wood/mechanics/spurgear16.json", 0, 0, 0, out gearFloats[BlockFacing.NORTH.Index]);
            gearMeshes[BlockFacing.EAST.Index] = UploadShape(textureSoureBlock, "shapes/block/wood/mechanics/spurgear16.json", 0, 270, 0, out gearFloats[BlockFacing.EAST.Index]);
            gearMeshes[BlockFacing.SOUTH.Index] = UploadShape(textureSoureBlock, "shapes/block/wood/mechanics/spurgear16.json", 0, 180, 0, out gearFloats[BlockFacing.SOUTH.Index]);
            gearMeshes[BlockFacing.WEST.Index] = UploadShape(textureSoureBlock, "shapes/block/wood/mechanics/spurgear16.json", 0, 90, 0, out gearFloats[BlockFacing.WEST.Index]);
            gearMeshes[BlockFacing.UP.Index] = UploadShape(textureSoureBlock, "shapes/block/wood/mechanics/spurgear16.json", 90, 0, 0, out gearFloats[BlockFacing.UP.Index]);
            gearMeshes[BlockFacing.DOWN.Index] = UploadShape(textureSoureBlock, "shapes/block/wood/mechanics/spurgear16.json", 270, 0, 0, out gearFloats[BlockFacing.DOWN.Index]);

            axleMeshes[(int)EnumAxis.X] = UploadShape(textureSoureBlock, "shapes/block/wood/mechanics/axle.json", 0, 0, 0, out axleFloats[(int)EnumAxis.X]);
            axleMeshes[(int)EnumAxis.Y] = UploadShape(textureSoureBlock, "shapes/block/wood/mechanics/axle.json", 0, 0, 90, out axleFloats[(int)EnumAxis.Y]);
            axleMeshes[(int)EnumAxis.Z] = UploadShape(textureSoureBlock, "shapes/block/wood/mechanics/axle.json", 0, 90, 0, out axleFloats[(int)EnumAxis.Z]);
        }

        MeshRef UploadShape(Block textureSourceBlock, string shapePath, float rotateX, float rotateY, float rotateZ, out CustomMeshDataPartFloat floats)
        {
            var shape = Shape.TryGet(capi, shapePath);
            capi.Tesselator.TesselateShape(textureSourceBlock, shape, out var mesh, new Vec3f(rotateX, rotateY, rotateZ));

            mesh.CustomFloats = floats = new CustomMeshDataPartFloat((16 + 4) * 10100)
            {
                Instanced = true,
                InterleaveOffsets = new int[] { 0, 16, 32, 48, 64 },
                InterleaveSizes = new int[] { 4, 4, 4, 4, 4 },
                InterleaveStride = 16 + (4 * 16),
                StaticDraw = false,
            };
            mesh.CustomFloats.SetAllocationSize((16 + 4) * 10100);

            return capi.Render.UploadMesh(mesh);
        }

        protected override void UpdateCustomFloatBuffer()
        {
            System.Array.Clear(gearCounts, 0, gearCounts.Length);
            System.Array.Clear(axleCounts, 0, axleCounts.Length);

            var cameraPos = capi.World.Player.Entity.CameraPos;
            foreach (var dev in renderedDevices.Values)
            {
                if (dev is not BEBehaviorMPSpurGear gear) continue;

                tmp.Set((float)(dev.Position.X - cameraPos.X), (float)(dev.Position.InternalY - cameraPos.Y), (float)(dev.Position.Z - cameraPos.Z));

                AddGearPart(gear, gear.Facing, false);
                foreach (var face in gear.SideCogFaces)
                {
                    AddGearPart(gear, face, gear.IsSideCogRotationInverted(face));
                }

                if (gear.HasCenterAxle)
                {
                    AddAxlePart(gear);
                }
            }
        }

        void AddGearPart(BEBehaviorMPSpurGear gear, BlockFacing face, bool invert)
        {
            int index = gearCounts[face.Index]++;
            int[] axisSign = BEBehaviorMPSpurGear.AxisSignForFacing(face);
            float rotation = gear.GearAngleRad % GameMath.TWOPI;
            if (invert) rotation = -rotation;

            UpdateLightAndTransformMatrix(gearFloats[face.Index].Values, index, tmp, gear.LightRgba, rotation * axisSign[0], rotation * axisSign[1], rotation * axisSign[2]);
        }

        void AddAxlePart(BEBehaviorMPSpurGear gear)
        {
            int axisIndex = (int)gear.Facing.Axis;
            int index = axleCounts[axisIndex]++;
            int[] axisSign = BEBehaviorMPSpurGear.AxisSignForFacing(gear.Facing);
            float rotation = gear.AngleRad % GameMath.TWOPI;

            UpdateLightAndTransformMatrix(axleFloats[axisIndex].Values, index, tmp, gear.LightRgba, rotation * axisSign[0], rotation * axisSign[1], rotation * axisSign[2]);
        }

        protected override void UpdateLightAndTransformMatrix(int index, Vec3f distToCamera, float rotRad, IMechanicalPowerRenderable dev)
        {
        }

        public override void OnRenderFrame(float deltaTime, IShaderProgram prog)
        {
            UpdateCustomFloatBuffer();

            for (int i = 0; i < gearMeshes.Length; i++)
            {
                if (gearCounts[i] <= 0) continue;

                gearFloats[i].Count = gearCounts[i] * 20;
                updateMesh.CustomFloats = gearFloats[i];
                capi.Render.UpdateMesh(gearMeshes[i], updateMesh);
                capi.Render.RenderMeshInstanced(gearMeshes[i], gearCounts[i]);
            }

            for (int i = 0; i < axleMeshes.Length; i++)
            {
                if (axleCounts[i] <= 0) continue;

                axleFloats[i].Count = axleCounts[i] * 20;
                updateMesh.CustomFloats = axleFloats[i];
                capi.Render.UpdateMesh(axleMeshes[i], updateMesh);
                capi.Render.RenderMeshInstanced(axleMeshes[i], axleCounts[i]);
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            foreach (var mesh in gearMeshes)
            {
                mesh?.Dispose();
            }

            foreach (var mesh in axleMeshes)
            {
                mesh?.Dispose();
            }
        }
    }
}
