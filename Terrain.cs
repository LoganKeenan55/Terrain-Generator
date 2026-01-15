using Godot;
using System;
using System.Collections.Generic;
using System.Drawing;

public partial class Terrain : Node3D {
    
    [Export] FastNoiseLite noise;
    [Export] double amplitude = 10;
    [Export] int fidelity = 1;
    [Export] int size = 70;
    [Export] bool randomizeSeed = false;
    private double time = 1;
    private double[,] pointArr;
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
        pointArr = new double[size,size];
        vertArr = new Vector3[size*size];
        indicieList = new();

        if(randomizeSeed) {noise.Seed = (int)GD.Randi();}

        int totalIndex = 0;
        for(int x = 0; x < size; x++) {
            for(int z = 0; z < size; z++) {
                
                //2D double array of all points, value is height
                float noisePos = noise.GetNoise2D(x,z);
                pointArr[x,z] = (double)(noisePos*amplitude);

                //1D Vector3 array of verticies
                vertArr[totalIndex] = new Vector3(x,(float)(noisePos*amplitude),z);
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
        for(int x = 0; x < size-1; x++) {
            for(int z = 0; z < size-1; z++) {
                int a = x*size+z;
                int b = (x+1)*size+z;
                int c = x*size+(z+1);
                int d = (x+1)*size+(z+1);

                indicieList.Add(a); indicieList.Add(b); indicieList.Add(d); // Triangle 1
                indicieList.Add(a); indicieList.Add(d); indicieList.Add(c); // Triangle 2

            }
        }
        buildMesh(vertArr,indicieList);
       
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
