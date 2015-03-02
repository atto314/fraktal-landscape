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
    public class MyPoint
    {
        public int x = 0;
        public int y = 0;

        public MyPoint(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public MyPoint halfwayTo(MyPoint other)
        {
            MyPoint result = new MyPoint(0, 0);
            result.x = (this.x + other.x) / 2;
            result.y = (this.y + other.y) / 2;

            return result;
        }
    }

    public class MyLeaf
    {
        public MyLeaf(MyPoint minIndex, MyPoint maxIndex, int depth)
        {
            this.minIndex = minIndex;
            this.maxIndex = maxIndex;
            this.depth = depth;

            bHasChildren = false;

            this.centerIndex = minIndex.halfwayTo(maxIndex);
        }

        public MyPoint minIndex, maxIndex;

        public int depth;

        public MyPoint centerIndex;

        private bool bHasChildren;

        public MyLeaf topLeftChild;
        public MyLeaf topRightChild;
        public MyLeaf bottomLeftChild;
        public MyLeaf bottomRightChild;

        public bool hasChildren()
        {
            return bHasChildren;
        }

        public void addChildren(MyLeaf topLeftChild, MyLeaf topRightChild, MyLeaf bottomLeftChild, MyLeaf bottomRightChild)
        {
            this.topLeftChild = topLeftChild;
            this.topRightChild = topRightChild;
            this.bottomLeftChild = bottomLeftChild;
            this.bottomRightChild = bottomRightChild;
            bHasChildren = true;
        }

    }


    public class MyVertex
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }

        public C4b color = C4b.White;

        public MyVertex normal;

        //the list index. if it is not -1, it is already in the vertex list and its index can be added to the index list
        public int listIndex = -1;

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

        public void normalizeVector()
        {
            float length = this.length();

            x = x / length;
            y = y / length;
            z = z / length;
        }

        public float length()
        {
            return (float)Math.Sqrt(x * x + y * y + z * z);
        }

        public static MyVertex operator -(MyVertex thisVector)
        {
            thisVector.x = thisVector.x * (-1);
            thisVector.y = thisVector.y * (-1);
            thisVector.z = thisVector.z * (-1);

            return thisVector;
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

        public int runningIndex = 0; //running index variable for generation of vertexList

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

        public MyTerrainLod(float errorThreshold)
        {
            this.errorThreshold = errorThreshold;
            terrainLod = new Dictionary<int, MyTerrain>();
            vgLod = new Dictionary<int, VertexGeometry>();
        }

        public float errorThreshold;

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

        public void render(bool optimized, bool allLevels)
        {
            if (allLevels)
            {
            foreach (int level in terrainLod.Keys)
            {

                MyTerrain currentTerr = this.getTerrainLod(level);

                if (!optimized)
                {
                    vgLod.Add(level, this.terrainToVg(currentTerr));
                }
                else
                {
                    vgLod.Add(level, this.terrainToVgOptimized(currentTerr));
                }
            }
            }
            else
            {
                int maxlevel = terrainLod.Max(t => t.Key);
                MyTerrain currentTerr = this.getTerrainLod(maxlevel);

                if (!optimized)
                {
                    vgLod.Add(maxlevel, this.terrainToVg(currentTerr));
                }
                else
                {
                    vgLod.Add(maxlevel, this.terrainToVgOptimized(currentTerr));
                }
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

        private MyVertex averageOfFour(MyVertex v1, MyVertex v2, MyVertex v3, MyVertex v4)
        {
            MyVertex result = new MyVertex();

            result.x = (v1.x + v2.x + v3.x + v4.x) / 4;
            result.y = (v1.y + v2.y + v3.y + v4.y) / 4;
            result.z = (v1.z + v2.z + v3.z + v4.z) / 4;

            return result;
        }

        private MyVertex averageOfTwo(MyVertex v1, MyVertex v2)
        {
            MyVertex result = new MyVertex();

            result.x = (v1.x + v2.x) / 2;
            result.y = (v1.y + v2.y) / 2;
            result.z = (v1.z + v2.z) / 2;

            return result;
        }

        private MyVertex cross(MyVertex v1, MyVertex v2)
        {
            MyVertex result = new MyVertex();

            result.x = (v1.y * v2.z) - (v1.z * v2.y);
            result.y = (v1.z * v2.x) - (v1.x * v2.z);
            result.z = (v1.x * v2.y) - (v1.y * v2.x);

            result.normalizeVector();

            return result;
        }

        private MyVertex vectorBetweenPoints(MyVertex p1, MyVertex p2)
        {
            MyVertex result = new MyVertex();

            result.x = p2.x - p1.x;
            result.y = p2.y - p1.y;
            result.z = p2.z - p1.z;

            return result;
        }

        private MyVertex directionBetweenPoints(MyVertex p1, MyVertex p2)
        {
            MyVertex result = vectorBetweenPoints(p1,p2);

            result.normalizeVector();

            return result;
        }


        private VertexGeometry terrainToVg(MyTerrain terrain)
        {
            //build a VertexGeometry from our collection of vertices

            List<int> IndicesList = new List<int>();
            List<V3f> VerticesList = new List<V3f>();
            List<C4b> ColorsList = new List<C4b>();
            List<V3f> NormalsList = new List<V3f>();


            //Calculate surface normals in a separate generation step.
            //for each inner vertex, calculate the normal as the vector perpendicular to its (approximate) gradient.
            for (int y = 1; y < terrain.size - 1; y++)
            {
                for (int x = 1; x < terrain.size - 1; x++)
                {
                    MyVertex current = terrain.terr[x, y];
                    MyVertex top = terrain.terr[x, y - 1];
                    MyVertex right = terrain.terr[x + 1, y];
                    MyVertex bottom = terrain.terr[x, y + 1];
                    MyVertex left = terrain.terr[x - 1, y];

                    MyVertex dTop = directionBetweenPoints(current, top);
                    MyVertex dRight = directionBetweenPoints(current, right);
                    MyVertex dBottom = directionBetweenPoints(current, bottom);
                    MyVertex dLeft = directionBetweenPoints(current, left);

                    MyVertex TR = cross(dTop, dRight);
                    MyVertex BL = cross(dBottom, dLeft);

                    MyVertex RB = cross(dRight, dBottom);
                    MyVertex LT = cross(dLeft, dTop);
                    MyVertex norm1 = averageOfFour(TR, RB, BL, LT);

                    MyVertex topright = terrain.terr[x + 1, y - 1];
                    MyVertex bottomright = terrain.terr[x + 1, y + 1];
                    MyVertex topleft = terrain.terr[x - 1, y - 1];
                    MyVertex bottomleft = terrain.terr[x - 1, y + 1];

                    MyVertex dTR = directionBetweenPoints(current, topright);
                    MyVertex dBR = directionBetweenPoints(current, bottomright);
                    MyVertex dTL = directionBetweenPoints(current, topleft);
                    MyVertex dBL = directionBetweenPoints(current, bottomleft);

                    MyVertex TRBR = cross(dTR, dBR);
                    MyVertex BRBL = cross(dBR, dBL);
                    MyVertex BLTL = cross(dBL, dTL);
                    MyVertex TLTR = cross(dTL, dTR);
                    MyVertex norm2 = averageOfFour(TRBR, BRBL, BLTL, TLTR);

                    MyVertex norm = averageOfTwo(norm1, norm2);

                    //MyVertex norm = averageOfTwo(TR, BL);
                    norm.normalizeVector();

                    current.normal = -norm;
                }
            }
            //separate handling of the border vertices
            for (int index = 0; index < terrain.size; index++)
            {
                MyVertex norm = new MyVertex() { x = 0, y = 0, z = 1 };

                terrain.terr[index, 0].normal = norm;
                terrain.terr[0, index].normal = norm;
                terrain.terr[index, terrain.size - 1].normal = norm;
                terrain.terr[terrain.size - 1, index].normal = norm;
            }


            //now add the vertices to our data structure
            //start: add entire top row
            //VerticesList.Add(terrain.terr[0, 0].toV3f());
            for (int x = 0; x < terrain.size; x++ )
            {
                VerticesList.Add(terrain.terr[x, 0].toV3f());
                ColorsList.Add(terrain.terr[x, 0].color);
                NormalsList.Add(terrain.terr[x, 0].normal.toV3f());
            }

            //for all vertex positions, take the right, bottom and bottom right neighbours position, add the two missing vertices (bottom and bottom-right) to the vertex list, 
            //and add all four serialized indices forming two triangles to the index list
            for (int y = 0; y < terrain.size - 1; y++)
            {
                //in each column, add the bottom neighbour
                VerticesList.Add(terrain.terr[0, y + 1].toV3f());
                ColorsList.Add(terrain.terr[0, y + 1].color);
                NormalsList.Add(terrain.terr[0, y + 1].normal.toV3f());

                for (int x = 0; x < terrain.size - 1; x++)
                {
                    //in each row, add the bottom-right neighbour
                    VerticesList.Add(terrain.terr[x + 1, y + 1].toV3f());
                    ColorsList.Add(terrain.terr[x + 1, y + 1].color);
                    NormalsList.Add(terrain.terr[x + 1, y + 1].normal.toV3f());

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

                Normals = NormalsList.ToArray(),

            };

            return result;
        }

        private VertexGeometry terrainToVgOptimized(MyTerrain terrain)
        {
            //build a VertexGeometry from our collection of vertices

            List<int> IndicesList = new List<int>();
            List<V3f> VerticesList = new List<V3f>();
            List<C4b> ColorsList = new List<C4b>();
            List<V3f> NormalsList = new List<V3f>();


            //Calculate surface normals in a separate generation step.
            //for each inner vertex, calculate the normal as the vector perpendicular to its (approximate) gradient.
            for (int y = 1; y < terrain.size - 1; y++)
            {
                for (int x = 1; x < terrain.size - 1; x++)
                {
                    MyVertex current = terrain.terr[x, y];
                    MyVertex top = terrain.terr[x, y - 1];
                    MyVertex right = terrain.terr[x + 1, y];
                    MyVertex bottom = terrain.terr[x, y + 1];
                    MyVertex left = terrain.terr[x - 1, y];

                    MyVertex dTop = directionBetweenPoints(current, top);
                    MyVertex dRight = directionBetweenPoints(current, right);
                    MyVertex dBottom = directionBetweenPoints(current, bottom);
                    MyVertex dLeft = directionBetweenPoints(current, left);

                    MyVertex TR = cross(dTop, dRight);
                    MyVertex BL = cross(dBottom, dLeft);

                    MyVertex RB = cross(dRight, dBottom);
                    MyVertex LT = cross(dLeft, dTop);
                    MyVertex norm1 = averageOfFour(TR, RB, BL, LT);

                    MyVertex topright = terrain.terr[x + 1, y - 1];
                    MyVertex bottomright = terrain.terr[x + 1, y + 1];
                    MyVertex topleft = terrain.terr[x - 1, y - 1];
                    MyVertex bottomleft = terrain.terr[x - 1, y + 1];

                    MyVertex dTR = directionBetweenPoints(current, topright);
                    MyVertex dBR = directionBetweenPoints(current, bottomright);
                    MyVertex dTL = directionBetweenPoints(current, topleft);
                    MyVertex dBL = directionBetweenPoints(current, bottomleft);

                    MyVertex TRBR = cross(dTR, dBR);
                    MyVertex BRBL = cross(dBR, dBL);
                    MyVertex BLTL = cross(dBL, dTL);
                    MyVertex TLTR = cross(dTL, dTR);
                    MyVertex norm2 = averageOfFour(TRBR, BRBL, BLTL, TLTR);

                    MyVertex norm = averageOfTwo(norm1, norm2);

                    //MyVertex norm = averageOfTwo(TR, BL);
                    norm.normalizeVector();

                    current.normal = -norm;
                }
            }

            //separate handling of the border vertices
            for (int index = 0; index < terrain.size; index++)
            {
                MyVertex norm = new MyVertex() { x = 0, y = 0, z = 1 };

                terrain.terr[index, 0].normal = norm;
                terrain.terr[0, index].normal = norm;
                terrain.terr[index, terrain.size - 1].normal = norm;
                terrain.terr[terrain.size - 1, index].normal = norm;
            }


            //create a quadtree from our vertices. Check each cell if a subdivision is needed. If so, subdivide.
            
            
            //preconditions
            MyPoint minIndex = new MyPoint(0, 0);
            MyPoint maxIndex = new MyPoint(terrain.size - 1, terrain.size - 1);
            terrain.runningIndex = 0;

            //quadtree and stack for tree traversal.
            MyLeaf root = new MyLeaf(minIndex, maxIndex, 0);

            Stack<MyLeaf> stack = new Stack<MyLeaf>();
            stack.Push(root);

            int i = 0;

            //algorithm
            while(stack.Count != 0 && i <= 100000)   //break when stack is empty 
            {
                i++;

                MyLeaf currentLeaf = stack.Pop();

                if(currentLeaf.depth >= terrain.size-2) 
                {
                    //if this leaf is at the smalles possible terrain resolution, we can not subdivide further. it is finished
                    continue;
                }

                float currentError = calcLeafError(terrain, currentLeaf);

                if(currentError <= errorThreshold)
                {
                    //if this leaf's error is smaller than the error threshold, it has enough detail. the leaf is finished
                    continue;
                }
                else
                {
                    //if not, then the leaf needs to be subidivided. divide it into four equal parts, assign them as this leaf's children,
                    //and push them on the stack.

                    //new children are the four equal subdivision results of the current leaf
                    int newDepth = currentLeaf.depth + 1;
                    MyLeaf topLeftChild = new MyLeaf(new MyPoint(currentLeaf.minIndex.x, currentLeaf.minIndex.y), new MyPoint(currentLeaf.centerIndex.x, currentLeaf.centerIndex.y), newDepth);
                    MyLeaf topRightChild = new MyLeaf(new MyPoint(currentLeaf.centerIndex.x, currentLeaf.minIndex.y), new MyPoint(currentLeaf.maxIndex.x, currentLeaf.centerIndex.y), newDepth);
                    MyLeaf bottomRightChild = new MyLeaf(new MyPoint(currentLeaf.centerIndex.x, currentLeaf.centerIndex.y), new MyPoint(currentLeaf.maxIndex.x, currentLeaf.maxIndex.y), newDepth);
                    MyLeaf bottomLeftChild = new MyLeaf(new MyPoint(currentLeaf.minIndex.x, currentLeaf.centerIndex.y), new MyPoint(currentLeaf.centerIndex.x, currentLeaf.maxIndex.y), newDepth);

                    //assign the new children as this leaf's children
                    currentLeaf.addChildren(topLeftChild, topRightChild, bottomLeftChild, bottomRightChild);

                    //push them on the stack
                    stack.Push(topLeftChild);
                    stack.Push(topRightChild);
                    stack.Push(bottomRightChild);
                    stack.Push(bottomLeftChild);

                    //done
                }//if
            }//while

            
            //the quadtree now contains the final vertices within its nodes that have no children. 
            //traverse the quadtree. If a leaf is found, add its four corners to the final geometry.
 
            i=0;
            stack.Push(root);

            while (stack.Count != 0 && i <= 100000)   //break when stack is empty 
            {
                i++;

                MyLeaf currentLeaf = stack.Pop();

                if(currentLeaf.hasChildren())
                {
                    //this node has children, push them to the stack and continue
                    stack.Push(currentLeaf.topLeftChild);
                    stack.Push(currentLeaf.topRightChild);
                    stack.Push(currentLeaf.bottomRightChild);
                    stack.Push(currentLeaf.bottomLeftChild);

                    continue;
                }
                else
                {
                    //this node has no children, the four vertices at its corners belong to the final mesh

                    MyVertex topLeftVertex = getVertex(terrain, currentLeaf.minIndex.x, currentLeaf.minIndex.y);
                    MyVertex topRightVertex = getVertex(terrain, currentLeaf.maxIndex.x, currentLeaf.minIndex.y);
                    MyVertex bottomRightVertex = getVertex(terrain, currentLeaf.maxIndex.x, currentLeaf.maxIndex.y);
                    MyVertex bottomLeftVertex = getVertex(terrain, currentLeaf.minIndex.x, currentLeaf.maxIndex.y);

                    //the square patch represented by this leaf is added as two triangles

                    addVertex(bottomLeftVertex, currentLeaf.minIndex.x, currentLeaf.maxIndex.y, IndicesList, VerticesList, ColorsList, NormalsList, terrain);
                    addVertex(topRightVertex, currentLeaf.maxIndex.x, currentLeaf.minIndex.y, IndicesList, VerticesList, ColorsList, NormalsList, terrain);
                    addVertex(topLeftVertex, currentLeaf.minIndex.x, currentLeaf.minIndex.y, IndicesList, VerticesList, ColorsList, NormalsList, terrain);

                    addVertex(bottomLeftVertex, currentLeaf.minIndex.x, currentLeaf.maxIndex.y, IndicesList, VerticesList, ColorsList, NormalsList, terrain);
                    addVertex(bottomRightVertex, currentLeaf.maxIndex.x, currentLeaf.maxIndex.y, IndicesList, VerticesList, ColorsList, NormalsList, terrain);
                    addVertex(topRightVertex, currentLeaf.maxIndex.x, currentLeaf.minIndex.y, IndicesList, VerticesList, ColorsList, NormalsList, terrain);
                }

            }

            //end algorithm



            //store the result into a VertexGeometry. 
            var result = new VertexGeometry()
            {

                Indices = IndicesList.ToArray(),

                Positions = VerticesList.ToArray(),

                Colors = ColorsList.ToArray(),

                Normals = NormalsList.ToArray(),

            };

            return result;
        }

        public float calcLeafError(MyTerrain terrain, MyLeaf leaf)
        {
            //returns the error (= height field difference) between this leaf's corner points and the actual heightfield values
            float result = 0;

            //corner vertices and center vertex
            MyVertex topLeft = getVertex(terrain, leaf.minIndex.x, leaf.minIndex.y);
            MyVertex topRight = getVertex(terrain, leaf.maxIndex.x, leaf.minIndex.y);
            MyVertex bottomLeft = getVertex(terrain, leaf.minIndex.x, leaf.maxIndex.y);
            MyVertex bottomRight = getVertex(terrain, leaf.maxIndex.x, leaf.maxIndex.y);
            MyVertex center = getVertex(terrain, leaf.centerIndex.x, leaf.centerIndex.y);

            //the vertices interpolated in the middle of two edges, and their actual values
            MyVertex topInterp = averageOfTwo(topLeft, topRight);
            MyVertex topActual = getVertex(terrain, leaf.centerIndex.x, leaf.minIndex.y);
            MyVertex rightInterp = averageOfTwo(topRight, bottomRight);
            MyVertex rightActual = getVertex(terrain, leaf.maxIndex.x, leaf.centerIndex.y);
            MyVertex bottomInterp = averageOfTwo(bottomRight, bottomLeft);
            MyVertex bottomActual = getVertex(terrain, leaf.centerIndex.x, leaf.maxIndex.y);
            MyVertex leftInterp = averageOfTwo(bottomLeft, topLeft);
            MyVertex leftActual = getVertex(terrain, leaf.minIndex.x, leaf.centerIndex.y);
            MyVertex centerInterp = averageOfFour(topLeft, topRight, bottomRight, bottomLeft);

            //the height field differences
            float errorTop = Math.Abs(topInterp.z - topActual.z);
            float errorRight = Math.Abs(rightInterp.z - rightActual.z);
            float errorBottom = Math.Abs(bottomInterp.z - bottomActual.z);
            float errorLeft = Math.Abs(leftInterp.z - leftActual.z);
            float errorCenter = Math.Abs(centerInterp.z - center.z);

            //the overall error is the maximum of the individual errors
            result = Math.Max(errorTop, Math.Max(errorRight, Math.Max(errorBottom, Math.Max(errorLeft, errorCenter)))); 

            return result;
        }

        public MyVertex getVertex(MyTerrain terrain, int xIndex, int yIndex)
        {
            return terrain.terr[xIndex, yIndex];
        }

        public void addVertex(MyVertex vert, int xIndex, int yIndex, List<int> IndicesList, List<V3f> VerticesList, List<C4b> ColorsList, List<V3f> NormalsList, MyTerrain terrain)
        {
            //check if this vertex already exists within the vertexList. if yes, append only its index to the index list. 
            //if no, calculate its index and enter the vertex into the vertexList.

            if(vert.listIndex == -1)
            {
                //does not exist yet
                //int index = terrain.size * yIndex + xIndex;

                int index = terrain.runningIndex;

                VerticesList.Add(vert.toV3f());
                ColorsList.Add(vert.color);
                NormalsList.Add(vert.normal.toV3f());

                vert.listIndex = index;

                IndicesList.Add(vert.listIndex);

                terrain.runningIndex = terrain.runningIndex + 1;

                return;
            }
            else
            {
                //already exists
                IndicesList.Add(vert.listIndex);
                return;
            }

        }
    }

    public class FractalTerrain
    {
        public float initialScale { get; set; }

        public float roughness = 1.0f;
        public float flatness = 1.0f;

        public bool drawWater;
        public bool colorize;
        public bool optimizeTerrain;
        public int selectedColorization = 0;
        public float errorThreshold;

        private Random rand = new Random();

        private VertexGeometry vg;

        private MyQuad startVertices;

        private MyTerrainLod terrainLod;

        private List<MyColorization> colorizations;
        private C4b waterColor;

        private int maxlevel;

        public FractalTerrain(float scale, bool drawWater, bool colorize, int colorIndex, float errorThreshold)
        {
            this.drawWater = drawWater;
            this.colorize = colorize;
            initialScale = scale;
            this.errorThreshold = errorThreshold;
            init();
            selectColorization(colorIndex);
            optimizeTerrain = false;
            errorThreshold = 0.0f;
        }

        private void init()
        {
            //base mesh is a single plane
            terrainLod = new MyTerrainLod(errorThreshold);
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

            terrainLod.render(optimizeTerrain,false);
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
