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
    class MyApp
    {

        private SimpleRenderApplication app;
        private FractalTerrain myTerrain;

        public void init(SlimDx9Renderer Dx9Renderer)
        {

            app = new SimpleRenderApplication(Dx9Renderer, true);

            app.Init();

            // Default camera
            app.MainViewTrafo.Location.Val = new V3d(20, 20, 20);
            app.MainViewTrafo.LookAt.Val = V3d.Zero;

            // Background color
            app.BackgroundColor = new C4f(0.7, 0.8, 0.9);

            // Default camera controller
            app.CameraController.MouseCursorHidingEnabled = true;
            app.CameraController.MouseCursorStopsAtScreenBorder = false;

            // Set up triggers
            initTriggers();

            // Set up scene
            initScene();

            // Set flags
            app.GlobalCullMode = CullMode.Clockwise;
            app.HeadLightEnabled = true;
            app.OverlayFpsEnabled = true;
            app.OverlayVRVisInfoEnabled = true;
            app.ShowGlobalBoundingBox = false;
            app.ShowGlobalAxes = true;

            app.GlobalFillMode = FillMode.Wireframe;
            //app.CenterScene();
        }

        public void run()
        {
            app.Show();
            app.Run();
        }

        private void initTriggers()
        {
            // Switch between fly and explore camera controllers
            app.CameraController.AddTrigger(
                new Trigger(
                    "<SPACE> toggles fly/explore navigation style",
                    state => !state.IsKeyPressed(Keys.Alt) && state.IsKeyPressed(Keys.Space),
                    state =>
                    {
                        state.ClearKey(Keys.Space);
                        switch (app.NavigationStyle)
                        {
                            case ControllableRenderTask.Style.Explore:
                                app.NavigationStyle = ControllableRenderTask.Style.Fly;
                                Console.WriteLine("switched to fly mode");
                                break;
                            case ControllableRenderTask.Style.Fly:
                                app.NavigationStyle = ControllableRenderTask.Style.Explore;
                                Console.WriteLine("switched to explore mode");
                                break;
                        }

                    })
                );

            // Center scene
            app.CameraController.AddTrigger(
                new Trigger(
                    "<Alt>-<C> centers scene",
                    state => state.IsKeyPressed(Keys.Alt) && state.IsKeyPressed(Keys.C),
                    state =>
                    {
                        state.ClearKey(Keys.C);
                        app.CenterScene();
                    })
                );

        }

        private void initScene()
        {
            float terrainScale = 75.0f;
            float terrainRoughness = 9.0f;
            float terrainFlatness = 1.5f;
            bool terrainHasWater = true;
            bool colorizeTerrain = true;
            int lodLevel = 8;

            myTerrain = new FractalTerrain(terrainScale, terrainHasWater, colorizeTerrain);

            var group = Sg.Group();

            //generate a terrain up to a certain lod level, then put the result into the scene graph
            float currentRoughness = 1.0f / (terrainRoughness / 10.0f);
            myTerrain.buildTerrain(lodLevel, currentRoughness, terrainFlatness); 

            group.Add(myTerrain.toVertexGeometrySet(lodLevel));

            app.SemanticSceneGraph = group;
        }
    }
}
