﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Packages;

namespace LegendaryExplorerCore.Unreal.BinaryConverters
{
    public class UTexture2D : ObjectBinary
    {
        public List<Texture2DMipMap> Mips;
        public int Unk1;
        public Guid TextureGuid;

        protected override void Serialize(SerializingContainer2 sc)
        {
            if (!sc.Game.IsGame3())
            {
                int dummy = 0;
                sc.Serialize(ref dummy);
                sc.Serialize(ref dummy);
                sc.Serialize(ref dummy);
                sc.SerializeFileOffset();
            }
            sc.Serialize(ref Mips, SCExt.Serialize);
            if (sc.Game != MEGame.UDK)
            {
                sc.Serialize(ref Unk1);
            }

            if (sc.Game > MEGame.ME1)
            {
                sc.Serialize(ref TextureGuid);
            }

            if (sc.Game == MEGame.UDK)
            {
                var zeros = new byte[32];
                sc.Serialize(ref zeros, 32);
            }
            if (sc.Game == MEGame.ME3 || sc.Game.IsLEGame())
            {
                int dummy = 0;
                sc.Serialize(ref dummy);
            }
        }

        public class Texture2DMipMap
        {
            public StorageTypes StorageType;
            public int UncompressedSize;
            public int CompressedSize;
            public int DataOffset;
            public byte[] Mip;
            public int SizeX;
            public int SizeY;

            // Utility stuff
            /// <summary>
            /// The start of this mipmap 'struct' in the export, at the time it was read
            /// </summary>
            public int MipInfoOffsetFromBinStart;

            public bool IsLocallyStored => ((int)StorageType & (int)StorageFlags.externalFile) == 0;
            public bool IsCompressed =>
                ((int)StorageType & (int)StorageFlags.compressedLZO) != 0 ||
                ((int)StorageType & (int)StorageFlags.compressedLZMA) != 0 ||
                ((int)StorageType & (int)StorageFlags.compressedZlib) != 0 ||
                ((int)StorageType & (int)StorageFlags.compressedOodle) != 0;
        }
    }

    public class LightMapTexture2D : UTexture2D
    {
        public ELightMapFlags LightMapFlags;
        protected override void Serialize(SerializingContainer2 sc)
        {
            base.Serialize(sc);
            if (sc.Game >= MEGame.ME3)
            {
                int lmf = (int)LightMapFlags;
                sc.Serialize(ref lmf);
                LightMapFlags = (ELightMapFlags)lmf;
            }
        }
    }
    public enum ELightMapFlags
    {
        LMF_None,
        LMF_Streamed,
        LMF_SimpleLightmap
    }

    public static partial class SCExt
    {
        public static void Serialize(this SerializingContainer2 sc, ref UTexture2D.Texture2DMipMap mip)
        {
            if (sc.IsLoading)
            {
                mip = new UTexture2D.Texture2DMipMap();
                mip.MipInfoOffsetFromBinStart = (int)sc.ms.Position; // this is used to update the DataOffset later
            }

            int mipStorageType = (int)mip.StorageType;
            sc.Serialize(ref mipStorageType);
            mip.StorageType = (StorageTypes)mipStorageType;
            sc.Serialize(ref mip.UncompressedSize);
            sc.Serialize(ref mip.CompressedSize);
            if (sc.IsSaving && mip.IsLocallyStored)
            {
                // This code is not accurate as the start offset may be 0 if the export is new and doesn't have a DataOffset yet.
                sc.SerializeFileOffset();
            }
            else
            {
                sc.Serialize(ref mip.DataOffset);
            }

            if (mip.IsLocallyStored)
            {
                sc.Serialize(ref mip.Mip, mip.CompressedSize);
            }
            sc.Serialize(ref mip.SizeX);
            sc.Serialize(ref mip.SizeY);
        }
    }
}