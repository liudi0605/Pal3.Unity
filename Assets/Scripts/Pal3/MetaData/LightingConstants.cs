﻿// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.MetaData
{
    using System.Collections.Generic;
    using UnityEngine;

    public static class LightingConstants
    {
        /// <summary>
        /// This is the lighting color override for the target scene city.
        /// </summary>
        #if PAL3
        public static readonly Dictionary<string, Color> MainLightColorInfoGlobal = new()
        {
            { "q13", new Color(40f / 255f, 80f / 255f, 200f / 255f) },           // 酆都
            { "q14", new Color(100f / 255f, 100f / 255f, 200f / 255f) },         // 鬼界外围
            { "m24", new Color(30f / 255f, 80f / 255f, 200f / 255f) },           // 剑冢
        };
        #elif PAL3A
        public static readonly Dictionary<string, Color> MainLightColorInfoGlobal = new()
        {
            { "m02", new Color(10f / 255f, 30f / 255f, 60f / 255f) },       // 地脉门户
            { "m15", new Color(80f / 255f, 130f / 255f, 200f / 255f) },     // 月光城
        };
        #endif
        
        /// <summary>
        /// This is the lighting color override for the target scene.
        /// </summary>
        #if PAL3
        public static readonly Dictionary<string, Color> MainLightColorInfo = new()
        {
            { "q02_n14_0",  new Color(75f / 255f, 75f / 255f, 75f / 255f) },      // 地牢1
            { "q02_n16_0",  new Color(30f / 255f, 30f / 255f, 30f / 255f) },      // 地牢2
            { "q07_q07a_1", new Color(120f / 255f, 50f / 255f, 30f / 255f) },     // 安宁村黄昏
            { "m06_4_-1",   new Color(0f / 255f, 0f / 255f, 80f / 255f) },        // 蓬莱密道
        };
        #elif PAL3A
        public static readonly Dictionary<string, Color> MainLightColorInfo = new()
        {
        };
        #endif
        
        /// <summary>
        /// This is the lighting color override for the target scene.
        /// </summary>
        #if PAL3
        public static readonly Dictionary<string, Quaternion> MainLightRotationInfo = new()
        {
            { "q01_yn01a_1", Quaternion.Euler(130f, 40f, 0f) },     // 重楼当剑
            { "q07_q07a_1",  Quaternion.Euler(145f, 50f, 0f) },     // 安宁村黄昏
            { "m06_1_-1",    Quaternion.Euler(30f, -50f, 0f) },     // 蓬莱码头
        };
        #elif PAL3A
        public static readonly Dictionary<string, Quaternion> MainLightRotationInfo = new()
        {
        };
        #endif
    }
}