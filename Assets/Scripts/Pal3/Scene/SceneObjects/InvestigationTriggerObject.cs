// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.DataReader.Scn;
    using Core.GameBox;
    using Core.Renderer;
    using Data;
    using MetaData;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.Pushable)]
    [ScnSceneObject(ScnSceneObjectType.Switch)]
    [ScnSceneObject(ScnSceneObjectType.LiftingMechanism)]
    [ScnSceneObject(ScnSceneObjectType.VirtualInvestigationTrigger)]
    [ScnSceneObject(ScnSceneObjectType.InvestigationTriggerObject)]
    public class InvestigationTriggerObject : SceneObject
    {
        private const float MAX_INTERACTION_DISTANCE = 5f;

        public InvestigationTriggerObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);
            sceneGameObject.AddComponent<InvestigationTriggerController>().Init(this);
            return sceneGameObject;
        }

        public override bool IsInteractable(float distance)
        {
            return distance < MAX_INTERACTION_DISTANCE;
        }

        public override void Interact()
        {
            if (Info.Times != 0xFF)
            {
                if (Info.Times <= 0) return;
                else Info.Times--;
            }

            CommandDispatcher<ICommand>.Instance.Dispatch(
                new PlayerInteractionTriggeredNotification());
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ActorStopActionAndStandCommand(ActorConstants.PlayerActorVirtualID));
            CommandDispatcher<ICommand>.Instance.Dispatch(
                new ScriptRunCommand((int)Info.ScriptId));
        }
    }

    public class InvestigationTriggerController : MonoBehaviour
    {
        private InvestigationTriggerObject _investigationTriggerBox;
        private Bounds _bounds;

        public void Init(InvestigationTriggerObject investigationTriggerBox)
        {
            _investigationTriggerBox = investigationTriggerBox;

            // 2: 模型触发
            if (investigationTriggerBox.Info.TriggerType == 2 &&
                GetComponentInChildren<StaticMeshRenderer>() is { } meshRenderer)
            {
                _bounds = meshRenderer.GetRendererBounds();
            }
            else
            {
                _bounds.SetMinMax(GameBoxInterpreter.ToUnityPosition(_investigationTriggerBox.Info.BoundBox.Min),
                    GameBoxInterpreter.ToUnityPosition(_investigationTriggerBox.Info.BoundBox.Max));
            }

            //Utility.DrawBounds(_bounds);

            // Set active Y position of the lifting mechanism
            // TODO: impl of the lifting logic
            if (_investigationTriggerBox.Info.Type == ScnSceneObjectType.LiftingMechanism)
            {
                Vector3 oldPosition = transform.position;
                var activeYPosition = ((float)_investigationTriggerBox.Info.Parameters[0] /
                                       GameBoxInterpreter.GameBoxUnitToUnityUnit) - 0.02f; // A small Y offset to ensure actor shadow is properly rendered
                transform.position = new Vector3(oldPosition.x, activeYPosition, oldPosition.z);
            }
        }
    }
}