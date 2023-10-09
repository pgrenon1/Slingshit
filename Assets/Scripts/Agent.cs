using Godot;
using System;
using System.Collections.Generic;
using MEC;

[GlobalClass]
public partial class Agent : RigidBody3D
{
    [Export] public float speed = 3f;
    [Export] public float acceleration = 1f;

    [Export] public NavigationAgent3D navAgent;
    public bool SuspendNavigation { get; set; }

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

        if (SuspendNavigation)
            return;
        
        Vector3 nextPosition = navAgent.GetNextPathPosition();
        Vector3 deltaToNextPosition = nextPosition - GlobalPosition;
        Vector3 desiredVelocity = deltaToNextPosition.Normalized() * speed;

        Vector3 velocity = LinearVelocity.MoveToward(desiredVelocity, (float)delta * acceleration);

        LinearVelocity = velocity;
    }

    public void SetTarget(Vector3 targetPosition)
    {
        navAgent.TargetPosition = targetPosition;
    }
}