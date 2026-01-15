using Godot;
using System;

public partial class Camera3d : Camera3D
{
    [Export] public float Sensitivity = 0.2f;
    [Export] public float MoveSpeed = 10f;

    private float yaw = 0f;
    private float pitch = 0f;
    private Terrain terrain;
    public override void _Ready()
    {   
        terrain = GetNode<Terrain>("../Terrain");
        terrain.terrainGenerationFinished += moveStartingPosition;
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _Process(double delta)
    {
        HandleMovement((float)delta);

        if (Input.IsActionJustPressed("`"))
            GetTree().Quit();
    }

    private void HandleMovement(float delta)
    {
        Vector3 direction = Vector3.Zero;

        if (Input.IsActionPressed("w"))
            direction -= Transform.Basis.Z;
        if (Input.IsActionPressed("s"))
            direction += Transform.Basis.Z;

        if (Input.IsActionPressed("a"))
            direction -= Transform.Basis.X;
        if (Input.IsActionPressed("d"))
            direction += Transform.Basis.X;

        if (Input.IsActionPressed("space"))
            direction += Transform.Basis.Y;
        if (Input.IsActionPressed("ctrl"))
            direction -= Transform.Basis.Y;

        if (direction != Vector3.Zero)
            Position += direction.Normalized() * MoveSpeed * delta;

        if (Input.IsActionJustPressed("shift")) {
            MoveSpeed = 40f;
        }
        if (Input.IsActionJustReleased("shift")) {
            MoveSpeed = 10f;
        }
    }

    public void moveStartingPosition(int mapSize) {
        Position = new Vector3(mapSize/2,20,mapSize/2);
    }
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotion)
        {
            yaw -= mouseMotion.Relative.X * Sensitivity;
            pitch -= mouseMotion.Relative.Y * Sensitivity;

            pitch = Mathf.Clamp(pitch, -90f, 90f);

            RotationDegrees = new Vector3(pitch, yaw, 0f);
        }
    }
}
