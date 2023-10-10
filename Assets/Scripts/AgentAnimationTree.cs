using Godot;
using System;

public partial class AgentAnimationTree : AnimationTree
{
    [Export] public bool isWalking = false;

    public bool IsWalking
    {
        get => isWalking;
        set => isWalking = value;
    }
}
