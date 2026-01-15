using Godot;
using System;
using System.Collections.Generic;
using System.Drawing;

public partial class Terrain : Node3D {
   [Signal]
   public delegate void terrainGenerationFinishedEventHandler(int size);
    [Export] FastNoiseLite noise;
    [Export(PropertyHint.Range, "0,50,0.5")]double amplitude = 20;
    [Export(PropertyHint.Range, "0,10,0.5")] float fidelity = 1;
    [Export(PropertyHint.Range, "10,1000,10")] int size = 50;
    [Export] bool randomizeSeed = false;
    private Vector3[] vertArr;
    private List<int> indicieList;
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

        //makes it so higher fidelity = higher resolution landscape
        fidelity = 1/fidelity;

        //scales world with fidelity
        int worldSize = Mathf.FloorToInt(size / fidelity) + 1;

        vertArr = new Vector3[worldSize*worldSize];
        indicieList = new();

        if(randomizeSeed) {noise.Seed = (int)GD.Randi();}


        int totalIndex = 0;
        for(int x = 0; x < worldSize; x++) {
            for(int z = 0; z < worldSize; z++) {
                //scales x and z with fidelity
                float sizedX = x*fidelity;
                float sizedZ = z*fidelity;

                float noisePos = noise.GetNoise2D(sizedX,sizedZ);

                //1D Vector3 array of verticies
                vertArr[totalIndex] = new Vector3(sizedX,(float)(noisePos*amplitude),sizedZ);
                totalIndex++;


                //creat balls for debugging
                /*
                MeshInstance3D ball = new MeshInstance3D();
                ball.Mesh = new SphereMesh() {Radius = 0.3f};
                AddChild(ball);
                ball.Position = new Vector3(x,(float)(noisePos*amplitude),z);
                */
            }
        }
        //list of all indicies
        for(int x = 0; x < worldSize-1; x++) {
            for(int z = 0; z < worldSize-1; z++) {
                
                int a = x*worldSize+z;
                int b = (x+1)*worldSize+z;
                int c = x*worldSize+(z+1);
                int d = (x+1)*worldSize+(z+1);

                //counter clockwise
                indicieList.Add(a); indicieList.Add(b); indicieList.Add(d); //triangle 1
                indicieList.Add(a); indicieList.Add(d); indicieList.Add(c); //triangle 2

            }
        }
        buildMesh(vertArr,indicieList);
        
        EmitSignal(SignalName.terrainGenerationFinished,worldSize);
       
    }
    public void buildMesh(Vector3[] vertices, List<int> indicies) {
        int[] indexArray = indicies.ToArray();
        var arrays = new Godot.Collections.Array();

        arrays.Resize((int)Mesh.ArrayType.Max);

        arrays[(int)Mesh.ArrayType.Vertex] = vertices;
        arrays[(int)Mesh.ArrayType.Index] = indexArray;

        var mesh = new ArrayMesh();

        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles,arrays);

        var meshInstance = new MeshInstance3D();
        meshInstance.Mesh = mesh;
        AddChild(meshInstance);



    }

}
