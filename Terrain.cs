using Godot;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using Vector3 = Godot.Vector3;
public partial class Terrain : Node3D {
   [Signal]
   public delegate void terrainGenerationFinishedEventHandler(int size);

    [Export(PropertyHint.Range, "0,50,0.1")] double detailAmplitude = 20;
    [Export(PropertyHint.Range, "0,200,0.1")] double terrainAmplitude = 100;
    [Export(PropertyHint.Range, "0,5,0.1")] float fidelity = 1;
    [Export(PropertyHint.Range, "10,5000,10")] int size = 1000;
    [Export] bool randomizeSeed = true;

    [Export] double grassHeight = -.1;
    [Export] double snowHeight = .4;
    //noise for small details in terrain
    [Export] FastNoiseLite detailNoise;
    //noise for large details in terrain 
    [Export] FastNoiseLite largeTerrainNoise;

    private Vector3[] vertArr;
    private List<int> indicieList;
    private Godot.Color[] colorArr;
    private List<float> steepnessList;
    private Vector3[] terrainVertArr;
    //normal map
    private Vector3[] normalArr;
    private int worldSize;
    public override void _Ready()
    {
        buildTerrain(randomizeSeed);
    }

    public void buildTerrain(bool randomizeSeed) {

        if (randomizeSeed) { generateSeed(); }

        initializeData();

        createVerticeArray();

        createIndicieList();

        createSteepnessList();

        applyVertexColors();

        GenerateVertexNormals();

        buildMesh();

        EmitSignal(SignalName.terrainGenerationFinished, size);
    }

    public void createSteepnessList() {
        for (int i = 0; i < indicieList.Count; i += 3) {

            //Vectors A B C
            //AB = B-A, AC = C-A
            //AB x AC = normal vector

            int Aindicie = indicieList[i];
            int Bindicie = indicieList[i + 1];
            int Cindicie = indicieList[i + 2];

            Vector3 A = terrainVertArr[Aindicie];
            Vector3 B = terrainVertArr[Bindicie];
            Vector3 C = terrainVertArr[Cindicie];

            Vector3 AB = B - A;
            Vector3 AC = C - A;

            Vector3 crossProduct = new Vector3((AC.Y * AB.Z) - (AC.Z * AB.Y),-((AC.X * AB.Z) - (AC.Z * AB.X)),(AC.X * AB.Y) - (AC.Y * AB.X));

            //divide by magnitude so it just direction
            Vector3 normal = crossProduct.Normalized();

            float steepness = normal.Dot(new Vector3(0f, 1f, 0f));

            steepnessList.Add(steepness);
            //1 = flat
            //0 = vertical
        }
    }

    public void initializeData() {
        
        //scales world with fidelity
        fidelity = 1 / fidelity;
        //makes it so higher fidelity = higher resolution landscape
        worldSize = Mathf.FloorToInt(size / fidelity) + 1;
        
        colorArr = new Godot.Color[worldSize * worldSize];
        vertArr = new Vector3[worldSize * worldSize];
        terrainVertArr = new Vector3[worldSize * worldSize];
        normalArr = new Vector3[worldSize * worldSize];
        indicieList = [];
        steepnessList = [];

        
    }

    public void generateSeed() {
        detailNoise.Seed = (int)GD.Randi();
        largeTerrainNoise.Seed = (int)GD.Randi();
    }

