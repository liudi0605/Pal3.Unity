// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

#if PAL3

namespace Pal3.MiniGame
{
    using System;
    using Command;
    using Command.SceCommands;
    using Script;

    public sealed class SkiMiniGame : IDisposable,
        ICommandExecutor<MiniGameStartSkiCommand>
    {
        private readonly ScriptManager _scriptManager;

        public SkiMiniGame(ScriptManager scriptManager)
        {
            _scriptManager = scriptManager ?? throw new ArgumentNullException(nameof(scriptManager));
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public void Execute(MiniGameStartSkiCommand command)
        {
            _scriptManager.AddScript((uint)command.EndGameScriptId);
        }
    }
}

#endif