﻿// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Scene.SceneObjects
{
    using System.Collections;
    using Core.DataReader.Scn;
    using Data;
    using Renderer;
    using UnityEngine;

    [ScnSceneObject(ScnSceneObjectType.StaticOrAnimated)]
    public class StaticOrAnimatedObject : SceneObject
    {
        public StaticOrAnimatedObject(ScnObjectInfo objectInfo, ScnSceneInfo sceneInfo)
            : base(objectInfo, sceneInfo)
        {
        }

        public override GameObject Activate(GameResourceProvider resourceProvider, Color tintColor)
        {
            GameObject sceneGameObject = base.Activate(resourceProvider, tintColor);
            sceneGameObject.AddComponent<StaticOrAnimatedObjectController>().Init(this);
            return sceneGameObject;
        }
    }

    public class StaticOrAnimatedObjectController : MonoBehaviour
    {
        private StaticOrAnimatedObject _sceneObject;
        private Component _effectComponent;

        public void Init(StaticOrAnimatedObject sceneObject)
        {
            _sceneObject = sceneObject;
        }

        private float _startYPosition;
        private void Start()
        {
            _startYPosition = transform.localPosition.y;

            // Randomly play animation if Parameters[1] == 0 for Cvd modeled objects.
            if (_sceneObject.Info.Parameters[1] == 0)
            {
                if (gameObject.GetComponent<CvdModelRenderer>() is {} cvdModelRenderer)
                {
                    StartCoroutine(PlayAnimationRandomly(cvdModelRenderer));
                }
            }
        }

        // Play animation with random wait time.
        private IEnumerator PlayAnimationRandomly(CvdModelRenderer cvdModelRenderer)
        {
            var animationWaiter = new WaitForSeconds(cvdModelRenderer.GetAnimationDuration());
            while (isActiveAndEnabled)
            {
                yield return new WaitForSeconds(UnityEngine.Random.Range(0.5f, 3.5f));
                cvdModelRenderer.PlayAnimation(1f, 1);
                yield return animationWaiter;
            }
        }

        void LateUpdate()
        {
            // Parameters[2] describes animated object's default animation.
            // 0 means no animation. 1 means the object is animated up and down (sine curve).
            // 2 means the object is animated with constant rotation.
            if ((int)_sceneObject.Info.Parameters[2] == 1)
            {
                Transform currentTransform = transform;
                Vector3 currentPosition = currentTransform.localPosition;
                transform.localPosition = new Vector3(currentPosition.x,
                    _startYPosition + Mathf.Sin(Time.time) / 6f,
                    currentPosition.z);
            }
            else if ((int)_sceneObject.Info.Parameters[2] == 2)
            {
                Transform currentTransform = transform;
                transform.RotateAround(currentTransform.position, currentTransform.up, Time.deltaTime * 80f);
            }
        }

        private void OnDisable()
        {
            Destroy(_effectComponent);
        }
    }
}