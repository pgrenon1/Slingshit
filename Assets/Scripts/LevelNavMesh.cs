using Godot;

[GlobalClass]
public partial class LevelNavMesh : NavigationRegion3D
{
    public override void _Ready()
    {
        base._Ready();

        LevelManager.Instance.NavigationRegion3D = this;
    }
}