﻿// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.DataReader.Scn;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.Climbable)]
    public class ClimbableObject : SceneObject
    {
        private const float MAX_INTERACTION_DISTANCE = 6f;

        public ClimbableObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override bool IsInteractable(float distance)
        {
            return distance < MAX_INTERACTION_DISTANCE;
        }

        public override void Interact()
        {
            if (Info.ScriptId == 0)
            {
                // For climbable scene object:
                // Parameters[0] = x1
                // Parameters[1] = y1
                // Parameters[2] = x2
                // Parameters[3] = y2
                // Parameters[4] == 1 ? Crossing different layer : Same layer

                CommandDispatcher<ICommand>.Instance.Dispatch(new PlayerActorClimbObjectCommand(Info.Id,
                    new Vector2Int((int)Info.Parameters[0], (int)Info.Parameters[1]),
                    new Vector2Int((int)Info.Parameters[2], (int)Info.Parameters[3]),
                    (int)Info.Parameters[4] == 1));
            }
            else
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new PlayerInteractionTriggeredNotification());
                CommandDispatcher<ICommand>.Instance.Dispatch(
                    new ScriptRunCommand((int)Info.ScriptId));
            }
        }
    }
}