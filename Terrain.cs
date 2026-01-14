using Godot;
using System;
using System.Drawing;

public partial class Terrain : Node3D {
    
    [Export] FastNoiseLite noise;
    [Export] double amplitude = 10;
    [Export] int fidelity = 10;
    [Export] int size = 70;
    [Export] bool randomizeSeed = false;
    private double time = 1;
    private MeshInstance3D[] pointArr;
    public override void _Ready()
	{

        buildTerrain(randomizeSeed);

        
        
        
    }
    public override void _Process(double delta) {
        //time += .1;
        //float noisePos = noise.GetNoise2D((float)time,(float)0.5);
        //Position = new Vector3(0f,noisePos*10,0f);
        //GD.Print(Position);
    }

    public void buildTerrain(bool randomizeSeed) {
        pointArr = new MeshInstance3D[size];

        if(randomizeSeed) {this.noise.Seed = (int)GD.Randi();}

        for(int x = 0; x < size; x++) {
            for(int z = 0; z < size; z++) {
                MeshInstance3D ball = new MeshInstance3D();
                ball.Mesh = new SphereMesh() {Radius = 0.3f};
                AddChild(ball);
                float noisePos = noise.GetNoise2D(x,z);
                ball.Position = new Vector3(x,(float)(noisePos*amplitude),z);
            }
        }
    }

}
