﻿// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using System.IO;
    using Core.DataLoader;
    using Core.DataReader.Cpk;
    using Core.DataReader.Cvd;
    using Core.DataReader.Pol;
    using Core.DataReader.Scn;
    using Core.GameBox;
    using Data;
    using Dev;
    using Effect;
    using MetaData;
    using Renderer;
    using UnityEngine;

    public abstract class SceneObject
    {
        public ScnObjectInfo Info;
        public GraphicsEffect GraphicsEffect { get; }
        
        public SceneObjectModelType ModelType { get; }
        
        private readonly string _modelFilePath;

        protected SceneObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo, bool hasModel = true)
        {
            Info = objectInfo;

            _modelFilePath = hasModel && !string.IsNullOrEmpty(objectInfo.Name) ?
                GetModelFilePath(objectInfo, sceneInfo) : string.Empty;

            ModelType = SceneObjectModelTypeResolver.GetType(Path.GetFileName(_modelFilePath));
            GraphicsEffect = GetEffectType(objectInfo);
        }

        private GraphicsEffect GetEffectType(ScnObjectInfo objectInfo)  
        {
            if (!objectInfo.Name.StartsWith('+')) return GraphicsEffect.None;
            
            if ((int)objectInfo.Parameters[1] == 1 && ModelType == SceneObjectModelType.CvdModel)
            {
                // Dead object
                return GraphicsEffect.None;
            }

            return EffectTypeResolver.GetEffectByNameAndType(objectInfo.Name, objectInfo.EffectModelType);
        }

        private string GetModelFilePath(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
        {
            var separator = CpkConstants.DirectorySeparator;
            var modelFilePath = string.Empty;

            if (objectInfo.Name.StartsWith('_'))
            {
                modelFilePath = $"{sceneInfo.CityName}{CpkConstants.FileExtension}{separator}" +
                                $"{sceneInfo.Model}{separator}{objectInfo.Name}";
            }
            else if (objectInfo.Name.StartsWith('+'))
            {
                // Special vfx effect.
            }
            else if (!objectInfo.Name.Contains('.'))
            {
                modelFilePath = $"{FileConstants.BaseDataCpkPathInfo.cpkName}{separator}item" +
                                $"{separator}{objectInfo.Name}{separator}{objectInfo.Name}.pol";
            }
            else
            {
                modelFilePath = $"{FileConstants.BaseDataCpkPathInfo.cpkName}{separator}object" +
                                $"{separator}{objectInfo.Name}";
            }

            return modelFilePath;
        }

        public virtual GameObject Activate(GameResourceProvider resourceProvider,
            Color tintColor)
        {
            var sceneGameObject = new GameObject($"Object_{Info.Id}_{Info.Type}");

            // Attach SceneObjectInfo to the GameObject for better debuggability
            #if UNITY_EDITOR
            var infoPresenter = sceneGameObject.AddComponent<SceneObjectInfoPresenter>();
            infoPresenter.sceneObjectInfo = Info;
            #endif

            if (ModelType == SceneObjectModelType.PolModel)
            {
                (PolFile PolFile, ITextureResourceProvider TextureProvider) poly = resourceProvider.GetPol(_modelFilePath);
                var sceneObjectRenderer = sceneGameObject.AddComponent<PolyModelRenderer>();
                sceneObjectRenderer.Render(poly.PolFile,
                    resourceProvider.GetMaterialFactory(),
                    poly.TextureProvider,
                    tintColor);
            }
            else if (ModelType == SceneObjectModelType.CvdModel)
            {
                (CvdFile CvdFile, ITextureResourceProvider TextureProvider) cvd = resourceProvider.GetCvd(_modelFilePath);
                var sceneObjectRenderer = sceneGameObject.AddComponent<CvdModelRenderer>();

                var initTime = 0f;
                // if (Info.Type == ScnSceneObjectType.Switch &&
                //     Info.Parameters[0] == 1 &&
                //     Info.Parameters[1] == 1)
                // {
                //     initTime = cvd.CvdFile.AnimationDuration;
                // }

                sceneObjectRenderer.Init(cvd.CvdFile,
                    resourceProvider.GetMaterialFactory(),
                    cvd.TextureProvider,
                    tintColor,
                    initTime);

                if (Info.Type == ScnSceneObjectType.General)
                {
                    sceneObjectRenderer.PlayAnimation();
                }
            }

            sceneGameObject.transform.position = GameBoxInterpreter.ToUnityPosition(Info.Position);
            #if PAL3
            sceneGameObject.transform.rotation =
                Quaternion.Euler(Info.XRotation, -Info.YRotation, 0f);
            #elif PAL3A
            sceneGameObject.transform.rotation =
                Quaternion.Euler(Info.XRotation, -Info.YRotation, Info.ZRotation);
            #endif

            if (GraphicsEffect != GraphicsEffect.None &&
                EffectTypeResolver.GetEffectComponentType(GraphicsEffect) is {} effectComponentType)
            {
                Component effectComponent = sceneGameObject.AddComponent(effectComponentType);
                #if PAL3
                var effectParameter = Info.EffectModelType;
                #elif PAL3A
                var effectParameter = (uint)Info.Parameters[5];
                #endif
                Debug.Log($"Adding {GraphicsEffect} [{effectParameter}] effect for scene object {Info.Id}");
                (effectComponent as IEffect)!.Init(resourceProvider, effectParameter);   
            }

            return sceneGameObject;
        }

        public virtual bool IsInteractable(float distance)
        {
            return false;
        }

        public virtual void Interact()
        {
        }
    }
}