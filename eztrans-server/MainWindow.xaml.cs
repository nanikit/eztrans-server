#nullable enable
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace eztrans_server {
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window {
    public MainWindow() {
      var vm = new HttpServerVM();
      PrintProgramVersion(vm);
      _ = vm.Restart();

      DataContext = vm;
      InitializeComponent();
    }

    private void LogScrollDown(object sender, TextChangedEventArgs e) {
      double bottom = TbLog.VerticalOffset + TbLog.ViewportHeight;
      bool isBottommost = bottom >= TbLog.ExtentHeight - 10;
      if (isBottommost) {
        TbLog.ScrollToEnd();
      }
    }

    private static void PrintProgramVersion(HttpServerVM vm) {
      string build = Properties.Resources.BuildDate;
      string date = $"{build.Substring(2, 2)}{build.Substring(5, 2)}{build.Substring(8, 2)}";
      vm.Logs.Add($"eztrans-server v{date} by nanikit");
    }
  }

  class HttpServerVM : ViewModelBase {
    public ObservableCollection<string> Logs { get; private set; }

    public RelayCommand RestartCommand { get; private set; }

    private string _Log = "";
    public string Log {
      get => _Log;
      set => Set(ref _Log, value);
    }

    private string _Origin = "http://localhost:8000/";
    public string Origin {
      get => _Origin;
      set => Set(ref _Origin, value);
    }

    private string _Title = "Eztrans server";
    public string Title {
      get => _Title;
      set => Set(ref _Title, value);
    }

    private TranslationHttpServer? HttpServer;
    private int RequestCount = 0;

    public HttpServerVM() {
      RestartCommand = new RelayCommand(() => _ = Restart());
      Logs = new ObservableCollection<string>();
      Logs.CollectionChanged += MergeLogs;

      SetOriginFromCommandLine();
    }

    public async Task Restart() {
      try {
        if (HttpServer != null) {
          HttpServer.Dispose();
        }

        RequestCount = 0;
        IJp2KrTranslator translator = await EztransXp.Create().ConfigureAwait(false);
        translator = new BatchTranslator(translator);
        HttpServer = new TranslationHttpServer(translator);
        HttpServer.OnRequest += OnRequest;

        var uri = new Uri(Origin);
        Task server = HttpServer.Run(uri);
        Logs.Add($"서버를 시작했습니다: {uri}");
        await server.ConfigureAwait(false);
        Logs.Add($"서버를 종료했습니다: {uri}");
      }
      catch (HttpListenerException e) {
        int codeAccessDenied = 5;
        if (e.ErrorCode == codeAccessDenied) {
          Logs.Add("외부와 연결하기 위해 관리자 권한으로 재실행합니다.");
          RunSelfAsAdmin();
        }
        else {
          Logs.Add(e.Message);
        }
      }
      catch (Exception e) {
        Logs.Add(e.Message);
      }
    }

    private void SetOriginFromCommandLine() {
      string[] args = Environment.GetCommandLineArgs();
      if (args.Length >= 2) {
        LogIfThrown(() => {
          var uri = new Uri(args[1]);
          Origin = uri.AbsoluteUri;
        });
      }
    }

    private void RunSelfAsAdmin() {
      LogIfThrown(() => {
        Process p = new Process();
        p.StartInfo.Verb = "runas";
        p.StartInfo.UseShellExecute = true;
        p.StartInfo.FileName = System.Reflection.Assembly.GetEntryAssembly().Location;
        p.StartInfo.Arguments = Origin;
        p.Start();

        Application.Current.Dispatcher.InvokeAsync(() => {
          Application.Current.Shutdown();
        });
      });
    }

    private void LogIfThrown(Action action) {
      try {
        action();
      }
      catch (Exception e) {
        Logs.Add(e.Message);
      }
    }

    private void OnRequest(IPEndPoint ip, string? req) {
      string datetime = $"{DateTime.Now:[MM-dd HH:mm:ss]}";
      string head = req?.Substring(0, Math.Min(40, req.Length)) ?? "";
      string log = $"{datetime} {ip.Address}: {head}";

      if (Logs.Count > 1000) {
        Logs.RemoveAt(0);
      }
      Logs.Add(log);
      Title = $"요청 수: {++RequestCount}";
    }

    private void MergeLogs(object sender, NotifyCollectionChangedEventArgs e) {
      Log = string.Join("\n", Logs);
    }
  }
}

