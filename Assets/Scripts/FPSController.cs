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
    
    private Vector3 velocity;
    private float y_velocity;
    private Vector3 _forward;
    private bool _isJumping;
    private Vector3 _momentum = Vector3.Zero;
    private Plane _horizontalPlane = new(Vector3.Up);
        
    private const string UP = "up";
    private const string DOWN = "down";
    private const string RIGHT = "right";
    private const string LEFT = "left";
    
    public float MovementSpeedSquared { get; private set; }

    public override void _Ready()
    {
        // cameraPivot = GetNode<Node3D>("CameraPivot");
        // camera = GetNode<Camera3D>("CameraPivot/CameraBoom/Camera");

        Input.MouseMode = Input.MouseModeEnum.Captured;
        groundCheck.GroundContactLost += OnGroundContactLost;
        groundCheck.GroundContactGained += OnGroundContactGained;
    }

    private void OnGroundContactGained()
    {
        
    }

    private void OnGroundContactLost()
    {
        // Vector3 desiredVelocity = GetDesiredVelocity();
        // float desiredVelocityMagnitude = desiredVelocity.LengthSquared();
        //
        // //Check if the controller has both momentum and a current movement velocity;
        // if(desiredVelocityMagnitude >= 0f && _momentum.LengthSquared() > 0f)
        // {
        //     Vector3 normalizedDesiredVelocity = desiredVelocity.Normalized();
        //     
        //     //Project momentum onto movement direction;
        //     Vector3 projectedMomentum = _momentum.Project(normalizedDesiredVelocity);
        //     //Calculate dot product to determine whether momentum and movement are aligned;
        //     float dot = projectedMomentum.Normalized().Dot(normalizedDesiredVelocity);
        //
        //     //If current momentum is already pointing in the same direction as movement velocity,
        //     //Don't add further momentum (or limit movement velocity) to prevent unwanted speed accumulation;
        //     if(projectedMomentum.LengthSquared() >= desiredVelocityMagnitude && dot > 0f)
        //         desiredVelocity = Vector3.Zero;
        //     else if(dot > 0f)
        //         desiredVelocity -= projectedMomentum;
        // }
        //
        // //Add movement velocity to momentum;
        // _momentum += desiredVelocity;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        MovementSpeedSquared = speed * speed;
        
        if (Input.IsActionJustPressed("ui_cancel"))
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
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
    }

    private void HandleJump(float delta)
    {
        if (!groundCheck.IsColliding())
            return;
        
        _isJumping = false;
        
        if (Input.IsActionJustPressed("jump"))
        {
            LinearVelocity += Transform.Basis.Y * 7;
            _isJumping = true;
        }
    }
    
    private float _acceleration;
    private Vector3 _desiredVelocity;
    private Vector3 _projectedMoveInput;

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

        Vector3 verticalVelocity = ExtractDotVector(LinearVelocity, Transform.Basis.Y);
        Vector3 horizontalVelocity = LinearVelocity - verticalVelocity;
        
        Vector3 velocityDelta = _desiredVelocity - horizontalVelocity;

        Vector3 newVelocity = IncrementVectorTowardTargetVector(horizontalVelocity, _acceleration, delta, _desiredVelocity);

        LinearVelocity = newVelocity + verticalVelocity;
        
        DebugDraw.Arrow(GlobalPosition, LinearVelocity, LinearVelocity.Length(), Colors.Red);
        DebugDraw.Arrow(GlobalPosition, _desiredVelocity, _desiredVelocity.Length(), Colors.Blue);
    }

    private void SetVelocity(Vector3 desiredVelocity)
    {
        Vector3 currentVelocity = LinearVelocity;
        Vector3 acceleration = desiredVelocity - currentVelocity;

        LinearVelocity = currentVelocity + acceleration;
    }

    // private void HandleMomentum(float delta)
    // {
    //     Vector3 verticalMomentum = Vector3.Zero;
    //     Vector3 horizontalMomentum = Vector3.Zero;
    //
    //     //Split momentum into vertical and horizontal components
    //     if(_momentum != Vector3.Zero)
    //     {
    //         verticalMomentum = ExtractDotVector(_momentum, Transform.Basis.Y);
    //         horizontalMomentum = _momentum - verticalMomentum;
    //     }
    //     
    //     //Add gravity to vertical momentum;
    //     verticalMomentum -= Transform.Basis.Y * gravity * delta;
    //
    //     //Remove any downward force if the controller is grounded
    //     if (groundCheck.IsGrounded && GetDotProduct(verticalMomentum, Transform.Basis.Y) < 0f)
    //         verticalMomentum = Vector3.Zero;
    //
    //     //Manipulate momentum to steer controller in the air
    //     if (!groundCheck.IsGrounded)
    //     {
    //         Vector3 movementVelocity = GetDesiredVelocity();
    //         
    //         //If controller has received additional momentum from somewhere else
    //         if (horizontalMomentum.LengthSquared() > MovementSpeedSquared)
    //         {
    //             //Prevent unwanted accumulation of speed in the direction of the current momentum
    //             if(GetDotProduct(movementVelocity, horizontalMomentum.Normalized()) > 0f)
    //                 movementVelocity = RemoveDotVector(movementVelocity, horizontalMomentum.Normalized());
    //             
    //             //Lower air control slightly with a multiplier to add some 'weight' to any momentum applied to the controller (??????)
    //             float airControlMultiplier = 0.25f;
    //             horizontalMomentum += movementVelocity * delta * airControlRate * airControlMultiplier;
    //         }
    //         //If controller has not received additional momentum
    //         else
    //         {
    //             //Clamp horizontal velocity to prevent accumulation of speed;
    //             horizontalMomentum += movementVelocity * delta * airControlRate;
    //             horizontalMomentum.ClampMagnitude(speed);
    //         }
    //     }
    //     
    //     //Apply friction to horizontal momentum based on whether the controller is grounded;
    //     if(groundCheck.IsGrounded)
    //         horizontalMomentum = horizontalMomentum.MoveToward(Vector3.Zero, delta * groundFriction);
    //     else
    //         horizontalMomentum = horizontalMomentum.MoveToward(Vector3.Zero, delta * airFriction);
    //     
    //     //Add horizontal and vertical momentum back together;
    //     _momentum = horizontalMomentum + verticalMomentum;
    // }

    private Vector3 GetNormalizedHorizontalDirection()
    {
        Vector3 direction = Vector3.Zero;

        Vector3 projectedForward = _horizontalPlane.Project(cameraPivot.Transform.Basis.Z);

        if (Input.IsActionPressed(UP))
            direction -= projectedForward;
        if (Input.IsActionPressed(DOWN))
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
    
    //Extract and return parts from a vector that are pointing in the same direction as 'direction';
    public static Vector3 ExtractDotVector(Vector3 vector, Vector3 direction)
    {
        //Normalize vector if necessary;
        if(direction.LengthSquared() != 1)
            direction.Normalized();
			    
        float dot = vector.Dot(direction);
			    
        return direction * dot;
    }
    
    //Returns the length of the part of a vector that points in the same direction as '_direction' (i.e., the dot product);
    public static float GetDotProduct(Vector3 vector, Vector3 direction)
    {
        //Normalize vector if necessary;
        if(direction.LengthSquared() != 1)
            direction.Normalized();
				
        float length = vector.Dot(direction);

        return length;
    }
    
    //Remove all parts from a vector that are pointing in the same direction as '_direction';
    public static Vector3 RemoveDotVector(Vector3 vector, Vector3 direction)
    {
        //Normalize vector if necessary;
        if(direction.LengthSquared() != 1)
            direction.Normalized();
			
        float amount = vector.Dot(direction);
			
        vector -= direction * amount;
			
        return vector;
    }
    
    //Increments a vector toward a target vector, using '_speed' and '_deltaTime';
    public static Vector3 IncrementVectorTowardTargetVector(Vector3 currentVector, float speed, float deltaTime, Vector3 targetVector)
    {
        return currentVector.MoveToward(targetVector, speed * deltaTime);
    }
}

public static class Extensions
{
    public static void ClampMagnitude(this Vector3 vector, float maxLength)
    {
        float sqrMagnitude = vector.LengthSquared();
        
        if ((double) sqrMagnitude <= (double) maxLength * (double) maxLength)
            return;
        
        float num1 = (float) Mathf.Sqrt((double) sqrMagnitude);
        float num2 = vector.X / num1;
        float num3 = vector.Y / num1;
        float num4 = vector.Z / num1;
        
        vector = new Vector3(num2 * maxLength, num3 * maxLength, num4 * maxLength);
    }
}

