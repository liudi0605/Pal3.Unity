﻿// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Renderer
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;
    using Core.DataLoader;
    using Core.DataReader.Cvd;
    using Core.GameBox;
    using Core.Renderer;
    using Core.Utils;
    using Dev;
    using UnityEngine;

    /// <summary>
    /// CVD(.cvd) model renderer
    /// </summary>
    public class CvdModelRenderer : MonoBehaviour
    {
        private readonly Dictionary<string, Texture2D> _textureCache = new ();
        private readonly List<(CvdGeometryNode, Dictionary<int, RenderMeshComponent>)> _renderers = new ();

        private Color _tintColor;

        private float _animationDuration;
        private Coroutine _animation;
        private CancellationTokenSource _animationCts;
        
        private const float TRANSPARENT_THRESHOLD = 1.0f;

        public void Init(CvdFile cvdFile,
            IMaterialFactory materialFactory,
            ITextureResourceProvider textureProvider,
            Color tintColor,
            float time)
        {
            _animationDuration = cvdFile.AnimationDuration;
            _tintColor = tintColor;

            foreach (CvdGeometryNode node in cvdFile.RootNodes)
            {
                BuildTextureCache(node, textureProvider, _textureCache);
            }

            var root = new GameObject("Cvd Mesh");

            for (var i = 0; i < cvdFile.RootNodes.Length; i++)
            {
                var hashKey = $"{i}";
                RenderMeshInternal(
                    time,
                    hashKey,
                    cvdFile.RootNodes[i],
                    _textureCache,
                    materialFactory,
                    root);
            }

            root.transform.SetParent(gameObject.transform, false);
        }

        public float GetAnimationDuration()
        {
            return _animationDuration;
        }

        private void BuildTextureCache(CvdGeometryNode node,
            ITextureResourceProvider textureProvider,
            Dictionary<string, Texture2D> textureCache)
        {
            if (node.IsGeometryNode)
            {
                foreach (CvdMeshSection meshSection in node.Mesh.MeshSections)
                {
                    if (string.IsNullOrEmpty(meshSection.TextureName)) continue;
                    if (textureCache.ContainsKey(meshSection.TextureName)) continue;
                    Texture2D texture2D = textureProvider.GetTexture(meshSection.TextureName);
                    if (texture2D != null) textureCache[meshSection.TextureName] = texture2D;
                }
            }

            foreach (CvdGeometryNode childNode in node.Children)
            {
                BuildTextureCache(childNode, textureProvider, textureCache);
            }
        }

        private int GetFrameIndex(float[] keyTimes, float time)
        {
            int frameIndex = 0;

            if (keyTimes.Length == 1 ||
                time >= keyTimes[^1])
            {
                frameIndex = keyTimes.Length - 1;
            }
            else
            {
                frameIndex = Utility.GetFloorIndex(keyTimes, time);
            }

            return frameIndex;
        }

        private Matrix4x4 GetFrameMatrix(float time, CvdGeometryNode node)
        {
            Vector3 position = GetPosition(time, node.PositionKeyInfos);
            Quaternion rotation = GetRotation(time, node.RotationKeyInfos);
            (Vector3 scale, Quaternion scaleRotation) = GetScale(time, node.ScaleKeyInfos);

            Quaternion scalePreRotation = scaleRotation;
            Quaternion scaleInverseRotation = Quaternion.Inverse(scalePreRotation);

            return Matrix4x4.Translate(position)
                   * Matrix4x4.Scale(new Vector3(node.Scale, node.Scale, node.Scale))
                   * Matrix4x4.Rotate(rotation)
                   * Matrix4x4.Rotate(scalePreRotation)
                   * Matrix4x4.Scale(scale)
                   * Matrix4x4.Rotate(scaleInverseRotation);
        }

        private void RenderMeshInternal(
            float time,
            string meshName,
            CvdGeometryNode node,
            Dictionary<string, Texture2D> textureCache,
            IMaterialFactory materialFactory,
            GameObject parent)
        {
            var meshObject = new GameObject(meshName);
            meshObject.transform.SetParent(parent.transform, false);

            if (node.IsGeometryNode)
            {
                var nodeMeshes = (node, new Dictionary<int, RenderMeshComponent>());

                var frameIndex = GetFrameIndex(node.Mesh.AnimationTimeKeys, time);
                Matrix4x4 frameMatrix = GetFrameMatrix(time, node);

                var influence = 0f;
                if (time > Mathf.Epsilon && frameIndex + 1 < node.Mesh.AnimationTimeKeys.Length)
                {
                    influence = (time - node.Mesh.AnimationTimeKeys[frameIndex]) /
                                (node.Mesh.AnimationTimeKeys[frameIndex + 1] -
                                 node.Mesh.AnimationTimeKeys[frameIndex]);
                }

                for (var i = 0; i < node.Mesh.MeshSections.Length; i++)
                {
                    CvdMeshSection meshSection = node.Mesh.MeshSections[i];

                    var sectionHashKey = $"{meshName}_{i}";

                    var meshDataBuffer = new MeshDataBuffer
                    {
                        VertexBuffer = new Vector3[meshSection.FrameVertices[frameIndex].Length],
                        NormalBuffer = new Vector3[meshSection.FrameVertices[frameIndex].Length],
                        UvBuffer = new Vector2[meshSection.FrameVertices[frameIndex].Length],
                    };
                    
                    UpdateMeshDataBuffer(ref meshDataBuffer,
                        meshSection,
                        frameIndex,
                        influence,
                        frameMatrix);

                    if (string.IsNullOrEmpty(meshSection.TextureName) ||
                        !textureCache.ContainsKey(meshSection.TextureName)) continue;

                    var meshSectionObject = new GameObject($"{sectionHashKey}");

                    // Attach BlendFlag and GameBoxMaterial to the GameObject for better debuggability.
                    #if UNITY_EDITOR
                    var materialInfoPresenter = meshObject.AddComponent<MaterialInfoPresenter>();
                    materialInfoPresenter.blendFlag = meshSection.BlendFlag;
                    materialInfoPresenter.material = meshSection.Material;
                    #endif

                    Material[] materials = materialFactory.CreateStandardMaterials(
                        textureCache[meshSection.TextureName],
                        shadowTexture: null, // CVD models don't have shadow textures
                        _tintColor,
                        meshSection.BlendFlag,
                        TRANSPARENT_THRESHOLD);
                    
                    var meshRenderer = meshSectionObject.AddComponent<StaticMeshRenderer>();
                    Mesh renderMesh = meshRenderer.Render(
                        ref meshDataBuffer.VertexBuffer,
                        ref meshSection.Triangles,
                        ref meshDataBuffer.NormalBuffer,
                        ref meshDataBuffer.UvBuffer,
                        ref meshDataBuffer.UvBuffer,
                        ref materials,
                        true);

                    renderMesh.RecalculateNormals();
                    renderMesh.RecalculateTangents();
                    
                    nodeMeshes.Item2[i] = new RenderMeshComponent
                    {
                        Mesh = renderMesh,
                        MeshRenderer = meshRenderer,
                        MeshDataBuffer = meshDataBuffer
                    };

                    meshSectionObject.transform.SetParent(meshObject.transform, false);
                }

                _renderers.Add(nodeMeshes);
            }

            for (var i = 0; i < node.Children.Length; i++)
            {
                var childMeshName = $"{meshName}-{i}";
                RenderMeshInternal(time,
                    childMeshName,
                    node.Children[i],
                    textureCache,
                    materialFactory,
                    meshObject);
            }
        }

        private void UpdateMeshDataBuffer(ref MeshDataBuffer meshDataBuffer,
            CvdMeshSection meshSection,
            int frameIndex,
            float influence,
            Matrix4x4 matrix)
        {
            var frameVertices = meshSection.FrameVertices[frameIndex];

            if (influence < Mathf.Epsilon)
            {
                for (var i = 0; i < frameVertices.Length; i++)
                {
                    meshDataBuffer.VertexBuffer[i] = matrix.MultiplyPoint3x4(frameVertices[i].Position);
                    meshDataBuffer.UvBuffer[i] = frameVertices[i].Uv;
                }
            }
            else
            {
                for (var i = 0; i < frameVertices.Length; i++)
                {
                    var toFrameVertices = meshSection.FrameVertices[frameIndex + 1];
                    Vector3 lerpPosition = Vector3.Lerp(frameVertices[i].Position, toFrameVertices[i].Position, influence);
                    meshDataBuffer.VertexBuffer[i] = matrix.MultiplyPoint3x4(lerpPosition);
                    Vector2 lerpUv = Vector2.Lerp(frameVertices[i].Uv, toFrameVertices[i].Uv, influence);
                    meshDataBuffer.UvBuffer[i] = lerpUv;
                }
            }
        }

        private void UpdateMesh(float time)
        {
            foreach ((CvdGeometryNode node, var renderMeshComponents) in _renderers)
            {
                var frameIndex = GetFrameIndex(node.Mesh.AnimationTimeKeys, time);
                Matrix4x4 frameMatrix = GetFrameMatrix(time, node);

                var influence = 0f;
                if (time > Mathf.Epsilon && frameIndex + 1 < node.Mesh.AnimationTimeKeys.Length)
                {
                    influence = (time - node.Mesh.AnimationTimeKeys[frameIndex]) /
                                (node.Mesh.AnimationTimeKeys[frameIndex + 1] -
                                 node.Mesh.AnimationTimeKeys[frameIndex]);
                }

                for (var i = 0; i < node.Mesh.MeshSections.Length; i++)
                {
                    if (!renderMeshComponents.ContainsKey(i)) continue;

                    RenderMeshComponent renderMeshComponent = renderMeshComponents[i];
                    if (!renderMeshComponent.MeshRenderer.IsVisible()) continue;

                    CvdMeshSection meshSection = node.Mesh.MeshSections[i];

                    MeshDataBuffer meshDataBuffer = renderMeshComponent.MeshDataBuffer;
                    UpdateMeshDataBuffer(ref meshDataBuffer,
                        meshSection,
                        frameIndex,
                        influence,
                        frameMatrix);

                    renderMeshComponent.Mesh.vertices = meshDataBuffer.VertexBuffer;
                    renderMeshComponent.Mesh.uv = meshDataBuffer.UvBuffer;
                    renderMeshComponent.Mesh.RecalculateBounds();
                }
            }
        }

        public void PlayAnimation(float timeScale = 1f, int loopCount = -1, float fps = -1f)
        {
            if (_animationDuration < Mathf.Epsilon) return;

            if (_renderers.Count == 0)
            {
                throw new Exception("Animation not initialized.");
            }

            StopAnimation();

            if (_animation != null) StopCoroutine(_animation);

            _animationCts = new CancellationTokenSource();
            WaitForSeconds animationDelay = fps <= 0 ? null : new WaitForSeconds(1 / fps);
            _animation = StartCoroutine(PlayAnimationInternal(timeScale,
                loopCount,
                animationDelay,
                _animationCts.Token));
        }

        private IEnumerator PlayAnimationInternal(float timeScale,
            int loopCount,
            WaitForSeconds animationDelay,
            CancellationToken cancellationToken)
        {
            if (loopCount == -1)
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    yield return PlayOneTimeAnimationInternal(timeScale, animationDelay, cancellationToken);
                }
            }
            else if (loopCount > 0)
            {
                while (!cancellationToken.IsCancellationRequested && --loopCount >= 0)
                {
                    yield return PlayOneTimeAnimationInternal(timeScale, animationDelay, cancellationToken);
                }
            }
        }

        private IEnumerator PlayOneTimeAnimationInternal(float timeScale,
            WaitForSeconds animationDelay,
            CancellationToken cancellationToken)
        {
            var startTime = Time.timeSinceLevelLoad;

            while (!cancellationToken.IsCancellationRequested)
            {
                var currentTime = timeScale > 0 ? 
                        (Time.timeSinceLevelLoad - startTime) * timeScale :
                        (_animationDuration - (Time.timeSinceLevelLoad - startTime)) * -timeScale;

                if ((timeScale > 0f && currentTime >= _animationDuration) ||
                    (timeScale < 0f && currentTime <= 0f))
                {
                    yield break;
                }

                UpdateMesh(currentTime);

                yield return animationDelay;
            }
        }

        private Vector3 GetPosition(float time, CvdAnimationPositionKeyFrame[] nodePositionInfo)
        {
            if (nodePositionInfo.Length == 1) return nodePositionInfo[0].Position;

            // TODO: Use binary search and interpolate value based on curve type
            for (var i = 0; i < nodePositionInfo.Length - 1; i++)
            {
                CvdAnimationPositionKeyFrame toKeyFrame = nodePositionInfo[i + 1];
                if (time < toKeyFrame.Time)
                {
                    CvdAnimationPositionKeyFrame fromKeyFrame =  nodePositionInfo[i];
                    var influence = (time - fromKeyFrame.Time) /
                                    (toKeyFrame.Time - fromKeyFrame.Time);
                    return Vector3.Lerp(fromKeyFrame.Position, toKeyFrame.Position, influence);
                }
            }

            return nodePositionInfo[^1].Position;
        }

        private (Vector3 Scale, Quaternion Rotation) GetScale(float time,
            CvdAnimationScaleKeyFrame[] nodeScaleInfo)
        {
            if (nodeScaleInfo.Length == 1)
            {
                CvdAnimationScaleKeyFrame scaleKeyFrame = nodeScaleInfo[0];
                return (scaleKeyFrame.Scale, scaleKeyFrame.Rotation);
            }

            // TODO: Use binary search and interpolate value based on curve type
            for (var i = 0; i < nodeScaleInfo.Length - 1; i++)
            {
                CvdAnimationScaleKeyFrame toKeyFrame = nodeScaleInfo[i + 1];
                if (time < toKeyFrame.Time)
                {
                    CvdAnimationScaleKeyFrame fromKeyFrame = nodeScaleInfo[i];
                    var influence = (time - fromKeyFrame.Time) /
                                    (toKeyFrame.Time - fromKeyFrame.Time);
                    Quaternion calculatedRotation = Quaternion.Lerp(
                        fromKeyFrame.Rotation,
                        toKeyFrame.Rotation,
                        influence);
                    return (Vector3.Lerp(fromKeyFrame.Scale, toKeyFrame.Scale, influence), calculatedRotation);
                }
            }

            CvdAnimationScaleKeyFrame lastKeyFrame = nodeScaleInfo[^1];
            return (lastKeyFrame.Scale, lastKeyFrame.Rotation);
        }

        private Quaternion GetRotation(float time,
            CvdAnimationRotationKeyFrame[] nodeRotationInfo)
        {
            if (nodeRotationInfo.Length == 1) return nodeRotationInfo[0].Rotation;

            // TODO: Use binary search and interpolate value based on curve type
            for (var i = 0; i < nodeRotationInfo.Length - 1; i++)
            {
                CvdAnimationRotationKeyFrame toKeyFrame = nodeRotationInfo[i + 1];
                if (time < toKeyFrame.Time)
                {
                    CvdAnimationRotationKeyFrame fromKeyFrame = nodeRotationInfo[i];
                    var influence = (time - fromKeyFrame.Time) /
                                    (toKeyFrame.Time - fromKeyFrame.Time);
                    return Quaternion.Lerp(
                        fromKeyFrame.Rotation,
                        toKeyFrame.Rotation,
                        influence);
                }
            }

            return nodeRotationInfo[^1].Rotation;
        }

        public void StopAnimation()
        {
            if (_animation != null)
            {
                _animationCts.Cancel();
                StopCoroutine(_animation);
                _animation = null;
            }
        }

        private void OnDisable()
        {
            Dispose();
        }

        private void Dispose()
        {
            StopAnimation();
            foreach (StaticMeshRenderer meshRenderer in GetComponentsInChildren<StaticMeshRenderer>())
            {
                Destroy(meshRenderer.gameObject);
            }
        }
    }
}