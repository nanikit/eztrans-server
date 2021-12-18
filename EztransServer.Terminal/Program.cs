using EztransServer.Core.Http;
using EztransServer.Core.Translator;
using EztransServer.Terminal.Report;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Net;
using System.Threading.Tasks;

namespace EztransServer.Terminal {
  public class Program {
    private const string _title = "EZTransXP Server Terminal";
    private const string _introduction = "EZTransXP Server Terminal\nCopyright <c> 2021 nanikit and community contributors.\n";
    private const string _defaultOrigin = "http://localhost:8000";

    private static readonly ReporterManager _reporterManager = new();

    private static TranslationServer? _translatorServer;
    private static int _requestCount;

    public static async Task Main(string[] args) {
      Console.Title = _title;
      Console.WriteLine(_introduction);

      if (!InitializeReportManager()) {
        return;
      }

      await (ReadArguments(args)?.Run() ?? Task.CompletedTask).ConfigureAwait(false);
    }

    private static bool InitializeReportManager() {
      try {
        _reporterManager.ReportUpdatedEvent += OnReportUpdated;
        _reporterManager.AddReport("Report manager is initialized.");

        return true;
      }
      catch (Exception e) {
        _reporterManager.AddReport(e.Message + "\r\n" + e.StackTrace, ReportType.EXCEPTION);
        return false;
      }
    }

    private static Program? ReadArguments(string[] args) {
      Program? instance = null;

      try {
        Option pathOption = new Option<string>("--dll-path", getDefaultValue: () => @"C:\Program Files (x86)\ChangShinSoft\ezTrans XP\J2KEngine.dll", description: "The path of EZTransXP dll.");
        pathOption.AddAlias("-p");

        Option originOption = new Option<string>("--origin", getDefaultValue: () => _defaultOrigin, description: "The origin URL which the server is used for watching requests.");
        originOption.AddAlias("-o");

        // Create a root command with some options
        var rootCommand = new RootCommand { pathOption, originOption };
        rootCommand.Description = "Terminal for EZTransXP Server";


        // Note that the parameters of the handler method are matched according to the names of the options
        rootCommand.Handler = CommandHandler.Create<string, string>((dllPath, origin) => {
          _reporterManager.AddReport($"DLL_PATH: '{dllPath}', ORIGIN: '{origin}'");
          instance = new Program(dllPath, origin);
        });

        // Parse the incoming args and invoke the handler
        int result = rootCommand.Invoke(args);

        if (result == 0) {
          _reporterManager.AddReport("Arguments are initialized.");
        }
        else {
          _reporterManager.AddReport("An error occurred while initializing arguments.", ReportType.CAUTION);
        }
      }
      catch (Exception e) {
        _reporterManager.AddReport(e.Message + "\r\n" + e.StackTrace, ReportType.EXCEPTION);
      }

      return instance;
    }

    private static void OnReportUpdated(object sender, ReportUpdatedEventArgs e) {
      Console.WriteLine(e.Text);
    }

    private static void OnRequest(IPEndPoint ip, string? req) {
      string head = req?[..Math.Min(40, req.Length)] ?? "";
      _reporterManager.AddReport($"({++_requestCount})'{ip.Address}' requested '{head}'.");
    }

    public Program(string? dllPath = null, string origin = _defaultOrigin) {
      _dllPath = dllPath;
      _origin = origin;
    }

    private readonly string? _dllPath;
    private readonly string _origin = _defaultOrigin;

    private async Task Run() {
      try {
        if (_translatorServer != null) {
          _translatorServer.Dispose();
        }

        _requestCount = 0;
        _translatorServer = new TranslationServer(new EhndTranslator(_dllPath));
        _translatorServer.OnRequest += OnRequest;

        var uri = new Uri(_origin);
        Task server = _translatorServer.Run(uri);
        _reporterManager.AddReport($"Server is started in '{uri}'");

        await server.ConfigureAwait(false);
        _reporterManager.AddReport($"Server is stopped in '{uri}'");
      }
      catch (HttpListenerException e) {
        int codeAccessDenied = 5;
        if (e.ErrorCode == codeAccessDenied) {
          _reporterManager.AddReport($"Please restart the application with administrator privileges.", ReportType.CAUTION);
        }
        else {
          _reporterManager.AddReport(e.Message + "\r\n" + e.StackTrace, ReportType.EXCEPTION);
        }
      }
      catch (Exception e) {
        _reporterManager.AddReport(e.Message + "\r\n" + e.StackTrace, ReportType.EXCEPTION);
      }
    }
  }
}
