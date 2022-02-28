﻿// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    [SceCommand(83, "设置战斗为必败")]
    public class CombatSetUnwinnableCommand : ICommand
    {
        public CombatSetUnwinnableCommand() {}
    }
}