﻿////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.Common
{

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  public class AsyncRedirectProcess : IDisposable
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public interface EventListener
    {
      void ProcessStdout (object sendingProcess, DataReceivedEventArgs args);

      void ProcessStderr (object sendingProcess, DataReceivedEventArgs args);

      void ProcessExited (object sendingProcess, EventArgs args);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected int m_startTicks = 0;

    protected int m_exitCode = 0;

    protected ManualResetEvent m_exitMutex = null;

    protected int m_lastOutputTimestamp = 0;

    protected TextWriter m_stdInputWriter = null;

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public AsyncRedirectProcess (string filename, string arguments, string workingDirectory = null)
    {
      if (string.IsNullOrEmpty (filename))
      {
        throw new ArgumentNullException ("filename");
      }

      if (arguments == null)
      {
        throw new ArgumentNullException ("arguments");
      }

      if (!File.Exists (filename))
      {
        throw new FileNotFoundException ("Could not find target executable.", filename);
      }

      Listener = null;

      StartInfo = CreateDefaultStartInfo ();

      StartInfo.FileName = filename;

      StartInfo.Arguments = arguments;

      StartInfo.WorkingDirectory = workingDirectory ?? Path.GetDirectoryName (filename);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Dispose ()
    {
      LoggingUtils.PrintFunction ();

      try
      {
        Process.Dispose ();
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected static ProcessStartInfo CreateDefaultStartInfo ()
    {
      LoggingUtils.PrintFunction ();

      ProcessStartInfo startInfo = new ProcessStartInfo ();

      startInfo.CreateNoWindow = true;

      startInfo.UseShellExecute = false;

      startInfo.LoadUserProfile = false;

      startInfo.ErrorDialog = false;

      startInfo.RedirectStandardOutput = true;

      startInfo.RedirectStandardError = true;

      startInfo.RedirectStandardInput = true;

      return startInfo;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Start ()
    {
      LoggingUtils.Print (string.Format ("[AsyncRedirectProcess] Start: {0} (Args=\"{1}\" Pwd=\"{2}\")", StartInfo.FileName, StartInfo.Arguments, StartInfo.WorkingDirectory));

      m_startTicks = Environment.TickCount;

      m_lastOutputTimestamp = m_startTicks;

      m_exitMutex = new ManualResetEvent (false);

      Process = new Process ();

      Process.StartInfo = StartInfo;

      Process.OutputDataReceived += new DataReceivedEventHandler (ProcessStdout);

      Process.ErrorDataReceived += new DataReceivedEventHandler (ProcessStderr);

      Process.Exited += new EventHandler (ProcessExited);

      Process.EnableRaisingEvents = true;

      if (!Process.Start ())
      {
        m_exitMutex.Set ();

        throw new InvalidOperationException ("Could not spawn async process - " + Process.StartInfo.FileName);
      }

      Process.BeginOutputReadLine ();

      Process.BeginErrorReadLine ();

      m_stdInputWriter = TextWriter.Synchronized (Process.StandardInput);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Kill ()
    {
      LoggingUtils.PrintFunction ();

      try
      {
        if (!m_exitMutex.WaitOne (0))
        {
          Process.Kill ();
        }
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public virtual void SendCommand (string command)
    {
      LoggingUtils.Print (string.Format ("[AsyncRedirectProcess] SendCommand: {0}", command));

      m_stdInputWriter.WriteLine (command);
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected void ProcessStdout (object sendingProcess, DataReceivedEventArgs args)
    {
      try
      {
        m_lastOutputTimestamp = Environment.TickCount;

        /*if (!string.IsNullOrWhiteSpace (args.Data))
        {
          LoggingUtils.Print (string.Format ("[AsyncRedirectProcess] ProcessStdout: {0}", args.Data));
        }*/
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);
      }
      finally
      {
        if (Listener != null)
        {
          Listener.ProcessStdout (sendingProcess, args);
        }
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected void ProcessStderr (object sendingProcess, DataReceivedEventArgs args)
    {
      try
      {
        m_lastOutputTimestamp = Environment.TickCount;

        /*if (!string.IsNullOrWhiteSpace (args.Data))
        {
          LoggingUtils.Print (string.Format ("[AsyncRedirectProcess] ProcessStdout: {0}", args.Data));
        }*/
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);
      }
      finally
      {
        if (Listener != null)
        {
          Listener.ProcessStderr (sendingProcess, args);
        }
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected void ProcessExited (object sendingProcess, EventArgs args)
    {
      try
      {
        LoggingUtils.Print (string.Format ("[AsyncRedirectProcess] ProcessExited: {0}", args));

        m_exitCode = ((Process) sendingProcess).ExitCode;

        m_exitMutex.Set ();

        LoggingUtils.Print (string.Format ("[AsyncRedirectProcess] exited ({0}) in {1} ms", m_exitCode, Environment.TickCount - m_startTicks));
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);
      }
      finally
      {
        if (Listener != null)
        {
          Listener.ProcessExited (sendingProcess, args);
        }
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public Process Process { get; protected set; }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public ProcessStartInfo StartInfo { get; protected set; }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public EventListener Listener { get; set; }

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
