#nullable enable
using EztransServer.Core.Http;
using EztransServer.Core.Translator;
using Nanikit.Ehnd;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;

namespace EztransServer.Gui {
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window {
    public MainWindow() {
      var vm = new HttpServerVM();
      DataContext = vm;
      InitializeComponent();

      PrintProgramVersion();
      vm.Logs.CollectionChanged += LogVmItem;
      _ = vm.Restart();
    }

    private void LogVmItem(object? sender, NotifyCollectionChangedEventArgs e) {
      if (e.Action == NotifyCollectionChangedAction.Add) {
        Log(e.NewItems?[0] as string ?? "");
      }
    }

    private void Log(string s) => Dispatcher.InvokeAsync(() => {
      BlockCollection blocks = TbLog.Document.Blocks;
      if (blocks.Count >= 1000) {
        blocks.Remove(blocks.FirstBlock);
      }
      var paragraph = new Paragraph(new Run(s));
      blocks.Add(paragraph);
    });

    private void LogScrollDown(object sender, TextChangedEventArgs e) {
      double bottom = TbLog.VerticalOffset + TbLog.ViewportHeight;
      bool isBottommost = bottom >= TbLog.ExtentHeight - 10;
      if (isBottommost) {
        TbLog.ScrollToEnd();
      }
    }

    private void PrintProgramVersion() {
      string build = Properties.Resources.BuildDate;
      string date = $"{build.Substring(2, 2)}{build.Substring(5, 2)}{build.Substring(8, 2)}";
      TbLog.Document.Blocks.Clear();
      Log($"eztrans-server v{date}");
    }
  }

  class HttpServerVM : ViewModelBase {
    public ObservableCollection<string> Logs { get; private set; } = new ObservableCollection<string>();

    public RelayCommand RestartCommand { get; private set; }

    private string _origin = "http://localhost:8000/";
    public string Origin {
      get => _origin;
      set => Set(ref _origin, value);
    }

    private string _title = "Eztrans server";
    public string Title {
      get => _title;
      set => Set(ref _title, value);
    }

    private int _requestCount = 0;
    public int RequestCount {
      get => _requestCount;
      set => Set(ref _requestCount, value);
    }

    private TranslationServer? _server;

    public HttpServerVM() {
      RestartCommand = new RelayCommand(() => _ = Restart());
      SetOriginFromCommandLine();
    }

    public async Task Restart() {
      try {
        if (_server != null) {
          _server.Dispose();
        }

        RequestCount = 0;
        var translator = new EhndTranslator();
        _server = new TranslationServer(translator);
        _server.OnRequest += OnRequest;
        _server.OnException += OnException;

        var uri = new Uri(_origin);
        Task server = Task.Run(() => _server.Run(uri));
        Logs.Add($"서버를 시작했습니다: {uri}");

        await server.ConfigureAwait(false);
        Logs.Add("서버를 종료했습니다.");
      }
      catch (HttpListenerException exception) {
        int codeAccessDenied = 5;
        if (exception.ErrorCode == codeAccessDenied) {
          Logs.Add("외부와 연결하기 위해 관리자 권한으로 재실행합니다.");
          RunSelfAsAdmin();
        }
        else {
          Logs.Add($"{exception}");
        }
      }
      catch (EhndNotFoundException exception) {
        Logs.Add($"{exception.Message}\r\n{exception.Details}");
      }
      catch (Exception exception) {
        Logs.Add($"FATAL: {exception.Message}\r\n{exception.StackTrace}");
      }
    }

    private void OnException(string text, Exception exception) {
      Logs.Add($"HTTP 500 [{text}]: \r\n{exception}");
    }

    private void SetOriginFromCommandLine() {
      string[] args = Environment.GetCommandLineArgs();
      if (args.Length >= 2) {
        LogIfThrow(() => {
          var uri = new Uri(args[1]);
          Origin = uri.AbsoluteUri;
        });
      }
    }

    private void RunSelfAsAdmin() {
      LogIfThrow(() => {
        Process p = new();
        p.StartInfo.Verb = "runas";
        p.StartInfo.UseShellExecute = true;
        p.StartInfo.FileName = Environment.ProcessPath;
        p.StartInfo.Arguments = Origin;
        p.Start();

        Application.Current.Dispatcher.InvokeAsync(() => {
          Application.Current.Shutdown();
        });
      });
    }

    private void LogIfThrow(Action action) {
      try {
        action();
      }
      catch (Exception e) {
        Logs.Add(e.Message);
      }
    }

    private void OnRequest(IPEndPoint ip, string? req) {
      string datetime = $"{DateTime.Now:[MM-dd HH:mm:ss]}";
      string head = req?[..Math.Min(40, req.Length)] ?? "";
      string log = $"{datetime} {ip.Address}: {head}";

      Application.Current.Dispatcher.InvokeAsync(() => {
        if (Logs.Count > 1000) {
          Logs.RemoveAt(0);
        }
        Logs.Add(log);
        Title = $"요청 수: {++RequestCount}";
      });
    }
  }
}

