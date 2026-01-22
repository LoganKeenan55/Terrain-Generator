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
    [Export(PropertyHint.Range, "0,5,0.1")] float fidelity = 2;
    [Export(PropertyHint.Range, "10,5000,10")] int size = 1000;
    [Export] bool randomizeSeed = true;

    [Export] double grassHeight = -.2;
    [Export] double snowHeight = .4;
    //noise for small details in terrain
    [Export] FastNoiseLite detailNoise;
    //noise for large details in terrain 
    [Export] FastNoiseLite largeTerrainNoise;

    
    private Vector3[] vertArr;
    private List<int> indicieList;
    private Godot.Color[] colorArr;
    private List<float> detailSteepnessList;
    private List<float> terrainSteepnessList;
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

        GenerateVertexNormals();

        applyVertexColors();
        
        buildMesh();

        EmitSignal(SignalName.terrainGenerationFinished, size);
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
        detailSteepnessList = [];
        terrainSteepnessList = [];
        
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

            float detailSteepness = detailSteepnessList[triangle];
            float terrainSteepness = terrainSteepnessList[triangle];
            triangle++;
            

            float height = (terrainVertArr[A].Y + terrainVertArr[B].Y + terrainVertArr[C].Y) / 3f;

            Godot.Color faceColor = getColorFromHeightAndSteepness(height, detailSteepness, terrainSteepness);

            colorArr[A] = faceColor;
            colorArr[B] = faceColor;
            colorArr[C] = faceColor;
        }
    }

    public Godot.Color getColorFromHeightAndSteepness(float height, float detailSteepness,float terrainSteepness) {
        Godot.Color grass = new Godot.Color(.01f, .6f, .05f) - new Godot.Color(0f, GD.Randf() / 6, 0f);;
        Godot.Color stone = new Godot.Color(.4f, .4f, .5f);
        Godot.Color snow = new Godot.Color(1f, 1f, 1f);

        float heightClamped = Math.Clamp(height / 100f, -1, 1);

        float subtractedColor = GD.Randf() / 10;
        var returnColor = stone - new Godot.Color(subtractedColor, subtractedColor, subtractedColor);

        //GRASS
        if (heightClamped <= grassHeight-.1) {
            if (terrainSteepness >= .7) {
                returnColor = grass;
            }
        }

        if (heightClamped <= grassHeight){
            if (detailSteepness > .6) {
                returnColor = grass -  new Godot.Color(.15f, .15f, .15f);
            }
        }

        //STONE
        if (heightClamped >= grassHeight && heightClamped <= snowHeight) {
            if (terrainSteepness >= .8) {
                returnColor += new Godot.Color(.2f, .2f, .2f);
            }
            if (terrainSteepness <= .7) {
                returnColor -= new Godot.Color(.1f, .1f, .1f);
            }
        }

        //SNOW
        if (heightClamped >= snowHeight+.1) {
            returnColor = snow;
        }

        if (heightClamped >= snowHeight - .2) {
            if (detailSteepness >= .6) {
                returnColor += new Godot.Color(.5f, .5f, .5f);
            }
        }

        if (heightClamped >= snowHeight - .1) {
             if (detailSteepness >= .8) {
                returnColor += new Godot.Color(.8f, .8f, .8f);
             }
        }

        if (heightClamped >= snowHeight - .1) {
            returnColor += new Godot.Color(.2f, .2f, .2f);
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

            //for final terrain, uses all details, needed for lighting and some coloring
            Vector3 A = vertArr[Aindicie];
            Vector3 B = vertArr[Bindicie];
            Vector3 C = vertArr[Cindicie];

            //for terrain before details are added, good for colors where small details don't matter
            Vector3 D = terrainVertArr[Aindicie];
            Vector3 E = terrainVertArr[Bindicie];
            Vector3 F = terrainVertArr[Cindicie];

            Vector3 AB = B - A;
            Vector3 AC = C - A;

            Vector3 DE = E - D;
            Vector3 DF = F - D;
            
            Vector3 detailCrossProduct = AC.Cross(AB).Normalized();
            Vector3 terrainCrossProduct = DF.Cross(DE).Normalized();

            normalArr[Aindicie] += detailCrossProduct;
            normalArr[Bindicie] += detailCrossProduct;
            normalArr[Cindicie] += detailCrossProduct;

            //0 = vertical 1 = flat
            float detailSteepness = detailCrossProduct.Dot(new Vector3(0f, 1f, 0f));
            float terrainSteepness = terrainCrossProduct.Dot(new Vector3(0f, 1f, 0f));

            detailSteepnessList.Add(detailSteepness);
            terrainSteepnessList.Add(terrainSteepness);
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
        meshInstance.MaterialOverride = material;
        AddChild(meshInstance);
    }
}
