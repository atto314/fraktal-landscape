using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aardvark.Algodat;
using Aardvark.Math;
using Aardvark.Rendering;
using Aardvark.Rendering.SlimDx;
using Aardvark.Runtime;
using Aardvark.SceneGraph;
using Keys = System.Windows.Forms.Keys;
using Aardvark.State;

namespace FractalLandscape
{
    public class MyVertex
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }

        public C4b color = C4b.White;

        public V3f toV3f()
        {
            V3f result = new V3f(x, y, z);

            return result;
        }

        public void normalizeForWater()
        {
            if(z<0)
            {
                z = 0;
            }
        }

    }

    public class MyQuad
    {
        public MyVertex v1 { get; set; }
        public MyVertex v2 { get; set; }
        public MyVertex v3 { get; set; }
        public MyVertex v4 { get; set; }

    }

    public class MyColorization
    {
        public C4b[] colors;
    }

    public class MyTerrain
    {
        public int size;

        public MyTerrain(int _size)
        {
            size = _size;
            terr = new MyVertex[size,size];
        }

        public MyVertex[,] terr {get;set;}

        public void normalizeForWater()
        {
            foreach(MyVertex v in terr)
            {
                v.normalizeForWater();
            }
        }
    }

    public class MyTerrainLod
    {
        public Dictionary<int, MyTerrain> terrainLod { get; set; }
        public Dictionary<int, VertexGeometry> vgLod { get; set; }

        public MyTerrainLod()
        {
            terrainLod = new Dictionary<int, MyTerrain>();
            vgLod = new Dictionary<int, VertexGeometry>();
        }

        public void setTerrainLod(int level, MyTerrain terrain)
        {
            terrainLod.Add(level, terrain);
        }

        public void finalize(bool normalizeForWater)
        {

            foreach (int level in terrainLod.Keys)
            {
                MyTerrain currentTerr = this.getTerrainLod(level);
                if (normalizeForWater)
                {
                    currentTerr.normalizeForWater();
                }

            }
        }

        public void render()
        {

            foreach (int level in terrainLod.Keys)
            {
                MyTerrain currentTerr = this.getTerrainLod(level);

                vgLod.Add(level, this.terrainToVg(currentTerr));
            }
        }

        public MyTerrain getTerrainLod(int level)
        {
            MyTerrain result;
            terrainLod.TryGetValue(level, out result);
            return result;
        }

        public VertexGeometry getVgLod(int level)
        {
            VertexGeometry result;
            vgLod.TryGetValue(level, out result);
            return result;
        }

        private VertexGeometry terrainToVg(MyTerrain terrain)
        {
            //build a VertexGeometry from our collection of vertices

            List<int> IndicesList = new List<int>();
            List<V3f> VerticesList = new List<V3f>();
            List<C4b> ColorsList = new List<C4b>();

            //start: add entire top row
            //VerticesList.Add(terrain.terr[0, 0].toV3f());
            for (int x = 0; x < terrain.size; x++ )
            {
                VerticesList.Add(terrain.terr[x, 0].toV3f());
                ColorsList.Add(terrain.terr[x, 0].color);
            }

            //for all vertex positions, take the right, bottom and bottom right neighbours position, add the two missing vertices (bottom and bottom-right) to the vertex list, 
            //and add all four serialized indices forming two triangles to the index list
            for (int y = 0; y < terrain.size - 1; y++)
            {
                //in each column, add the bottom neighbour
                VerticesList.Add(terrain.terr[0, y + 1].toV3f());
                ColorsList.Add(terrain.terr[0, y + 1].color);

                for (int x = 0; x < terrain.size - 1; x++)
                {
                    //in each row, add the bottom-right neighbour
                    VerticesList.Add(terrain.terr[x + 1, y + 1].toV3f());
                    ColorsList.Add(terrain.terr[x + 1, y + 1].color);

                    //get the serialized indices
                    int currentRowOffset = terrain.size * y;
                    int belowRowOffset = terrain.size * (y + 1);

                    int currentIndex = currentRowOffset + x;
                    int rightNeighbourIndex = currentRowOffset + x + 1;
                    int bottomNeighbourIndex = belowRowOffset + x;
                    int bottomRightNeighbourIndex = belowRowOffset + x + 1;

                    //make two triangles (counter-clockwise) and add them to the index list
                    IndicesList.Add(bottomNeighbourIndex);
                    IndicesList.Add(rightNeighbourIndex);
                    IndicesList.Add(currentIndex);

                    IndicesList.Add(bottomNeighbourIndex);
                    IndicesList.Add(bottomRightNeighbourIndex);
                    IndicesList.Add(rightNeighbourIndex);
                }


            }

            //store the result into a VertexGeometry. 
            var result = new VertexGeometry()
            {
                
                Indices = IndicesList.ToArray(),

                Positions = VerticesList.ToArray(),

                Colors = ColorsList.ToArray(),
            };

            return result;
        }
    }

    public class FractalTerrain
    {
        public float initialScale { get; set; }

        public float roughness = 1.0f;
        public float flatness = 1.0f;

        public bool drawWater;
        public bool colorize;
        public int selectedColorization = 0;

        private Random rand = new Random();

        private VertexGeometry vg;

        private MyQuad startVertices;

        private MyTerrainLod terrainLod;

        private List<MyColorization> colorizations;
        private C4b waterColor;

        private int maxlevel;

        public FractalTerrain(float scale, bool drawWater, bool colorize, int colorIndex)
        {
            this.drawWater = drawWater;
            this.colorize = colorize;
            initialScale = scale;
            init();
            selectColorization(colorIndex);
        }

        private void init()
        {
            //base mesh is a single plane
            terrainLod = new MyTerrainLod();
            colorizations = new List<MyColorization>();

            startVertices = new MyQuad();

            startVertices.v1 = new MyVertex { x = -1, y = -1, z = 0 };
            startVertices.v2 = new MyVertex { x = 1, y = -1, z = 0 };
            startVertices.v3 = new MyVertex { x = 1, y = 1, z = 0 };
            startVertices.v4 = new MyVertex { x = -1, y = 1, z = 0 };

            scaleQuad(startVertices, initialScale);

            vg = quadToVertexGeometry(startVertices);

            //add default colorization - handmade
            MyColorization defaultCol = new MyColorization();

            defaultCol.colors = new C4b[] {
            new C4b(243,247,106),
            new C4b(252,230,81),
            new C4b(227,181,82),
            new C4b(212,187,76),
            new C4b(209,176,115),
            new C4b(181,170,123),
            new C4b(118,168,89),
            new C4b(118,219,18),
            new C4b(118,219,18),
            new C4b(133,237,28),
            new C4b(133,237,28),
            new C4b(129,201,142),
            new C4b(129,201,142),
            new C4b(154,181,160),
            new C4b(197,209,206),
            new C4b(220,242,237),
            new C4b(235,247,244),
            new C4b(242,244,245)
            };

            waterColor = C4b.Blue;


            colorizations.Add(defaultCol);
            

            //some programmatic colorizations
            MyColorization grayscaleCol = new MyColorization();

            List<C4b> grayscaleVal = new List<C4b>();
            int gsvalues = 1000;

            for(int i=0; i<gsvalues; i++)
            {
                double curVal = Math.Sqrt((double)i / (double)gsvalues);
                grayscaleVal.Add(new C4b(curVal, curVal, curVal, 1.0d));
            }

            grayscaleCol.colors = grayscaleVal.ToArray();

            colorizations.Add(grayscaleCol);



            MyColorization rainbowCol = new MyColorization();

            List<C4b> rainbowVal = new List<C4b>();
            int rvalues = 10000;

            float rPortion = 5.0f;
            float gPortion = 6.0f;
            float bPortion = 7.0f;

            for (int i = 0; i < rvalues; i++)
            {
                double curVal = ((double)i / (double)rvalues);
                rainbowVal.Add(new C4b((curVal * rPortion) % 1.0d, (curVal * gPortion) % 1.0d, (curVal * bPortion)%1.0d, 1.0d));
            }

            rainbowCol.colors = rainbowVal.ToArray();

            colorizations.Add(rainbowCol);

            //selectColorization(0);
        }

        public void selectColorization(int index)
        {
            selectedColorization = index;
        }

        //generate and store all terrain lod levels up to the specified level
        public void buildTerrain(int level, float roughness, float flatness)
        {
            maxlevel = level;

            //build the terrain from the start vertices up to a tessellation level
            this.roughness = roughness;
            this.flatness = flatness;

            MyTerrain previousTerr = quadToTerr(startVertices);
            terrainLod.setTerrainLod(0, previousTerr);

            //for levels 1 ... level
            for(int i=1; i<=level; i++)
            {
                //get terrain size for current level
                int terrainsize = calcTerrainSize(i);
                
                //create new terrain
                MyTerrain currentTerr = new MyTerrain(terrainsize);

                //copy over the old values from the last terrain
                copyOldTerrainValuesLod(previousTerr, currentTerr);

                //create the new values between the old values
                generateNewTerrainValues(currentTerr, i);

                //add it to our LOD data structure
                terrainLod.setTerrainLod(i, currentTerr);

                //repeat
                previousTerr = currentTerr;
            }

            terrainLod.finalize(drawWater);

            //colorize the terrains
            if (colorize)
            {
                foreach (var currentTerr in terrainLod.terrainLod.Values)
                {
                    this.colorizeTerrain(currentTerr, colorizations[selectedColorization]);
                }
            }

            terrainLod.render();
        }

        private void generateNewTerrainValues(MyTerrain currentTerrain, int currentLevel)
        {
            //in the first step, interpolate 4 vertices diagonally. need only quads of even-indexed vertices, starting from top left
            for(int x=0; x<currentTerrain.size-2; x=x+2)
            {
                for(int y=0; y<currentTerrain.size-2; y=y+2)
                {
                    //new vertex in the middle is the average of the four diagonal vertices
                    MyVertex topLeft = currentTerrain.terr[x, y];
                    MyVertex topRight = currentTerrain.terr[x+2, y];
                    MyVertex bottomLeft = currentTerrain.terr[x, y+2];
                    MyVertex bottomRight = currentTerrain.terr[x+2, y+2];

                    MyVertex newVertex = calcNewVertex(topLeft, topRight, bottomLeft, bottomRight, currentLevel);

                    //insert the new vertex into the center of the four
                    currentTerrain.terr[x + 1, y + 1] = newVertex;
                }
            }

            //in the second step, interpolate the remaining vertices in-between. the border vertices have 3 neighbours, the inner vertices have 4.
            //border vertices.
            for(int index=1; index<currentTerrain.size-1; index=index+2)
            {
                //horizontal borders
                //top
                MyVertex before = currentTerrain.terr[index - 1, 0];
                MyVertex after = currentTerrain.terr[index + 1, 0];
                MyVertex inner = currentTerrain.terr[index, 1];
                currentTerrain.terr[index, 0] = calcNewVertex(before, after, inner, currentLevel);

                //bottom
                before = currentTerrain.terr[index - 1, currentTerrain.size - 1];
                after = currentTerrain.terr[index + 1, currentTerrain.size - 1];
                inner = currentTerrain.terr[index, currentTerrain.size - 2];
                currentTerrain.terr[index, currentTerrain.size - 1] = calcNewVertex(before, after, inner, currentLevel);

                //vertical border
                //left
                before = currentTerrain.terr[0, index - 1];
                after = currentTerrain.terr[0, index + 1];
                inner = currentTerrain.terr[1, index];
                currentTerrain.terr[0, index] = calcNewVertex(before, after, inner, currentLevel);

                //right
                before = currentTerrain.terr[currentTerrain.size - 1, index - 1];
                after = currentTerrain.terr[currentTerrain.size - 1, index + 1];
                inner = currentTerrain.terr[currentTerrain.size - 2, index];
                currentTerrain.terr[currentTerrain.size - 1, index] = calcNewVertex(before, after, inner, currentLevel);
            }

            //inner vertices
            for (int index = 1; index < currentTerrain.size - 1; index = index + 2)
            {
                for(int other = 2; other < currentTerrain.size - 2; other = other + 2)
                {
                    //columns
                    MyVertex topLeft = currentTerrain.terr[index-1, other];
                    MyVertex topRight = currentTerrain.terr[index, other-1];
                    MyVertex bottomLeft = currentTerrain.terr[index+1, other];
                    MyVertex bottomRight = currentTerrain.terr[index, other+1];

                    MyVertex newVertex = calcNewVertex(topLeft, topRight, bottomLeft, bottomRight, currentLevel);

                    currentTerrain.terr[index, other] = newVertex;

                    //rows
                    topLeft = currentTerrain.terr[other - 1, index];
                    topRight = currentTerrain.terr[other, index - 1];
                    bottomLeft = currentTerrain.terr[other + 1, index];
                    bottomRight = currentTerrain.terr[other, index + 1];

                    newVertex = calcNewVertex(topLeft, topRight, bottomLeft, bottomRight, currentLevel);

                    currentTerrain.terr[other, index] = newVertex;
                }
            }

            //result is the new terrain with all previously empty values filled out
        }

        private MyVertex calcNewVertex(MyVertex before, MyVertex after, MyVertex inner, int currentLevel)
        {
            //make a new vertex from three vertices (on the border of a quadratic terrain). interpolate the outer two vertices for x,y values, 
            //add the inner vertex for the height, add random variation
            MyVertex result = new MyVertex();

            float randomVariation = getRandomValue(currentLevel);

            result.x = (before.x + after.x) / 2.0f;
            result.y = (before.y + after.y) / 2.0f;
            result.z = (before.z + after.z + inner.z) / 3.0f + randomVariation;

            return result;
        }

        private MyVertex calcNewVertex(MyVertex topLeft, MyVertex topRight, MyVertex bottomLeft, MyVertex bottomRight, int currentLevel)
        {
            //make new vertex from a quad of four vertices. Interpolate their values and add random variation.
            MyVertex result = new MyVertex();

            float randomVariation = getRandomValue(currentLevel);

            result.x = (topLeft.x + topRight.x + bottomLeft.x + bottomRight.x) / 4.0f;
            result.y = (topLeft.y + topRight.y + bottomLeft.y + bottomRight.y) / 4.0f;
            result.z = (topLeft.z + topRight.z + bottomLeft.z + bottomRight.z) / 4.0f + randomVariation;

            return result;
        }

        private void copyOldTerrainValuesLod(MyTerrain previousTerrain, MyTerrain currentTerrain)
        {
            //copy each vertex from the last terrain into the current terrain, leaving the spaces inbetween as 0
            for(int x=0; x<previousTerrain.size; x++)
            {
                for(int y=0; y<previousTerrain.size; y++)
                {
                    MyVertex previousVertex = previousTerrain.terr[x, y];

                    //get index by linear mapping
                    //int currentX = (int)Math.Round((((float)x) / (float)previousTerrain.size) * (float)currentTerrain.size);
                    //int currentY = (int)Math.Round((((float)y) / (float)previousTerrain.size) * (float)currentTerrain.size);

                    int currentX = 2 * x;
                    int currentY = 2 * y;

                    currentTerrain.terr[currentX, currentY] = previousVertex;
                }
            }
        }

        private int calcTerrainSize(int lodlevel)
        {
            //get terrain size for lod level. Currently using square terrain with size = 2^n+1

            return (int)(Math.Pow(2.0f, (float)lodlevel) + 1.0f);
        }

        private VertexGeometry quadToVertexGeometry(MyQuad quad)
        {
            var result = new VertexGeometry()
            {
                Indices = new[] { 0, 1, 2, 0, 2, 3 },

                Positions = new[] { new V3f(quad.v1.x,quad.v1.y,quad.v1.z), new V3f(quad.v2.x,quad.v2.y,quad.v2.z),
                                    new V3f(quad.v3.x,quad.v3.y,quad.v3.z), new V3f(quad.v4.x,quad.v4.y,quad.v4.z) },

                Colors = new[] { C4b.White, C4b.White, C4b.White, C4b.White },

            };

            return result;
        }

        private MyTerrain quadToTerr(MyQuad quad)
        {
            MyTerrain result = new MyTerrain(2);
            result.terr[0, 0] = quad.v4;
            result.terr[1, 0] = quad.v3;
            result.terr[0, 1] = quad.v1;
            result.terr[1, 1] = quad.v2;
            return result;
        }

        private void scaleQuad(MyQuad quad, float factor)
        {
            //scale the quad if it is centered around the origin
            quad.v1.x = quad.v1.x * factor;
            quad.v2.x = quad.v2.x * factor;
            quad.v3.x = quad.v3.x * factor;
            quad.v4.x = quad.v4.x * factor;

            quad.v1.y = quad.v1.y * factor;
            quad.v2.y = quad.v2.y * factor;
            quad.v3.y = quad.v3.y * factor;
            quad.v4.y = quad.v4.y * factor;

            quad.v1.z = quad.v1.z * factor;
            quad.v2.z = quad.v2.z * factor;
            quad.v3.z = quad.v3.z * factor;
            quad.v4.z = quad.v4.z * factor;
        }

        private float getRandomValue(int level)
        {
            //return a random value to be used as variation in the terrain
            float result = 0.0f;
            
            double sigma = 1.0d;

            //sigma = sigma * (double)initialScale;

            double stdDev = Math.Pow(sigma, 2.0d) / Math.Pow(2.0d, (double)level * (double)roughness);

            //gaussian distribution RNG taken from http://stackoverflow.com/questions/218060/random-gaussian-variables
            double u1 = rand.NextDouble(); //these are uniform(0,1) random doubles
            double u2 = rand.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                         Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
            double randNormal =
                         0 + stdDev * randStdNormal; //random normal(mean,stdDev^2)

            result = (float)randNormal*initialScale;

            //make the terrain flatter in the first level
            if(level < ((float)maxlevel*(2.0f/3.0f)))
            {
                result = result * 1.0f / (float)Math.Pow((double)flatness, 1.1d);
            }

            return result;
        }

        //returns the terrain with the selected lod level.
        public Sg.VertexGeometrySet toVertexGeometrySet(int LodLevel)
        {
            var vgs = new Sg.VertexGeometrySet() { VertexGeometryList = terrainLod.getVgLod(LodLevel).IntoList() };

            return vgs;
        }

        private void colorizeTerrain(MyTerrain terrain, MyColorization colors)
        {
            //for each vertex, linearly map its z value onto the color list (from lowest to highest)

            float minimumZ = 0;
            float maximumZ = 0;
            foreach(var vert in terrain.terr)
            {
                var currentZ = vert.z;
                if (currentZ < minimumZ)
                {
                    minimumZ = currentZ;
                }
                if (currentZ > maximumZ)
                {
                    maximumZ = currentZ;
                }
            }

            //numerical errors
            float delta = 0.000001f;
            minimumZ = minimumZ - delta;
            maximumZ = maximumZ + delta;

            float zRange = maximumZ - minimumZ;

            C4b[] col = colors.colors;
            int numCol = col.Length-1;

            foreach (var vert in terrain.terr)
            {
                //get normalized color index
                var currentZ = vert.z;

                if(drawWater)
                {
                    if(currentZ<delta)
                    {
                        vert.color = waterColor;
                        continue;
                    }
                }
                

                float colorIndex = ((vert.z - minimumZ) / zRange) * (float)numCol;

                int currentColorIndex = (int)colorIndex;

                vert.color = col[currentColorIndex];
            }

        }

    }
}
