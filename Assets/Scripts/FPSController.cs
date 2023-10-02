using System.Collections.Generic;
using Godot;

[GlobalClass]
public partial class FPSController : RigidBody3D
{
    [Export] float speed = 20f;

    [Export(PropertyHint.Range, "0.1,1.0")]
    float mouse_sensitivity = 0.3f;
    [Export(PropertyHint.Range, "-90,0,1")]
    float min_pitch = -90f;
    [Export(PropertyHint.Range, "0,90,1")]
    float max_pitch = 90f;

    [Export] public float groundAcceleration = 10f;
    [Export] public float airAcceleration = 10f;
    
    [Export] public Node3D cameraPivot;
    [Export] public Camera3D camera;
    [Export] public GroundCheck groundCheck;
    [Export] public RayCast3D lineOfSightRayCast;
    
    private bool _isJumping = false;
    private Plane _horizontalPlane = new(Vector3.Up);
    private float _acceleration;
    private Vector3 _desiredVelocity;
    private Target _mostLikelyTarget = null;
    private TargetHold[] _targetHolds = new TargetHold[2];
    private TargetHold _latestReleasedTargetHold;
    
    public float MovementSpeedSquared { get; private set; }
    public Vector3 ForwardDirection => -cameraPivot.Transform.Basis.Z;

    private const string UP = "up";
    private const string DOWN = "down";
    private const string RIGHT = "right";
    private const string LEFT = "left";
    private const string UI_CANCEL = "ui_cancel";
    private const string SHOOT1 = "shoot1";
    private const string SHOOT2 = "shoot2";
    private const string JUMP = "jump";

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        MovementSpeedSquared = speed * speed;
        
        if (Input.IsActionJustPressed(UI_CANCEL))
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
        
