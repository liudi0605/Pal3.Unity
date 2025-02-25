﻿// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Command.SceCommands
{
    #if PAL3A
    [SceCommand(173, "在角色头顶出现表情符号，" +
                    "参数：角色ID，表情编号ID")]
    public class ActorShowEmoji2Command : ICommand
    {
        public ActorShowEmoji2Command(int actorId, int emojiId)
        {
            ActorId = actorId;
            EmojiId = emojiId;
        }

        // 角色ID为-1时 (byte值就是255) 表示当前受玩家操作的主角
        public int ActorId { get; }
        public int EmojiId { get; }
    }
    #endif
}