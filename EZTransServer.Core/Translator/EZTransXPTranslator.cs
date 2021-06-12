using EZTransServer.Core.Utility;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace EZTransServer.Core.Translator
{
    public class EZTransXPTranslator : ITranslator
    {

        public static async Task<EZTransXPTranslator> Create(string? eztPath = null, int msDelay = 200)
        {
            var exceptions = new Dictionary<string, Exception>();
            foreach (string path in GetEztransDirs(eztPath))
            {
                if (!File.Exists(Path.Combine(path, "J2KEngine.dll")))
                {
                    continue;
                }
                try
                {
                    IntPtr eztransDll = await LoadNativeDll(path, msDelay).ConfigureAwait(false);
                    return new EZTransXPTranslator(eztransDll);
                }
                catch (Exception e)
                {
                    exceptions.Add(path, e);
                }
            }

            string detail = string.Join("", exceptions.Select(x => $"\n  {x.Key}: {x.Value.Message}"));
            throw new EztransNotFoundException(detail);
        }

        private static IEnumerable<string> GetEztransDirs(string? path)
        {
            var paths = new List<string>();

            if (path != null)
            {
                paths.Add(path);
            }

            string? regPath = GetEztransDirFromReg();
            if (regPath != null)
            {
                paths.Add(regPath);
            }

            string defPath = @"C:\Program Files (x86)\ChangShinSoft\ezTrans XP";
            paths.Add(defPath);
            paths.AddRange(GetAssemblyParentDirectories());

            return paths.Distinct();
        }

        public static string? GetEztransDirFromReg()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                RegistryKey key = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32);
                return key.OpenSubKey(@"Software\ChangShin\ezTrans")?.GetValue(@"FilePath") as string;
            }
            else
            {
                return null;
            }
        }

        private static IEnumerable<string> GetAssemblyParentDirectories()
        {
            string child = System.Reflection.Assembly.GetEntryAssembly().Location;
            while (true)
            {
                string? parent = Path.GetDirectoryName(child);
                if (parent == null)
                {
                    break;
                }
                yield return parent;
                child = parent;
            }
        }

        private static async Task<IntPtr> LoadNativeDll(string eztPath, int msDelay)
        {
            IntPtr EztransDll = LoadLibrary(GetDllPath(eztPath));
            if (EztransDll == IntPtr.Zero)
            {
                int errorCode = Marshal.GetLastWin32Error();
                throw new EztransException($"Failed to load library. Error code is '{errorCode}'.");
            }

            await Task.Delay(msDelay).ConfigureAwait(false);
            string key = Path.Combine(eztPath, "Dat");
            var initEx = GetFuncAddress<J2K_InitializeEx>(EztransDll, "J2K_InitializeEx");
            if (!initEx("CSUSER123455", key))
            {
                throw new EztransException("Engine initialization failed.");
            }

            return EztransDll;
        }

        private static string GetDllPath(string eztPath)
        {
            return Path.Combine(eztPath, "J2KEngine.dll");
        }

        private static T GetFuncAddress<T>(IntPtr dll, string name)
        {
            IntPtr addr = GetProcAddress(dll, name);
            if (addr == IntPtr.Zero)
            {
                throw new EztransException("This file is not a valid Ehnd file.");
            }
            return Marshal.GetDelegateForFunctionPointer<T>(addr);
        }


        private readonly J2K_FreeMem J2kFree;
        private readonly J2K_TranslateMMNTW J2kMmntw;

        private EZTransXPTranslator(IntPtr eztransDll)
        {
            J2kMmntw = GetFuncAddress<J2K_TranslateMMNTW>(eztransDll, "J2K_TranslateMMNTW");
            J2kFree = GetFuncAddress<J2K_FreeMem>(eztransDll, "J2K_FreeMem");
        }

        public Task<string?> Translate(string jpStr)
        {
            return Task.FromResult(TranslateInternal(jpStr));
        }

        public async Task<bool> IsHdorEnabled()
        {
            string? chk = await Translate("蜜ドル辞典").ConfigureAwait(false);
            return chk?.Contains("OK") ?? false;
        }

        public void Dispose()
        {
            // 원래 FreeLibrary를 호출하려 했는데 그러면 Access violation이 뜬다.
        }

        private string? TranslateInternal(string jpStr)
        {
            var escaper = new EscapeProcessor();
            string escaped = escaper.Escape(jpStr);
            IntPtr p = J2kMmntw(0, escaped);
            if (p == IntPtr.Zero)
            {
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
}