        if (Input.IsActionJustPressed(SHOOT1))
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
        }
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        
        if (@event is InputEventMouseMotion motionEvent)
        {
            Vector3 rotationDegrees = cameraPivot.RotationDegrees;
            rotationDegrees.Y -= motionEvent.Relative.X * mouse_sensitivity;
            cameraPivot.RotationDegrees = rotationDegrees;
            
            rotationDegrees = cameraPivot.RotationDegrees;
            rotationDegrees.X -= motionEvent.Relative.Y * mouse_sensitivity;
            rotationDegrees.X = Mathf.Clamp(rotationDegrees.X, min_pitch, max_pitch);
            cameraPivot.RotationDegrees = rotationDegrees;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        float deltaFloat = (float)delta;
        
        HandleMovement(deltaFloat);

        HandleJump(deltaFloat);

        HandleShoot(deltaFloat);
    }

    public class TargetHold
    {
        public Node3D target;
        public ulong acquisitionTime;
        public ulong releaseTime;

        public TargetHold(Node3D target)
        {
            this.target = target;
            acquisitionTime = Time.GetTicksMsec();
            releaseTime = 0;
        }

        public void ReleaseTarget()
        {
            releaseTime = Time.GetTicksMsec();
        }
    }

    private void HandleShoot(float delta)
    {
        _mostLikelyTarget = null;
        
        List<Target> results = new List<Target>();
        FindTargetsInCone(cameraPivot.GlobalPosition, ForwardDirection, 10f, 10f, results);
        
        float minAngle = float.MaxValue;
        foreach (Target target in results)
        {
            bool isTargetHold1 = _targetHolds[0] != null && _targetHolds[0].target == target;
            bool isTargetHold2 = _targetHolds[1] != null && _targetHolds[1].target == target;
            if (isTargetHold1 || isTargetHold2)
                continue;
            
            Vector3 targetDelta = target.GlobalPosition - cameraPivot.GlobalPosition;
            float angle = targetDelta.AngleTo(ForwardDirection);
            if (angle > minAngle)
                continue;
            
            // lineOfSightRayCast.TargetPosition = target.GlobalPosition + targetDelta.Normalized();
            lineOfSightRayCast.LookAt(target.GlobalPosition);
            lineOfSightRayCast.ForceRaycastUpdate();
            
            if (lineOfSightRayCast.IsColliding())
                if (lineOfSightRayCast.GetCollider() != target.collisionObject3D)
                    continue;

            minAngle = angle;
            _mostLikelyTarget = target;
        }
        
        if (_mostLikelyTarget != null)
            DebugDraw.Sphere(_mostLikelyTarget.GlobalPosition, 1f, Colors.WhiteSmoke);
        
        HandleShootInput(SHOOT1, 0);
        HandleShootInput(SHOOT2, 1);
    }
    
    private void HandleShootInput(string shootInput, int index)
    {
        TargetHold currentTargetHold = _targetHolds[index];

        if (currentTargetHold == null)
        {
            if (Input.IsActionJustPressed(shootInput))
            {
                int otherIndex = index == 0 ? 1 : 0;
                
                if (_mostLikelyTarget != null)
                    _targetHolds[index] = new TargetHold(_mostLikelyTarget);
            }
        }
        else
        {
            DebugDraw.Sphere(currentTargetHold.target.GlobalPosition, 1.1f, index == 0 ? Colors.Blue : Colors.Red);

            if (Input.IsActionJustReleased(shootInput))
            {
                ReleaseTarget(currentTargetHold);
                
                _targetHolds[index] = null;
            }
        }
    }

    private void ReleaseTarget(TargetHold targetHold)
    {
        targetHold.ReleaseTarget();
        
        if (_latestReleasedTargetHold == null || Time.GetTicksMsec() - _latestReleasedTargetHold.releaseTime > 1000f)
            _latestReleasedTargetHold = targetHold;
        else if (_latestReleasedTargetHold.target != targetHold.target)
        {
            // Launch
            GD.Print("SLINGSHIT");

            Vector3 impulseDirection = (_latestReleasedTargetHold.target.GlobalPosition - targetHold.target.GlobalPosition).Normalized();

            if (_latestReleasedTargetHold.target is Target latestReleasedTarget && latestReleasedTarget.Rigidbody != null)
                latestReleasedTarget.Rigidbody.ApplyImpulse(-impulseDirection * 35f);

            if (targetHold.target is Target target && target.Rigidbody != null)
                target.Rigidbody.ApplyImpulse(impulseDirection * 35f);
            
            _latestReleasedTargetHold = null;
        }
    }

    private void HandleJump(float delta)
    {
        if (!groundCheck.IsGrounded)
            return;
        
        _isJumping = false;
        
        if (Input.IsActionJustPressed(JUMP))
        {
            LinearVelocity += Transform.Basis.Y * 7;
            _isJumping = true;
        }
    }

    private void HandleMovement(float delta)
    {
        _desiredVelocity = GetDesiredVelocity();
        if (_desiredVelocity.LengthSquared() == 0f)
        {
            PhysicsMaterialOverride.Friction = 1f;
            
            return;
        }

        PhysicsMaterialOverride.Friction = 0.5f;
        
        _acceleration = groundCheck.IsGrounded ? groundAcceleration : airAcceleration;

        Vector3 verticalVelocity = LinearVelocity.ExtractDotVector(Transform.Basis.Y);
        Vector3 horizontalVelocity = LinearVelocity - verticalVelocity;
        
        Vector3 velocityDelta = _desiredVelocity - horizontalVelocity;

        Vector3 newVelocity = horizontalVelocity.MoveToward(_desiredVelocity, _acceleration * delta);

        LinearVelocity = newVelocity + verticalVelocity;
        
        // DebugDraw.Arrow(GlobalPosition, LinearVelocity, LinearVelocity.Length(), Colors.Red);
        // DebugDraw.Arrow(GlobalPosition, _desiredVelocity, _desiredVelocity.Length(), Colors.Blue);
    }

    private void SetVelocity(Vector3 desiredVelocity)
    {
        Vector3 currentVelocity = LinearVelocity;
        Vector3 acceleration = desiredVelocity - currentVelocity;

        LinearVelocity = currentVelocity + acceleration;
    }

    private Vector3 GetNormalizedHorizontalDirection()
    {
        Vector3 direction = Vector3.Zero;
        
        Vector3 projectedForward = _horizontalPlane.Project(ForwardDirection);

        if (Input.IsActionPressed(DOWN))
            direction -= projectedForward;
        if (Input.IsActionPressed(UP))
            direction += projectedForward;
        if (Input.IsActionPressed(LEFT))
            direction -= cameraPivot.Transform.Basis.X;
        if (Input.IsActionPressed(RIGHT))
            direction += cameraPivot.Transform.Basis.X;

        direction = direction.Normalized();
        
        return direction;
    }

    private Vector3 GetDesiredVelocity()
    {
        Vector3 normalizedDirection = GetNormalizedHorizontalDirection();

        Vector3 desiredVelocity = normalizedDirection * speed;

        return desiredVelocity;
    }

    private void FindTargetsInCone(Vector3 origin, Vector3 direction, float distance, float maxAngle, List<Target> results)
    {
        foreach (Target target in TargetManager.Instance.Targets)
        {
            Vector3 delta = target.GlobalPosition - origin;
            float angle = Mathf.RadToDeg(direction.AngleTo(delta));

            if (angle > maxAngle)
                continue;
            
            results.Add(target);
        }
    } 
}