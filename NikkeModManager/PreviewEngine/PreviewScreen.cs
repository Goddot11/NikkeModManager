using MonoGame.Forms.Controls;
using NikkeModManagerCore;

namespace NikkeModManager.PreviewEngine;

public abstract class PreviewScreen
{
    protected MonoGameControl game;
    protected float SkeletonScale = 1f;

    protected PreviewScreen(MonoGameControl game)
    {
        this.game = game;
    }

    public abstract void BuildSkeleton(NikkeBundle bundle);

    public abstract void LoadSkeleton(NikkeBundle bundle, string anim);

    public abstract void Update(float deltaTime);

    public abstract void Render();
}