using System.Collections.Generic;
using Godot;
using ImGuiNET;

[GlobalClass]
public partial class FPSController : RigidBody3D
{
    public class TargetHold
    {
        public Node3D target;
        public ulong releaseTime;

        public TargetHold(Node3D target)
        {
            this.target = target;
            releaseTime = 0;
        }

        public void ReleaseTarget()
        {
            releaseTime = Time.GetTicksMsec();
        }
    }
    
    [Export] float speed = 20f;
    [Export] private float jumpSpeed = 7f;

    [Export(PropertyHint.Range, "0.1,1.0")]
    float mouse_sensitivity = 0.3f;
    [Export(PropertyHint.Range, "-90,0,1")]
    float min_pitch = -90f;
    [Export(PropertyHint.Range, "0,90,1")]
    float max_pitch = 90f;

    [Export] public float groundAcceleration = 10f;
    [Export] public float airAcceleration = 10f;
    [Export] public float jumpInputStickyTime = 0.12f;
    [Export] public float coyoteTime = 0.2f;
    
    [Export] float slingShitForceOnRigidbody = 35f;
    [Export] float slingShitForceOnSelf = 35f;
    [Export] float slingShitTimeAllowedBetweenReleases = 0.2f;
    
    [Export] public Node3D cameraPivot;
    [Export] public Camera3D camera;
    [Export] public GroundCheck groundCheck;
    [Export] public RayCast3D lineOfSightRayCast;
    
    private Plane _horizontalPlane = new (Vector3.Up);
    private float _acceleration = 0f;
    private Vector3 _desiredVelocity = Vector3.Zero;
    private Target _mostLikelyTarget = null;
    private TargetHold[] _targetHolds = new TargetHold[2];
    private TargetHold _firstReleasedTargetHold = null;

    private bool _isHoldingJumpInput = false;
    private ulong _lastRequestedJumpTime = ulong.MinValue;
    private ulong _lastAvailableJumpTime = ulong.MinValue;
    
    public float MovementSpeedSquared { get; private set; }
    public Vector3 ForwardDirection => -cameraPivot.Transform.Basis.Z;

    private bool CanJump => Time.GetTicksMsec() - _lastAvailableJumpTime <= coyoteTime * 1000f;
    public bool IsGrounded { get; private set; }
    
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
    
    public override void _Process(double delta)
    {
        MovementSpeedSquared = speed * speed;
        
        if (Input.IsActionJustPressed(UI_CANCEL))
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
        
        if (Input.IsActionJustPressed(SHOOT1) && !Input.IsActionPressed(UI_CANCEL))
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
        }

        ImGui.Begin("Debug");
        {
            for (int i = 0; i < _targetHolds.Length; i++)
            {
                string text = _targetHolds[i] == null ? "null" : _targetHolds[i].target.ToString();
                ImGui.Text($"Hold {i}: {text}");
            }
            
            for (int i = 0; i < _releasedTargetHolds.Length; i++)
            {
                string text = _releasedTargetHolds[i] == null ? "null" : _releasedTargetHolds[i].target.ToString();
                ImGui.Text($"Released {i}: {text}");
            }
        }
        ImGui.End();
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

    public override void _UnhandledInput(InputEvent @event)
    {
        base._UnhandledInput(@event);

        HandleJumpInputs(@event);
    }

    private void HandleJumpInputs(InputEvent @event)
    {
        if (@event.IsActionPressed(JUMP))
        {
            RequestJump();

            _isHoldingJumpInput = true;
        }
        else
        {
            _isHoldingJumpInput = false;
        }
    }

    private void RequestJump()
    {
        _lastRequestedJumpTime = Time.GetTicksMsec();
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        float deltaFloat = (float)delta; // we could pre-alloc this but that could allow us to use it at invalid times

        ProcessGroundDetection();

        ProcessJump();
        
        ProcessMovement(deltaFloat);

        ProcessShootInputs();
    }

    private void ProcessGroundDetection()
    {
        if (groundCheck.Check())
        {
            _lastAvailableJumpTime = Time.GetTicksMsec();

            if (!IsGrounded)
            {
                // just landed
            }

            IsGrounded = true;
        }
        else
        {
            IsGrounded = false;
        }
    }

    private void ProcessShootInputs()
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
            DebugDraw3D.DrawSphere(_mostLikelyTarget.GlobalPosition, 1f, Colors.WhiteSmoke);
        
        HandleShootInput(SHOOT1, 0);
        HandleShootInput(SHOOT2, 1);

        HandleSlingShit();
    }

    private void HandleSlingShit()
    {
        if (_releasedTargetHolds[0] == null || _releasedTargetHolds[1] == null)
            return;

        if (Mathf.Abs((long)_releasedTargetHolds[0].releaseTime - (long)_releasedTargetHolds[1].releaseTime) <= slingShitTimeAllowedBetweenReleases * 1000f)
        {
            GD.Print("SLINGSHIT");

            Vector3 impulseDirection = (_releasedTargetHolds[0].target.GlobalPosition - _releasedTargetHolds[1].target.GlobalPosition).Normalized();

            if (_releasedTargetHolds[0].target is Target latestReleasedTarget && latestReleasedTarget.Rigidbody != null)
                latestReleasedTarget.Launch(-impulseDirection * slingShitForceOnRigidbody);

            if (_releasedTargetHolds[1].target is Target target && target.Rigidbody != null)
                target.Launch(impulseDirection * slingShitForceOnRigidbody);
            
            _releasedTargetHolds[0] = null;
            _releasedTargetHolds[1] = null;
        }
    }

    private TargetHold[] _releasedTargetHolds = new TargetHold[2];

    private void HandleShootInput(string shootInput, int index)
    {
        if (Input.IsActionJustPressed(shootInput))
        {
            if (_mostLikelyTarget != null)
            {
                _targetHolds[index] = new TargetHold(_mostLikelyTarget);
                _releasedTargetHolds[index] = null;
            }
        }
        else if (Input.IsActionJustReleased(shootInput))
        {
            Release(index);
        }

        if (_targetHolds[index] != null)
            DebugDraw3D.DrawSphereHd(_targetHolds[index].target.GlobalPosition, 1.1f, index == 0 ? Colors.Blue : Colors.Red);
    }

    private void Release(int index)
    {
        if (_targetHolds[index] != null)
        {
            _targetHolds[index].ReleaseTarget();
            _releasedTargetHolds[index] = _targetHolds[index];
        }

        _targetHolds[index] = null;
    }

    private void ProcessJump()
    {
        ulong time = Time.GetTicksMsec();
        if (CanJump && time - _lastRequestedJumpTime <= jumpInputStickyTime * 1000f)
        {
            Jump();
        }
    }

    private void Jump()
    {
        Vector3 currentVelocity = LinearVelocity; // prealloc?
        currentVelocity.Y = jumpSpeed;
        LinearVelocity = currentVelocity;

        FinalizeJump();
    }
    
    private void FinalizeJump()
    {
        IsGrounded = false;
        _lastRequestedJumpTime = ulong.MinValue;
        _lastAvailableJumpTime = ulong.MinValue;
    }

    private void ProcessMovement(float delta)
    {
        _desiredVelocity = GetDesiredVelocity();
        if (_desiredVelocity.LengthSquared() == 0f)
        {
            PhysicsMaterialOverride.Friction = 1f;
            
            return;
        }

        PhysicsMaterialOverride.Friction = 0.5f;
        
        _acceleration = IsGrounded ? groundAcceleration : airAcceleration;

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