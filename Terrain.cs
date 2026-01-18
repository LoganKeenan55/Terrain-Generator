using Godot;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using Vector3 = Godot.Vector3;
public partial class Terrain : Node3D {
   [Signal]
   public delegate void terrainGenerationFinishedEventHandler(int size);

    [Export(PropertyHint.Range, "0,50,0.1")]double detailAmplitude = 20;
    [Export(PropertyHint.Range, "0,200,0.1")]double terrainAmplitude = 100;
    [Export(PropertyHint.Range, "0,5,0.1")] float fidelity = 1;
    [Export(PropertyHint.Range, "10,2000,10")] int size = 50;
    [Export] bool randomizeSeed = true;

    //noise for small details in terrain
    [Export] FastNoiseLite detailNoise;
    //noise for large details in terrain 
    [Export] FastNoiseLite largeTerrainNoise;

    private Vector3[] vertArr;
    private List<int> indicieList;
    private Godot.Color[] colorArr;
    private List<float> steepnessList;
    public override void _Ready()
	{

        buildTerrain(randomizeSeed);

    }

    public void buildTerrain(bool randomizeSeed) {
        //scales world with fidelity
        fidelity = 1/fidelity;
        int worldSize = Mathf.FloorToInt(size / fidelity) + 1;

        if(randomizeSeed) {generateSeed();}

        initializeData(worldSize);

        createVerticeArray(worldSize);
        
        createIndicieList(worldSize);

        createSteepnessList(worldSize);

        applyVertexColors(worldSize);
        
        buildMesh(vertArr,indicieList);

        EmitSignal(SignalName.terrainGenerationFinished,size);
       
    }

    public void createSteepnessList(int worldSize) {
        for(int i = 0; i < indicieList.Count; i += 3) {
            //Vectors A B C
            //AB = B-A, AC = C-A
            //AB x AC = normal vector

            int Aindicie = indicieList[i];
            int Bindicie = indicieList[i+1];
            int Cindicie = indicieList[i+2];

            Vector3 A = vertArr[Aindicie];
            Vector3 B = vertArr[Bindicie];
            Vector3 C = vertArr[Cindicie];

            Vector3 AB = B-A;
            Vector3 AC = C-A;


            Vector3 crossProduct = new Vector3((AB.Y * AC.Z) - (AB.Z * AC.Y), - ((AB.X * AC.Z) - (AB.Z * AC.X)),(AB.X*AC.Y) - (AB.Y * AC.X));

            //divide by magnitude so it just direction
            Vector3 normal = crossProduct.Normalized();
            
            float steepness = normal.Dot(new Vector3(0f,0f,1f));

            steepnessList.Add(steepness);
            //1 = flat
            //0 = vertical
        }
    }




    public void initializeData(int worldSize) {
        //makes it so higher fidelity = higher resolution landscape
        

        colorArr = new Godot.Color[worldSize * worldSize];
        vertArr = new Vector3[worldSize*worldSize];
        indicieList = new();
        steepnessList = new();
    }
    public void generateSeed() {
        detailNoise.Seed = (int)GD.Randi();
        largeTerrainNoise.Seed = (int)GD.Randi();
    }

    public void createVerticeArray(int worldSize) {
        int totalIndex = 0;
        for(int x = 0; x < worldSize; x++) {
            for(int z = 0; z < worldSize; z++) {
                //scales x and z with fidelity
                float sizedX = x*fidelity;
                float sizedZ = z*fidelity;

                float detailNoisePos = detailNoise.GetNoise2D(sizedX,sizedZ);
                float largeNoisePos = largeTerrainNoise.GetNoise2D(sizedX,sizedZ);
                
                //add small / large noise to Y values
                float heightY = 0;
                heightY+= (float)(detailNoisePos*detailAmplitude);
                heightY+= (float)(largeNoisePos*terrainAmplitude);
                vertArr[totalIndex] = new Vector3(sizedX,heightY,sizedZ);

                //add color
                //colorArr[totalIndex] = getTerrainColor(heightY);

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
    }

public void applyVertexColors(int worldSize)
{
    int triangle = 0;
    for (int i = 0; i < indicieList.Count; i += 3)
    {
        int A = indicieList[i];
        int B = indicieList[i + 1];
        int C = indicieList[i + 2];

        float steepness = steepnessList[triangle++];

        float height = (vertArr[A].Y + vertArr[B].Y + vertArr[C].Y) / 3f;


        Godot.Color faceColor = getColorFromHeightAndSteepness(height, steepness);

        colorArr[A] = faceColor;
        colorArr[B] = faceColor;
        colorArr[C] = faceColor;
    }
}

    public Godot.Color getColorFromHeightAndSteepness(float height, float steepness) {
        Godot.Color grass = new Godot.Color(.01f,.6f,.05f);
        Godot.Color stone = new Godot.Color(.5f,.5f,.5f);
        Godot.Color snow = new Godot.Color(1f,1f,1f);

        

        
        /*
        if(heightClamped <= -.2) {
            return grass - new Godot.Color(0f,GD.Randf()/4,0f);
        }
        else if(heightClamped >= -.2 && heightClamped <= .4) {
            float subtractedColor = GD.Randf()/10;
            return stone - new Godot.Color(subtractedColor,subtractedColor,subtractedColor);
        }
        else if (heightClamped >= .4) {
            return snow;
        }
        */
        if(steepness <= .5){
            return grass;
        }
        return new Godot.Color(1f,1f,1f);

       
       
    }
    public void createIndicieList(int worldSize) {
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


}
