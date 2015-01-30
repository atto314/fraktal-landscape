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

namespace FractalLandscape
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
            Kernel.Start();

            var myApp = new MyApp();

            var toolsWindow = new UserControl1();

            // Initialization method using SlimDx9Renderer
            myApp.init(new SlimDx9Renderer(), toolsWindow);

            toolsWindow.init(myApp);

            // Start app
            myApp.run();

        }
    }
}
