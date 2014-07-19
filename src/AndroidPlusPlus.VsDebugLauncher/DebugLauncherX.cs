﻿////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using System.Xml;

using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.VCProjectEngine;
using Microsoft.VisualStudio.Project.Contracts.VS2010ONLY;
using Microsoft.VisualStudio.Project.Framework;
using Microsoft.VisualStudio.Project.Utilities.DebuggerProviders;

using AndroidPlusPlus.Common;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.VsDebugLauncher
{

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  [DebuggerScope("AndroidPlusPlusDebugger")]

  [ProjectScope(ProjectScopeRequired.ConfiguredProject)]

  [Export(typeof(IDebugLaunchProvider))]

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  public class DebugLauncherX : IDebugLaunchProvider
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    // 
    // Gets the set of debuggers already in the Visual C++ project system.
    // This allows us to find the debugger that we intend to wrap.
    // 

    [ImportMany]
    public List<Lazy<IDebugLaunchProvider, IDictionary<string, object>>> DebugProviders { get; set; }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    // 
    // Gets the service provider used by the top-level Visual C++ project engine.
    // 

    [Import]
    public IServiceProvider ServiceProvider { get; set; }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public bool CanLaunch (DebugLaunchOptions launchOptions, IDictionary <string, string> projectProperties)
    {
      LoggingUtils.PrintFunction ();

      return DebugLauncher.CanLaunch ((int) launchOptions);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public IEnumerable<IDebugLaunchSettings> PrepareLaunch (DebugLaunchOptions launchOptions, IDictionary<string, string> projectProperties)
    {
      LoggingUtils.PrintFunction ();

#if false
      // 
      // Print verbose details of the project being launched.
      // 

      foreach (KeyValuePair <string, string> projectPropsPair in projectProperties)
      {
        LoggingUtils.Print ("[DebugLauncher] Project property: '" + projectPropsPair.Key + "': '" + projectPropsPair.Value + "'");
      }
#endif

      try
      {
        DebugLaunchSettings debugLaunchSettings = new DebugLaunchSettings (launchOptions);

        Project startupProject = DebugLauncher.GetStartupSolutionProject (ServiceProvider, (Dictionary <string, string>) projectProperties);

        if (startupProject == null)
        {
          throw new InvalidOperationException ("Could not find solution startup project.");
        }

        LoggingUtils.Print ("Launcher startup project: " + startupProject.Name + " (" + startupProject.FullName + ")");

        if (launchOptions.HasFlag (DebugLaunchOptions.NoDebug))
        {
          debugLaunchSettings = (DebugLaunchSettings) DebugLauncher.StartWithoutDebugging ((int)launchOptions, (Dictionary<string, string>) projectProperties, startupProject);
        }
        else
        {
          debugLaunchSettings = (DebugLaunchSettings) DebugLauncher.StartWithDebugging ((int)launchOptions, (Dictionary<string, string>) projectProperties, startupProject);
        }

        return new IDebugLaunchSettings [] { debugLaunchSettings };
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        DebugLauncher.ShowErrorDialog (ServiceProvider, string.Format ("Debugging failed to launch, encountered exception:\n\n {0}", e.Message));
      }

      return null;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  }

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
