using EztransServer.Terminal.Utility;
using System;
using System.IO;
using System.Text;

namespace EztransServer.Terminal.Report {
  internal class ReporterManager {
    private static ReporterManager _instance = null;

    internal static ReporterManager Instance {
      get {
        if (_instance == null) {
          _instance = new ReporterManager();
        }

        return _instance;
      }
      set {
        _instance = value;
      }
    }

    internal delegate void ReportUpdatedEventHandler(object sender, ReportUpdatedEventArgs e);
    internal event ReportUpdatedEventHandler ReportUpdatedEvent;

    private StringBuilder _builder = new StringBuilder();

    internal string Report {
      get {
        return _builder.ToString();
      }
    }

    internal void AddReport(string text, ReportType type = ReportType.INFO) {
      string report = $"{DateTime.Now.ToString("HH:mm:ss.fff")} - [{type}] {text}";
      _builder.AppendLine(report);
      ReportUpdatedEvent?.Invoke(this, new ReportUpdatedEventArgs(report));
    }

    internal void ResetReport() {
      _builder.Clear();
    }

    internal void ExportReport(string savePath) {
      string report = _builder.ToString();

      if (!Directory.Exists(Path.GetDirectoryName(savePath))) {
        Directory.CreateDirectory(Path.GetDirectoryName(savePath));
      }

      FileManager.WriteTextFile(savePath, report, Encoding.UTF8);
    }
  }
}
