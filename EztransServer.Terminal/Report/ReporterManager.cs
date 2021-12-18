using EztransServer.Terminal.Utility;
using System;
using System.IO;
using System.Text;

namespace EztransServer.Terminal.Report {
  internal class ReporterManager {
    internal delegate void ReportUpdatedEventHandler(object sender, ReportUpdatedEventArgs e);
    internal event ReportUpdatedEventHandler ReportUpdatedEvent = delegate { };

    internal string Report {
      get {
        return _builder.ToString();
      }
    }

    internal void AddReport(string text, ReportType type = ReportType.INFO) {
      string report = $"{DateTime.Now:HH:mm:ss.fff} - [{type}] {text}";
      _builder.AppendLine(report);
      ReportUpdatedEvent?.Invoke(this, new ReportUpdatedEventArgs(report));
    }

    internal void ResetReport() {
      _builder.Clear();
    }

    internal void ExportReport(string savePath) {
      string report = _builder.ToString();

      string? directory = Path.GetDirectoryName(savePath);
      if (directory != null && !Directory.Exists(directory)) {
        Directory.CreateDirectory(directory);
      }

      FileManager.WriteTextFile(savePath, report, Encoding.UTF8);
    }

    private readonly StringBuilder _builder = new();
  }
}
