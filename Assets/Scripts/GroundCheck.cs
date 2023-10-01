using Godot;
using System;

public partial class GroundCheck : ShapeCast3D
{
    private bool _isGrounded;
    public bool IsGrounded
    {
        get => _isGrounded;
        set
        {
            bool wasGrounded = _isGrounded;

            _isGrounded = value;
            
            if (!wasGrounded && IsGrounded)
                GroundContactGained?.Invoke();
            else if (wasGrounded && !IsGrounded)
                GroundContactLost?.Invoke();
        }
    }
    
    public Vector3 GroundNormal { get; private set; }
    
    public delegate void OnGroundContactLost();
    public event OnGroundContactLost GroundContactLost;
    
    public delegate void OnGroundContactGained();
    public event OnGroundContactGained GroundContactGained;
    
    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        IsGrounded = IsColliding();
        
        if (IsGrounded)
            GroundNormal = GetCollisionNormal(0);
    }
}
