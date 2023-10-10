using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MEC;

[GlobalClass]
public partial class Agent : RigidBody3D
{
    [Export] public float speed = 3f;
    [Export] public float acceleration = 1f;
    [Export] public float maxStabilizedVelocityMagnitude = 0.5f;
    [Export] public float stabilizationDuration = 2f;
    [Export] public float frictionOnLaunch = 0.9f;
    [Export] public float defaultFriction = 0f;

    [Export] public NavigationAgent3D navAgent;
    [Export] public AgentAnimationTree animationTree;
    [Export] public Node3D visualsRoot;

    private bool _suspendNavigation;
    private float _stabilizeTimer;
    private bool _isStabilizing;

    public override void _Ready()
    {
        base._Ready();

        navAgent.TargetReached += NavAgentOnTargetReached;
        Timing.RunCoroutine(Wait());
    }

    private IEnumerator<double> Wait()
    {
        yield return Timing.WaitForSeconds(1f);
        
        SetTarget(GetRandomTargetOnNavMesh());
    }

    private void NavAgentOnTargetReached()
    {
        SetTarget(GetRandomTargetOnNavMesh());
    }
    
    public void SetTarget(Vector3 targetPosition)
    {
        navAgent.TargetPosition = targetPosition;
    }

    private Vector3 GetRandomTargetOnNavMesh()
    {
       return LevelManager.Instance.GetRandomPointOnNavMesh();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        
        DebugDraw3D.DrawSphere(navAgent.TargetPosition);
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        
        if (_suspendNavigation)
        {
            if (_isStabilizing)
            {
                Stabilize(delta);
            }
            if (LinearVelocity.Length() <= maxStabilizedVelocityMagnitude && !_isStabilizing)
            {
                StartStabilizing();
            }
        }
        
        ProcessMovement(delta);
    }

    private Quaternion _originalStabilizingRotation;
    
    private void StartStabilizing()
    {
        _isStabilizing = true;
        _originalStabilizingRotation = Basis.GetRotationQuaternion();
        ToggleAngularLock(true);
    }

    private void ProcessMovement(double delta)
    {
        if (_suspendNavigation)
            return;
        
        Vector3 nextPosition = navAgent.GetNextPathPosition();
        Vector3 deltaToNextPosition = nextPosition - GlobalPosition;
        Vector3 desiredVelocity = deltaToNextPosition.Normalized() * speed;

        Vector3 velocity = LinearVelocity.MoveToward(desiredVelocity, (float) delta * acceleration);

        LinearVelocity = velocity;

        if (LinearVelocity.LengthSquared() > 0f)
            visualsRoot.Quaternion = visualsRoot.Transform.LookingAt(-LinearVelocity, Vector3.Up).Basis.GetRotationQuaternion();
    }

    private void Stabilize(double delta)
    {
        _stabilizeTimer += (float)delta;
        
        if (_stabilizeTimer <= stabilizationDuration)
        {
            float t = _stabilizeTimer / stabilizationDuration;
            
            // lerp position so feet are on ground ?
            
            // slerp rotation so agent is standing
            Quaternion = _originalStabilizingRotation.Slerp(Quaternion.Identity, Easing.Cubic.InOut(t));
        }
        else
        {
            FinishStabilization();
        }
    }

    private void FinishStabilization()
    {
        _stabilizeTimer = 0f;
        _suspendNavigation = false;
        PhysicsMaterialOverride.Friction = defaultFriction;
        ToggleAngularLock(true);
        _isStabilizing = false;
        Quaternion = Quaternion.Identity;
        animationTree.IsWalking = true;
    }
    
    public void OnLaunch()
    {
        _stabilizeTimer = 0f;
        _suspendNavigation = true;
        PhysicsMaterialOverride.Friction = frictionOnLaunch;
        ToggleAngularLock(false);
        _isStabilizing = false;
        animationTree.IsWalking = false;
    }

    public void ToggleAngularLock(bool value)
    {
        AxisLockAngularX = value;
        AxisLockAngularZ = value;
        AxisLockAngularY = value;
    }
}