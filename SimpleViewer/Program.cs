//#define COMBINED_VG
//#define ORTHO_CAM
using System;
using System.Collections.Generic;
using Aardvark.Algodat;
using Aardvark.Math;
using Aardvark.Rendering;
using Aardvark.Rendering.SlimDx;
using Aardvark.Runtime;
using Aardvark.SceneGraph;
using Keys = System.Windows.Forms.Keys;
using Aardvark.State;

namespace SimpleViewer
{
    #region EntryPoint
    /// <summary>
    /// Workaround class to call Aardvark.Bootstrapper.Init() 
    /// before any other assembly needs to be loaded.
    /// </summary>
    static class EntryPoint
    {
        [STAThread]
        static void Main(string[] args)
        {
            Aardvark.Bootstrapper.Init();
            Program.Start(args);
        }
    }
    #endregion

    /*
     * First steps information can be found in the Aardvark Wiki:
     * 
     * http://redmine.vrvis.lan/wiki/aardvark2008
     * 
     */
    class Program
    {
        [STAThread]
        public static void Start(string[] args)
        {
            //Report.Verbosity = 999;

            // The first thing to do is starting the Aardvark kernel.
            // Doing this ensures that everything is initialized properly.
            Kernel.Start();

            // The Aardvark.Rendering library knows nothing about concrete
            // rendering APIs like DirectX or OpenGL. In order to create a
            // binding to a concrete API we have to create a renderer, e.g.
            var renderer = new SlimDx9Renderer();

            // The fastest way to get up and running is to use the
            // SimpleRenderApplication class which hides all the low-level
            // plumbing and is simple to use.
            var app = new SimpleRenderApplication(renderer, true);

            //var myForm = (SlimDx9RenderForm)app.RenderTarget;
            //myForm.IsFullscreen = true;

            // Lets handbuild a quad with a texture

            Texture texture;
            try
            {
                // WorkDir.FindFile recursively searches the directory specified
                // in the environment variable AARDVARK_WORKDIR
                texture = new Texture(Convertible.FromFile(
                                          WorkDir.FindFile("Aardvark.bmp")));
            }
            catch
            {
                texture = new Texture(Aardvark.Rendering.Resources
                                        .Logos.AardvarkFps.Convertible());
            }

            // since a vertex geometry may contain multiple texture coordinates,
            // they need to be put into a map.
            var coords = new CoordinatesMap();
            coords[VertexGeometry.Property.DiffuseColorCoordinates] =
                new[] { V2f.OO, V2f.OI, V2f.II, V2f.IO };

            // ditto for textures
            var textures = new TexturesMap();
            textures[VertexGeometry.Property.DiffuseColorTexture] =
                    texture;

            var vg = new VertexGeometry()
            {
                // three indices per triangle
                Indices = new[] { 0, 1, 2, 0, 2, 3 },

                // vertex positions
                Positions = new[] { new V3f(8,0,2), new V3f(12,0,2),
                                    new V3f(12,4,2), new V3f(8,4,2) },

                // vertex colors
                Colors = new[] { C4b.White, C4b.Cyan, C4b.Magenta, C4b.Yellow },

                // optionally: Normals = new[] { new V3f(....) ..... },

                // finally textures and coordinates
                Coordinates = coords,
                Textures = textures,
            };

            // in order to put the vertex geomtry into a scene graph it needs to be wrapped
            // one such wrapper can contain a whole list of vertex geometries
            var vgs = new Sg.VertexGeometrySet() { VertexGeometryList = vg.IntoList() };

            // Lets create some colored boxes.

            var boxesGroup = new Group(
                Primitives.Box(Box3d.FromMinAndSize(new V3d(-1, -2, 0), new V3d(15, 10, 1)), C4b.Green).ToVertexGeometrySet(),
                Primitives.Box(Box3d.FromMinAndSize(new V3d(2, 1, 1), new V3d(3, 4, 2)), C4b.Yellow).ToVertexGeometrySet(),
                Primitives.Box(Box3d.FromMinAndSize(new V3d(2, 1, 3), new V3d(1, 1, 3)), C4b.White).ToVertexGeometrySet());

            var primitives = new Sg.VertexGeometrySet()
            {
                VertexGeometryList = new List<VertexGeometry>() 
                {
                    Primitives.Sphere(Sphere3d.FromCenterAndRadius(new V3d(20, 0, 0), 2.0), 16, C4b.Red, true),
                    Primitives.Cylinder(new V3d(25, 5, 0), V3d.ZAxis, 6.0, 1.5, 12, C4b.Blue, true),
                    Primitives.Cone(new V3d(30, 0, 0), V3d.ZAxis, 5.0, 3.0, 128, C4b.White, true),
                },
            };

            ISg sg = args.Length >= 1 ? Load.As<ISg>(args[0]) : EmptyLeaf.Singleton;

            var sgOn = new Group(sg, boxesGroup, primitives);
            var sgOff = new Group(vgs);

            // A switchable scene graph that can be triggerd from the user interface.
            // Search for the trigger name "mychoice" to find the corresponding key-
            // board binding.
            var semanticSg = new MySg.EnvSelector()
            {
                InitialChoice = 0,
                SceneGraphArray = new ISg[] { sgOn, sgOff },
                EnvName = "mychoice",
            };

#if ORTHO_CAM
            var initViewTrafo = new ControllableViewTrafo();
                initViewTrafo.Location.Val = new V3d(5, 5, 5);
                initViewTrafo.LookAt.Val = new V3d(0, 0, 0);
                initViewTrafo.Sky.Val = V3d.ZAxis;

            app.MainCameraInstance = new Sg.Camera()
            {
                Name = RenderTask.Property.MainCamera,
                NearDistance = 0,
                FarDistance = 100,
                FocusPlaneDistance = 0.0,
                ViewWidth = 20,
                ViewTrafoProvider = new Sg.ViewTrafo()
                {
                    InitialViewTrafo =
                        initViewTrafo,
                    Name = RenderTask.Property.MainViewTrafo,
                },
                //SceneGraph = sg,
            };
#endif

#if COMBINED_VG
            var vg1 = Primitives.Box(Box3d.FromMinAndSize(new V3d(-1, -2, 0), new V3d(15, 10, 1)), new C4b(255, 0, 0, 130));
            var vg2 = Primitives.Box(Box3d.FromMinAndSize(new V3d(2, 1, 1), new V3d(3, 4, 2)), new C4b(255, 255, 255, 130));
            var vg3 = Primitives.Box(Box3d.FromMinAndSize(new V3d(2, 1, 3), new V3d(1, 1, 3)), new C4b(0, 255, 0, 130));

            var textures1 = new TexturesMap();
            textures1[VertexGeometry.Property.DiffuseColorTexture] = new Texture(Aardvark.Rendering.Resources.Logos.VRVis.Convertible());

            var textures2 = new TexturesMap();
            textures2[VertexGeometry.Property.DiffuseColorTexture] = new Texture(Aardvark.Rendering.Resources.Logos.Aardvark.Convertible());

            vg1.Textures = textures1;
            vg2.Textures = textures2;
            vg3.Textures = textures1;

            var vgs = new[] { vg1, vg2, vg3 };
            var combinedVg = vgs.CombineVertexGeometries();
            var bspCombinedVg = combinedVg.BspSortableVertexGeometry(0.001);

            semanticSg = new Group(
                semanticSg,
                combinedVg.ToVertexGeometrySet().FastTrafo(Trafo3d.Translation(-20, 0, 0)),
                bspCombinedVg.ToVertexGeometrySet().FastTrafo(Trafo3d.Translation(-20, -20, 0)));
#endif

            // Finally, we assign a scene graph to the app's SceneGraph property.
            app.SemanticSceneGraph = Rsg.Apply(
                Rsg.Attribute.Renderer.BlendMode(new BlendMode() { Enabled = true }),
                semanticSg);

            app.Init();

            //app.SemanticSceneGraph = Primitives.Box(
            //    Box3d.FromMinAndSize(new V3d(0, 0, 0), new V3d(10, 5, 5)), C4b.Blue).RotateZInDegrees(20);

            // Optionally, we can move our camera.
            var boundingBox = sg.GetBoundingBox3d(app.RootState);

            app.MainViewTrafo.Location.Val = new V3d(20, 20, 20);
            app.MainViewTrafo.LookAt.Val = V3d.Zero;

            // Optionally, we can set the background color.
            app.BackgroundColor = new C4f(0.7, 1.0, 0.0);

            // The simple render application automatically creates a
            // standard camera controller - you can use the mouse to
            // navigate through your scene.
            app.CameraController.MouseCursorHidingEnabled = true;
            app.CameraController.MouseCursorStopsAtScreenBorder = false;

            // We can also add a controller for a XBox gamepad.
            app.AddGamePadVirtualEarthController();

            // It is also possible to define arbitrary triggers,
            // e.g. to define custom keyboard bindings.
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

            // add a trigger to switch between scene graphs
            app.CameraController.AddTrigger(
                new Trigger(
                    "<T> trigger",
                    state => !state.IsKeyPressed(Keys.Alt) && state.IsKeyPressed(Keys.T),
                    state =>
                    {
                        state.ClearKey(Keys.T);
                        int choice = app.EnvironmentMap.Get<int>("mychoice", 0);
                        choice = 1 - choice;
                        app.EnvironmentMap["mychoice"] = choice;
                    })
                );

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

            //app.CameraController.AddTrigger(
            //    new Trigger(
            //        "<K> toggle fps",
            //        state => state.IsKeyPressed(Keys.K),
            //        state =>
            //        {
            //            state.ClearKey(Keys.K);
            //            Rendering.Events.FpsOverlayEnabled.CreateToggle();
            //        })
            //    );

            //Kernel.CQ.EnqueuePeriodic(cq =>
            //{
            //    Report.Line("{0}: {1}", Rendering.Events.FpsOverlayEnabled.Name, Kernel.EventTable[Rendering.Events.FpsOverlayEnabled].IsOn);
            //}, 1.0);

            // Finally, Run() starts the simple render application.
            app.GlobalCullMode = CullMode.Clockwise;
            app.HeadLightEnabled = true;
            app.OverlayFpsEnabled = true;
            app.OverlayVRVisInfoEnabled = true;
            app.ShowGlobalBoundingBox = false;
            app.ShowGlobalAxes = true;
            app.Show();
            app.Run();

        }
    }
}
