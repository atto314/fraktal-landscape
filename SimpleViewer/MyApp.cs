﻿using System;
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
    public class MyApp
    {

        private SimpleRenderApplication app;
        public FractalTerrain myTerrain;
        private UserControl1 toolsWindow;

        public float terrainScale = 75.0f;
        public float terrainRoughness = 9.0f;
        public float terrainFlatness = 1.5f;
        public bool terrainHasWater = true;
        public bool colorizeTerrain = true;
        public bool optimizeTerrain = false;
        public int lodLevel = 8;
        public int colorIndex = 0;
        public float errorThreshold = 75.0f;

        public void init(SlimDx9Renderer Dx9Renderer, UserControl1 toolsWindow)
        {
            this.toolsWindow = toolsWindow;

            app = new SimpleRenderApplication(Dx9Renderer, true);

            app.Init();

            // Default camera
            app.MainViewTrafo.Location.Val = (new V3d(20, 20, 20))*5;
            app.MainViewTrafo.LookAt.Val = V3d.Zero;

            // Background color
            app.BackgroundColor = new C4f(0.7, 0.8, 0.9);

            // Default camera controller
            app.CameraController.MouseCursorHidingEnabled = true;
            app.CameraController.MouseCursorStopsAtScreenBorder = false;

            // Set up triggers
            initTriggers();

            // Set up scene
            //initScene();

            // Set up the toolbox WPF window
            toolsWindow.Left = 0;
            toolsWindow.Top = 0;
            toolsWindow.ResizeMode = System.Windows.ResizeMode.NoResize;
            toolsWindow.Show();

            // Set flags
            app.GlobalCullMode = CullMode.Clockwise;
            app.HeadLightEnabled = true;
            app.OverlayFpsEnabled = true;
            app.OverlayVRVisInfoEnabled = true;
            app.ShowGlobalBoundingBox = false;
            app.ShowGlobalAxes = true;

            app.GlobalFillMode = FillMode.Solid;
            app.Renderer.AntiAliasingMode = AntiAliasingMode.FourSamples;
            //app.CenterScene();
        }

        public void generateNewScene()
        {
            initScene();
        }

        public void rasterizeWireframe(bool state)
        {
            if(!state)
            {
                this.app.GlobalFillMode = FillMode.Solid;
            }
            else
            {
                this.app.GlobalFillMode = FillMode.Wireframe;
            }
        }

        public void enableShading(bool state)
        {
            if (state)
            {
                this.app.HeadLightEnabled = true;
            }
            else
            {
                this.app.HeadLightEnabled = false;
            }
        }

        public void run()
        {
            app.Show();
            app.Run();
        }

        public void shutdown()
        {
            app.Hide();
            myTerrain = null;
            app = null;
            Kernel.Stop();
            Kernel.KillProcess();
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

        public void centerScene()
        {
            if(myTerrain==null)
            {
                return;
            }
            app.CenterScene();
        }

        public void saveScreenshot()
        {
            app.SaveScreenshotToDesktop();
        }

        public void setOptimizeTerrain(bool state)
        {
            this.optimizeTerrain = state;
            if(myTerrain==null)
            {
                return;
            }
            myTerrain.optimizeTerrain = this.optimizeTerrain;
        }

        private void initScene()
        {
            myTerrain = new FractalTerrain(terrainScale, terrainHasWater, colorizeTerrain, colorIndex, this.errorThreshold);
            myTerrain.optimizeTerrain = this.optimizeTerrain;

            var group = Sg.Group();

            //generate a terrain up to a certain lod level, then put the result into the scene graph
            float currentRoughness = 1.0f / (terrainRoughness / 10.0f);
            myTerrain.buildTerrain(lodLevel, currentRoughness, terrainFlatness);

            var vgs = myTerrain.toVertexGeometrySet(lodLevel);

            group.Add(vgs);

            //add shading model/description here

            app.SemanticSceneGraph = group;
        }
    }
}
