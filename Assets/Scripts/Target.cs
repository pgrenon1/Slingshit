using Godot;

[GlobalClass]
public partial class Target : Node3D
{
    [Export] public CollisionObject3D collisionObject3D;
    [Export] public CollisionShape3D collider;
    [Export] public Agent agent;
    
    public Vector3 PreviousLinearVelocity { get; private set; }

    private bool _hasCheckedRigidbody = false;
    private RigidBody3D _rigidBody;
    public RigidBody3D Rigidbody
    {
        get
        {
            if (_hasCheckedRigidbody)
            {
                return _rigidBody;
            }
            else
            {
                _hasCheckedRigidbody = true;
                
                _rigidBody = collisionObject3D as RigidBody3D;
                
                return _rigidBody;
            }
        }
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        
        TargetManager.Instance.RegisterTarget(this);
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        
        TargetManager.Instance.UnregisterTarget(this);
    }

    private void OnBodyEntered(Node node)
    {
        var target = node.GetChildOfType<Target>();
        if (target != null)
        {
            // if (target.Rigidbody != null)
            // {
            //     GD.Print("RigidBody");
            // }
            // else
            // {
            //     GD.Print("StaticBody");
            // }
        }
    }

    public void Launch(Vector3 impulseForce)
    {
        if (agent != null)
            agent.SuspendNavigation = true;
        
        Rigidbody.ApplyImpulse(impulseForce);
    }
}