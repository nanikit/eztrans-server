using System;
using System.Net;
using System.Windows;

namespace eztrans_server {
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window {
    EztransHttpServer HttpServer = new EztransHttpServer();

    public MainWindow() {
      InitializeComponent();

      HttpServer.OnRequest += OnRequest;
      HttpServer.Run();
    }

    private void OnRequest(IPEndPoint ip, string req) {
      string datetime = $"{DateTime.Now:[yyyy-MM-dd HH:mm]}";
      //string log = $"\n{datetime} ${ip}: ${req?.Substring(0, 100) ?? ""}";
      string log = $"\n ${datetime}";
      Action act = () => {
        TbLog.AppendText(log);
      };
      Dispatcher.InvokeAsync(act);
    }
  }
}
