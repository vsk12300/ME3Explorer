﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct3D11;
using ME3Explorer.Packages;

namespace ME3Explorer.Scene3D
{
    /// <summary>
    /// Loads and caches textures for a Direct3D11 renderer
    /// </summary>
    public class PreviewTextureCache : IDisposable
    {
        /// <summary>
        /// Stores a texture and load state in the cache.
        /// </summary>
        public class PreviewTextureEntry : IDisposable
        {
            /// <summary>
            /// The full path of the pcc file;
            /// </summary>
            public string PCCPath;

            public int ExportID;

            /// <summary>
            /// The Direct3D ShaderResourceView for binding to shaders.
            /// </summary>
            public ShaderResourceView TextureView;

            /// <summary>
            /// The Direct3D texture for SHaderResourceView creation.
            /// </summary>
            public Texture2D Texture;

            /// <summary>
            /// Creates a new cache entry for the given texture.
            /// </summary>
            public PreviewTextureEntry(string pcc, int exportid)
            {
                PCCPath = pcc;
                ExportID = exportid;
            }

            /// <summary>
            /// Disposes <see cref="TextureView"/> and <see cref="Texture"/> if they have been loaded.
            /// </summary>
            public void Dispose()
            {
                TextureView?.Dispose();
                Texture?.Dispose();
            }
        }

        /// <summary>
        /// The DIrect3D11 device to create textures and resource views with.
        /// </summary>
        public Device Device { get; }

        /// <summary>
        /// Creates a new PreviewTextureCache.
        /// </summary>
        /// <param name="device">The DIrect3D11 device to create textures and resource views with.</param>
        public PreviewTextureCache(Device device)
        {
            Device = device;
        }

        /// <summary>
        /// Disposes all the textures and resource views.
        /// </summary>
        public void Dispose()
        {
            foreach (PreviewTextureEntry e in cache)
            {
                e.Dispose();
            }
            cache.Clear();
        }

        /// <summary>
        /// Stores loaded textures by their full name.
        /// </summary>
        private readonly List<PreviewTextureEntry> cache = new List<PreviewTextureEntry>();

        /// <summary>
        /// Queues a texture for eventual loading.
        /// </summary>
        /// <param name="pcc">The full path of the pcc where the texture export is.</param>
        /// <param name="exportid"></param>
        public PreviewTextureEntry LoadTexture(string pcc, int exportid)
        {
            foreach (PreviewTextureEntry e in cache)
            {
                if (e.PCCPath == pcc && e.ExportID == exportid)
                {
                    return e;
                }
            }
            using (ME3Package texpcc = MEPackageHandler.OpenME3Package(pcc))
            {
                PreviewTextureEntry entry = new PreviewTextureEntry(pcc, exportid);
                Unreal.Classes.Texture2D metex = new Unreal.Classes.Texture2D(texpcc, exportid);
                try
                {
                    entry.Texture = metex.generatePreviewTexture(Device, out Texture2DDescription _);
                    entry.TextureView = new ShaderResourceView(Device, entry.Texture);
                    cache.Add(entry);
                    return entry;
                } catch
                {
                    return null;
                }
            }
        }
    }
}
