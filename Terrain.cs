using Godot;
using System;
using System.Collections.Generic;
using System.Drawing;

public partial class Terrain : Node3D {
   [Signal]
   public delegate void terrainGenerationFinishedEventHandler(int size);

    [Export(PropertyHint.Range, "0,50,0.5")]double amplitude = 20;
    [Export(PropertyHint.Range, "0,10,0.5")] float fidelity = 1;
    [Export(PropertyHint.Range, "10,2000,10")] int size = 50;
    [Export] bool randomizeSeed = false;

    //noise for small details in terrain
    [Export] FastNoiseLite smallNoise;
    //noise for large details in terrain 
    [Export] FastNoiseLite largeNoise;

    private Vector3[] vertArr;
    private List<int> indicieList;
    private Godot.Color[] colorArr;
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

        colorArr = new Godot.Color[worldSize * worldSize];
        vertArr = new Vector3[worldSize*worldSize];
        indicieList = new();

        if(randomizeSeed) {smallNoise.Seed = (int)GD.Randi();}


        int totalIndex = 0;
        for(int x = 0; x < worldSize; x++) {
            for(int z = 0; z < worldSize; z++) {
                //scales x and z with fidelity
                float sizedX = x*fidelity;
                float sizedZ = z*fidelity;

                float smallNoisePos = smallNoise.GetNoise2D(sizedX,sizedZ);
                float largeNoisePos = largeNoise.GetNoise2D(sizedX,sizedZ);
                
                //add small / large noise to Y values
                float heightY = 0;
                heightY+= (float)(smallNoisePos*amplitude);
                heightY+= (float)(largeNoisePos*100);
                vertArr[totalIndex] = new Vector3(sizedX,heightY,sizedZ);

                //add color
                colorArr[totalIndex] = getTerrainColor(heightY);

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
        arrays[(int)Mesh.ArrayType.Color] = colorArr;
        var mesh = new ArrayMesh();

        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles,arrays);

        var meshInstance = new MeshInstance3D();
        meshInstance.Mesh = mesh;

        var material = new StandardMaterial3D();
        material.VertexColorUseAsAlbedo = true; 
        meshInstance.MaterialOverride = material;
        AddChild(meshInstance);



    }

    public Godot.Color getTerrainColor(float height) {
        Godot.Color grass = new Godot.Color(0,.6f,.1f);
        Godot.Color stone = new Godot.Color(.5f,.5f,.5f);
        Godot.Color snow = new Godot.Color(1f,1f,1f);

        float heightClamped = Math.Clamp(height/100f,0,1);
        GD.Print(heightClamped);

        if(heightClamped <= .005) {
            return grass;
        }
        else if(heightClamped >= .005 && heightClamped <= .25) {
            return stone;
        }
        else if (heightClamped >= .25) {
            return snow;
        }

        return grass.Lerp(stone,heightClamped);
       
    }
}
