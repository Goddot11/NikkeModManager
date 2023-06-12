using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NikkeModManagerConsole.spine_4_1_0;
using NikkeModManagerConsole.spine_monogame_4_1_0;
using NikkeModManagerCore;
using Color = Microsoft.Xna.Framework.Color;

namespace NikkeModManagerConsole.PreviewEngine;

public class PreviewScreen4_1_0 : PreviewScreen {
    readonly SkeletonRenderer _skeletonRenderer;
    Skeleton _skeleton;
    AnimationState _state;

    private readonly Dictionary<NikkeBundle, Skeleton> _cachedBundles = new Dictionary<NikkeBundle, Skeleton>();

    public PreviewScreen4_1_0(Game game) : base(game) {
        _skeletonRenderer = new SkeletonRenderer(game.GraphicsDevice) {
            PremultipliedAlpha = false
        };
    }

    public override void BuildSkeleton(NikkeBundle bundle) {
        Stream atlasStream = bundle.AtlasFile;
        Stream textureStream = bundle.AtlasTexture;
        Stream skeletonStream = bundle.SkeletonFile;

        TextReader atlasReader = new StreamReader(atlasStream);
        Atlas atlas = new Atlas(atlasReader, "", new XnaTextureLoader(game.GraphicsDevice, textureStream));
        SkeletonBinary binary = new SkeletonBinary(atlas) {
            Scale = SkeletonScale
        };
        SkeletonData skeletonData = binary.ReadSkeletonData(skeletonStream);
        Skeleton skeleton = new Skeleton(skeletonData);
        _cachedBundles[bundle] = skeleton;
        bundle.NotifyLoaded(skeletonData.animations.Select(q => q.name));
    }

    public override void LoadSkeleton(NikkeBundle bundle, string anim) {
        _skeleton = _cachedBundles[bundle];
        AnimationStateData stateData = new AnimationStateData(_skeleton.Data);
        _state = new AnimationState(stateData);
        anim = _skeleton.data.Animations.Select(a => a.name).Contains(anim) ? anim : "";
        _state.SetAnimation(0, anim != "" ? anim : _skeleton.data.Animations.First().name, true);
        _state.Apply(_skeleton);
        _skeleton.UpdateWorldTransform();
    }

    public override void Update(float deltaTime) {
        if (_skeleton == null || _state == null) return;
        _state.Update(deltaTime);
        _state.Apply(_skeleton);
        _skeleton.UpdateWorldTransform();
    }

    public override void Render() {

        float skelX = _skeleton.Data.X;
        float skelY = _skeleton.Data.Y;

        float skelWidth = _skeleton.Data.Width;
        float skelHeight = _skeleton.Data.Height;

        float centerX = skelWidth / 2 + skelX;
        float centerY = skelHeight / 2 + skelY;
        float radius = Math.Max(skelWidth, skelHeight) / 2 * 1.2f;

        float screenWidth = game.GraphicsDevice.Viewport.Width;
        float screenHeight = game.GraphicsDevice.Viewport.Height;

        float zoomX, zoomY;

        if (screenWidth > screenHeight) {
            zoomX = screenWidth / screenHeight;
            zoomY = 1;
        } else {
            zoomX = 1;
            zoomY = screenHeight / screenWidth;
        }
        game.GraphicsDevice.Clear(Color.Black);
        var projection = Matrix.CreateOrthographicOffCenter(
            centerX - radius * zoomX,
            centerX + radius * zoomX,
            -centerY + radius * zoomY,
            -centerY - radius * zoomY,
            1, 0);

        ((BasicEffect)_skeletonRenderer.Effect).Projection = projection;

        _skeletonRenderer.Begin();
        _skeletonRenderer.Draw(_skeleton);
        _skeletonRenderer.End();
    }
}