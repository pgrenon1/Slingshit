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

    private bool _suspendNavigation;
    private double _stabilizeTimer;
    
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
            if (LinearVelocity.Length() <= maxStabilizedVelocityMagnitude)
            {
                TryStabilize(delta);
            }
            else
                return;
        }
        
        Vector3 nextPosition = navAgent.GetNextPathPosition();
        Vector3 deltaToNextPosition = nextPosition - GlobalPosition;
        Vector3 desiredVelocity = deltaToNextPosition.Normalized() * speed;

        Vector3 velocity = LinearVelocity.MoveToward(desiredVelocity, (float)delta * acceleration);

        LinearVelocity = velocity;
    }
    
    private void TryStabilize(double delta)
    {
        _stabilizeTimer += delta;

        if (_stabilizeTimer >= stabilizationDuration)
        {
            OnStabilized();
        }
    }

    private void OnStabilized()
    {
        _suspendNavigation = false;
        PhysicsMaterialOverride.Friction = defaultFriction;
    }
    
    public void OnLaunch()
    {
        _suspendNavigation = true;
        PhysicsMaterialOverride.Friction = frictionOnLaunch;
        _stabilizeTimer = 0f;
    }
}