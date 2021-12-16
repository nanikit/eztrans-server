using EztransServer.Core;
using EztransServer.Core.Http;
using EztransServer.Core.Translator;
using EztransServer.Terminal.Report;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace EztransServer.Terminal {
  class Program {
    private static string _title = "EZTransXP Server Terminal";
    private static string _intoduction = "EZTransXP Server Terminal\r\nCopyright <c> 2021 nanikit and community contributors.\r\n";

    private static ReporterManager _reporterManager;

    private static string _eztransPath;
    private static string _origin;
    private static int _loadDelay;

    private static TranslatorServerProvider? _translatorServer;
    private static int _requestCount;

    static async Task Main(string[] args) {
      Console.Title = _title;
      Console.WriteLine(_intoduction);

      bool isCompatible = Compatibility.IsCompatible();

      if (isCompatible) {
        if (InitializeReportManager() && InitializeArguments(args)) {
          Compatibility.RegisterCodePages();
          await InitializeServer();
        }
      }
      else {
        Console.WriteLine("Application started on an unsupported platform. This application only supports Windows.");
      }
    }

    private static bool InitializeReportManager() {
      try {
        _reporterManager = new ReporterManager();
        _reporterManager.ReportUpdatedEvent += OnReportUpdated;
        _reporterManager.AddReport("Report manager is initialized.");

        return true;
      }
      catch (Exception e) {
        _reporterManager.AddReport(e.Message + "\r\n" + e.StackTrace, ReportType.EXCEPTION);
        return false;
      }
    }

    private static bool InitializeArguments(string[] args) {
      try {
        Option pathOption = new Option<string>("--eztrans-path", getDefaultValue: () => @"C:\Program Files (x86)\ChangShinSoft\ezTrans XP", description: "The path of EZTransXP.");
        pathOption.AddAlias("-p");

        Option originOption = new Option<string>("--origin", getDefaultValue: () => "http://localhost:8000", description: "The origin URL which the server is used for watching requests.");
        originOption.AddAlias("-o");

        Option loadDelayOption = new Option<int>("--load-delay", getDefaultValue: () => 200, description: "The delay for ignoring timeout.");
        loadDelayOption.AddAlias("-d");

        // Create a root command with some options
        var rootCommand = new RootCommand { pathOption, originOption, loadDelayOption };
        rootCommand.Description = "Terminal for EZTransXP Server";

        // Note that the parameters of the handler method are matched according to the names of the options
        rootCommand.Handler = CommandHandler.Create<string, string, int>((eztransPath, origin, loadDelay) => {
          _eztransPath = eztransPath;
          _origin = origin;
          _loadDelay = loadDelay;
        });

        // Parse the incoming args and invoke the handler
        int result = rootCommand.InvokeAsync(args).Result;

        if (result == 0) {
          _reporterManager.AddReport($"EZTRANS_PATH: '{_eztransPath}', ORIGIN: '{_origin}', LOAD_DELAY: {_loadDelay}");
          _reporterManager.AddReport("Arguments are initialized.");
          return true;
        }
        else {
          _reporterManager.AddReport("An error occurred while initializing arguments.", ReportType.CAUTION);
          return false;
        }
      }
      catch (Exception e) {
        _reporterManager.AddReport(e.Message + "\r\n" + e.StackTrace, ReportType.EXCEPTION);
        return false;
      }
    }

    private static async Task InitializeServer() {
      try {
        if (_translatorServer != null) {
          _translatorServer.Dispose();
        }

        _requestCount = 0;
        ITranslator translator = await EztransXpTranslator.Create(eztPath: _eztransPath, msDelay: _loadDelay).ConfigureAwait(false);
        translator = new BatchTranslator(translator);
        _translatorServer = new TranslatorServerProvider(translator);
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

    private static void OnReportUpdated(object sender, ReportUpdatedEventArgs e) {
      Console.WriteLine(e.Text);
    }

    private static void OnRequest(IPEndPoint ip, string? req) {
      string head = req?.Substring(0, Math.Min(40, req.Length)) ?? "";
      _reporterManager.AddReport($"({++_requestCount})'{ip.Address}' requested '{head}'.");
    }
  }
}
