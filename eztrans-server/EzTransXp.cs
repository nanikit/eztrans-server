﻿#nullable enable
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace eztrans_server {

  /// <summary>
  /// It can translate japanese to korean.
  /// </summary>
  public interface IJp2KrTranslator : IDisposable {
    /// <summary>
    /// Translate japanese string to korean.
    /// </summary>
    /// <param name="source">Japanese string</param>
    /// <returns>Korean string</returns>
    Task<string?> Translate(string source);
  }

  public class EzTransNotFoundException : ApplicationException {
    public override string Message => "이지트랜스를 찾지 못했습니다.";
  }

  public class EztransXp : IJp2KrTranslator {

    public static async Task<EztransXp> Create(string? eztPath = null, int msDelay = 200) {
      if (string.IsNullOrWhiteSpace(eztPath)) {
        eztPath = GetEztransDirFromReg();
      }
      if (eztPath == null || !File.Exists(GetDllPath(eztPath))) {
        throw new EzTransNotFoundException();
      }
      IntPtr eztransDll = await LoadNativeDll(eztPath, msDelay);
      return new EztransXp(eztransDll);
    }

    public static string? GetEztransDirFromReg() {
      RegistryKey key = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32);
      return key.OpenSubKey(@"Software\ChangShin\ezTrans")?.GetValue(@"FilePath") as string;
    }

    private static async Task<IntPtr> LoadNativeDll(string eztPath, int msDelay) {
      IntPtr EzTransDll = LoadLibrary(GetDllPath(eztPath));
      if (EzTransDll == IntPtr.Zero) {
        int errorCode = Marshal.GetLastWin32Error();
        throw new Exception($"라이브러리 로드 실패(에러 코드: {errorCode})");
      }

      await Task.Delay(msDelay).ConfigureAwait(false);
      string key = Path.Combine(eztPath, "Dat");
      var initEx = GetFuncAddress<J2K_InitializeEx>(EzTransDll, "J2K_InitializeEx");
      if (!initEx("CSUSER123455", key)) {
        throw new Exception("엔진 초기화에 실패했습니다.");
      }

      return EzTransDll;
    }

    private static string GetDllPath(string eztPath) {
      return Path.Combine(eztPath, "J2KEngine.dll");
    }

    private static T GetFuncAddress<T>(IntPtr dll, string name) {
      IntPtr addr = GetProcAddress(dll, name);
      if (addr == IntPtr.Zero) {
        throw new Exception("Ehnd 파일이 아닙니다.");
      }
      return Marshal.GetDelegateForFunctionPointer<T>(addr);
    }


    private readonly J2K_FreeMem J2kFree;
    private readonly J2K_TranslateMMNTW J2kMmntw;

    public Task<string?> Translate(string jpStr) {
      return Task.FromResult(TranslateInternal(jpStr));
    }

    public async Task<bool> IsHdorEnabled() {
      string? chk = await Translate("蜜ドル辞典").ConfigureAwait(false);
      return chk?.Contains("OK") ?? false;
    }

    public void Dispose() {
      // 원래 FreeLibrary를 호출하려 했는데 그러면 Access violation이 뜬다.
    }

    private EztransXp(IntPtr eztransDll) {
      J2kMmntw = GetFuncAddress<J2K_TranslateMMNTW>(eztransDll, "J2K_TranslateMMNTW");
      J2kFree = GetFuncAddress<J2K_FreeMem>(eztransDll, "J2K_FreeMem");
    }

    private string? TranslateInternal(string jpStr) {
      var escaper = new EzTransEscaper();
      string escaped = escaper.Escape(jpStr);
      IntPtr p = J2kMmntw(0, escaped);
      if (p == IntPtr.Zero) {
        return null;
      }
      string ret = Marshal.PtrToStringAuto(p);
      J2kFree(p);
      string? unescaped = ret == null ? null : escaper.Unescape(ret);
      return unescaped;
    }

    #region PInvoke
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr LoadLibrary(string libname);

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

    delegate bool J2K_InitializeEx(
      [MarshalAs(UnmanagedType.LPStr)] string user,
      [MarshalAs(UnmanagedType.LPStr)] string key);
    delegate IntPtr J2K_TranslateMMNTW(int data0, [MarshalAs(UnmanagedType.LPWStr)] string jpStr);
    delegate void J2K_FreeMem(IntPtr ptr);
    #endregion
  }


  /// <summary>
  /// EzTrans trims the string, so pre/post process are required to preserve spaces.
  /// </summary>
  /// <remarks>
  /// It can't be a simple text to text function. It will affect translation result
  /// to replace spaces at the end of line with non spaces.
  /// </remarks>
  internal class EzTransEscaper {

    enum EscapeKind {
      None,
      Symbol,
      Space
    }

    private static readonly string Escaper = "[;:}";
    private static readonly EncodingTester Sjis = new EncodingTester(932);
    private static readonly Regex RxDecode =
      new Regex(@"(\r\n)|(\[;:})|[\r\[]|[^\r\[]+", RegexOptions.Compiled);

    /// <summary>
    /// Filter characters which can be modified if repeated.
    /// </summary>
    private static bool IsSequenceMutableSymbol(char c) {
      return "─―#\\".Contains(c);
    }

    /// <summary>
    /// Test whether there is a possibility of the single letter falsification 
    /// </summary>
    private static bool IsUnsafeChar(char c) {
      return c == '@' // Hdor escape character
        || c == '-' // It may be changed to ―
        || !Sjis.IsEncodable(c);
    }


    private readonly List<string> preserveds = new List<string>();
    private readonly StringBuilder buffer = new StringBuilder();
    private readonly StringBuilder escaping = new StringBuilder();
    private EscapeKind kind = EscapeKind.None;

    public string Escape(string notEscaped) {
      buffer.Clear();
      buffer.EnsureCapacity(notEscaped.Length * 3 / 2);

      foreach (char c in notEscaped) {
        if (FeedEscape(c)) {
          continue;
        }
        else if (IsUnsafeChar(c)) {
          SetEscapingKind(EscapeKind.None);
          preserveds.Add(c.ToString());
          buffer.Append(Escaper);
        }
        else {
          buffer.Append(c);
        }
      }
      FlushSpaces();

      return buffer.ToString();
    }

    public string Unescape(string escaped) {
      buffer.Clear();

      List<string>.Enumerator hydrate = preserveds.GetEnumerator();
      foreach (Match m in RxDecode.Matches(escaped)) {
        if (m.Groups[1].Success || m.Groups[2].Success) {
          hydrate.MoveNext();
          buffer.Append(hydrate.Current);
        }
        else {
          buffer.Append(m.Value);
        }
      }

      return buffer.ToString();
    }

    private bool FeedEscape(char c) {
      if (IsSequenceMutableSymbol(c)) {
        SetEscapingKind(EscapeKind.Symbol);
        escaping.Append(c);
        return true;
      }
      else if (char.IsWhiteSpace(c)) {
        SetEscapingKind(EscapeKind.Space);
        escaping.Append(c);
        return true;
      }
      else {
        FlushSpaces();
        return false;
      }
    }

    private void SetEscapingKind(EscapeKind value) {
      if (kind != value) {
        FlushSpaces();
        kind = value;
      }
    }

    private void FlushSpaces() {
      string space = escaping.ToString();
      escaping.Clear();

      if (space.Length == 0) {
        return;
      }
      else if (space.Contains('\n')) {
        buffer.Append("\r\n");
        preserveds.Add(space);
      }
      else if (space.Length == 1) {
        buffer.Append(space);
      }
      else {
        buffer.Append(Escaper);
        preserveds.Add(space);
      }
    }
  }
}