    public void createVerticeArray() {
        int totalIndex = 0;
        for (int x = 0; x < worldSize; x++) {
            for (int z = 0; z < worldSize; z++) {
                
                //scales x and z with fidelity
                float sizedX = x * fidelity;
                float sizedZ = z * fidelity;

                float detailNoisePos = detailNoise.GetNoise2D(sizedX, sizedZ);
                float largeNoisePos = largeTerrainNoise.GetNoise2D(sizedX, sizedZ);

                //add small / large noise to Y values
                float heightY = 0;
                heightY += (float)(largeNoisePos * terrainAmplitude);

                //terrainVertArr only cares about largeNoise
                terrainVertArr[totalIndex] = new Vector3(sizedX, heightY, sizedZ);

                heightY += (float)(detailNoisePos * detailAmplitude);

                vertArr[totalIndex] = new Vector3(sizedX, heightY, sizedZ);

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

    public void applyVertexColors()
    {
        int triangle = 0;
        for (int i = 0; i < indicieList.Count; i += 3)
        {
            int A = indicieList[i];
            int B = indicieList[i + 1];
            int C = indicieList[i + 2];

            float steepness = steepnessList[triangle++];

            float height = (terrainVertArr[A].Y + terrainVertArr[B].Y + terrainVertArr[C].Y) / 3f;

            Godot.Color faceColor = getColorFromHeightAndSteepness(height, steepness);

            colorArr[A] = faceColor;
            colorArr[B] = faceColor;
            colorArr[C] = faceColor;
        }
    }

    public Godot.Color getColorFromHeightAndSteepness(float height, float steepness) {
        Godot.Color grass = new Godot.Color(.01f, .6f, .05f);
        Godot.Color stone = new Godot.Color(.4f, .4f, .5f);
        Godot.Color snow = new Godot.Color(1f, 1f, 1f);

        float heightClamped = Math.Clamp(height / 100f, -1, 1);

        float subtractedColor = GD.Randf() / 10;
        var returnColor = stone - new Godot.Color(subtractedColor, subtractedColor, subtractedColor);


        if (heightClamped <= grassHeight){
            if (steepness > .8) {
                returnColor = grass - new Godot.Color(0f, GD.Randf() / 6, 0f) -  new Godot.Color(.1f, .1f, .1f);
            }
        }

        if (heightClamped <= grassHeight) {
            if (steepness >= .9) {
                returnColor = grass - new Godot.Color(0f, GD.Randf() / 6, 0f);
            }
        }



        if (heightClamped >= grassHeight && heightClamped <= snowHeight) {
            if (steepness >= .8) {
                returnColor += new Godot.Color(.2f, .2f, .2f);
            }
            if (steepness <= .7) {
                returnColor -= new Godot.Color(.1f, .1f, .1f);
            }
        }

        if (heightClamped >= snowHeight) {
            returnColor = snow;
        }

        if (heightClamped >= snowHeight - .2) {
            if (steepness >= .8) {
                returnColor = snow;
            }
        }

        if (heightClamped >= snowHeight - .1) {
            if (GD.Randf() >= .2) {
                returnColor += new Godot.Color(.2f, .2f, .2f);
            }
        }

        if (heightClamped >= snowHeight - .2) {
            if (GD.Randf() >= .7) {
                returnColor += new Godot.Color(.2f, .2f, .2f);
            }
        }

        return returnColor;
    }

    public void GenerateVertexNormals()
    {

        for (int i = 0; i < indicieList.Count; i += 3)
        {        
            //Vectors A B C
            //AB = B-A, AC = C-A
            //AB x AC = normal vector

            int Aindicie = indicieList[i];
            int Bindicie = indicieList[i + 1];
            int Cindicie = indicieList[i + 2];

            Vector3 A = vertArr[Aindicie];
            Vector3 B = vertArr[Bindicie];
            Vector3 C = vertArr[Cindicie];

            Vector3 AB = B - A;
            Vector3 AC = C - A;

            Vector3 crossProduct = new Vector3((AC.Y * AB.Z) - (AC.Z * AB.Y),-((AC.X * AB.Z) - (AC.Z * AB.X)),(AC.X * AB.Y) - (AC.Y * AB.X));

            normalArr[Aindicie] += crossProduct;
            normalArr[Bindicie] += crossProduct;
            normalArr[Cindicie] += crossProduct;
        }
    }

    public void createIndicieList() {
        for (int x = 0; x < worldSize - 1; x++) {
            for (int z = 0; z < worldSize - 1; z++) {

                int a = x * worldSize + z;
                int b = (x + 1) * worldSize + z;
                int c = x * worldSize + (z + 1);
                int d = (x + 1) * worldSize + (z + 1);

                //counter clockwise
                indicieList.Add(a); indicieList.Add(b); indicieList.Add(d); //triangle 1
                indicieList.Add(a); indicieList.Add(d); indicieList.Add(c); //triangle 2
            }
        }
    }

    public void buildMesh()
    {
        int[] indexArray = indicieList.ToArray();
        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);

        
        arrays[(int)Mesh.ArrayType.Vertex] = vertArr;
        arrays[(int)Mesh.ArrayType.Index] = indexArray;
        arrays[(int)Mesh.ArrayType.Color] = colorArr;
        arrays[(int)Mesh.ArrayType.Normal] = normalArr;

        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);

        var meshInstance = new MeshInstance3D();
        meshInstance.Mesh = mesh;

        var material = new StandardMaterial3D();
        material.VertexColorUseAsAlbedo = true;
        material.ShadingMode = BaseMaterial3D.ShadingModeEnum.PerPixel;
        //material.Roughness = 1;
        //material.Metallic = 1;
        meshInstance.MaterialOverride = material;
        AddChild(meshInstance);
    }
}
