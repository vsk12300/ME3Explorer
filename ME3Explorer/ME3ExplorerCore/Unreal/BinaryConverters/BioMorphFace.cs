﻿using ME3ExplorerCore.SharpDX;

namespace ME3ExplorerCore.Unreal.BinaryConverters
{
    public class BioMorphFace : ObjectBinary
    {
        public Vector3[][] LODs;
        protected override void Serialize(SerializingContainer2 sc)
        {
            sc.Serialize(ref LODs, (SerializingContainer2 sc2, ref Vector3[] lod) =>
            {
                sc2.BulkSerialize(ref lod, SCExt.Serialize, 12);
            });
        }
    }
}
