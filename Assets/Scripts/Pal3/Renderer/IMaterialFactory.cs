﻿// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Renderer
{
    using Core.GameBox;
    using UnityEngine;

    public interface IMaterialFactory
    {
        /// <summary>
        /// Create a material for effect sprite without alpha channel.
        /// </summary>
        /// <returns>Material</returns>
        public Material CreateOpaqueSpriteMaterial();
        
        /// <summary>
        /// Create a material for effect sprite without alpha channel.
        /// </summary>
        /// <param name="texture">Texture</param>
        /// <returns>Material</returns>
        public Material CreateOpaqueSpriteMaterial(Texture2D texture);

        /// <summary>
        /// Create standard materials.
        /// </summary>
        /// <param name="mainTexture">Main texture</param>
        /// <param name="shadowTexture">Shadow texture</param>
        /// <param name="tintColor">Tint color</param>
        /// <param name="blendFlag">Blend flag</param>
        /// <param name="transparentThreshold">Transparent threshold</param>
        /// <returns>Materials</returns>
        public Material[] CreateStandardMaterials(
            Texture2D mainTexture,
            Texture2D shadowTexture,
            Color tintColor,
            GameBoxBlendFlag blendFlag,
            float transparentThreshold);

        /// <summary>
        /// Create material for water surface
        /// </summary>
        /// <param name="mainTexture">Main texture</param>
        /// <param name="shadowTexture">Shadow texture</param>
        /// <param name="alpha">Opacity</param>
        /// <returns>Material</returns>
        public Material CreateWaterMaterial(Texture2D mainTexture,
            Texture2D shadowTexture,
            float alpha);
    }
}