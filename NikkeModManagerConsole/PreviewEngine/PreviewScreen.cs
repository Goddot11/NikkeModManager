using Microsoft.Xna.Framework;
using NikkeModManagerCore;

namespace NikkeModManagerConsole.PreviewEngine;

public abstract class PreviewScreen
{
    protected Game game;
    protected float SkeletonScale = 1f;

    protected PreviewScreen(Game game)
    {
        this.game = game;
    }

    public abstract void BuildSkeleton(NikkeBundle bundle);

    public abstract void LoadSkeleton(NikkeBundle bundle, string anim);

    public abstract void Update(float deltaTime);

    public abstract void Render();
}