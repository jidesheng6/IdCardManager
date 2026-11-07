using System;
using System.IO;
using System.Reflection;
using System.Text;

class P
{
    static void Main()
    {
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
        {
            var name = e.Name.Split(',')[0];
            foreach (var c in new[]
            {
                @"F:\LrSingle\LRCenter\csmodules\" + name + ".dll",
                @"C:\Program Files (x86)\Common Files\Lealren\" + name + ".dll",
                @"F:\LrSingle\LRCenter\" + name + ".dll"
            })
            {
                if (File.Exists(c))
                {
                    try { return Assembly.LoadFrom(c); } catch { }
                }
            }
            return null;
        };

        Dump(@"F:\LrSingle\LRCenter\csmodules\Lr.Intero.Center.Context.dll");
        Dump(@"C:\Program Files (x86)\Common Files\Lealren\Lr.Intero.Context.dll");
        DumpStoreValue();
    }

    static void Dump(string path)
    {
        Console.WriteLine("==== " + path + " ====");
        if (!File.Exists(path)) { Console.WriteLine("missing"); return; }
        var a = Assembly.LoadFrom(path);
        Type[] types;
        try { types = a.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { types = Array.FindAll(ex.Types, t => t != null); }
        foreach (var t in types)
        {
            Console.WriteLine("TYPE " + t.FullName);
            foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            {
                if (p.Name.IndexOf("Key", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    p.Name.IndexOf("Station", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    p.Name.IndexOf("App", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    p.Name.IndexOf("Db", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    p.Name.IndexOf("Terminal", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Console.WriteLine("  PROP " + p.PropertyType.Name + " " + p.Name);
                }
            }
            foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                if (m.Name.IndexOf("SysConf", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    m.Name.IndexOf("AppKey", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    m.Name.IndexOf("Load", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Console.WriteLine("  METH " + m.Name);
                }
            }
        }
    }

    static void DumpStoreValue()
    {
        Console.WriteLine("==== StoreValue GetSysConf IL strings ====");
        var path = @"F:\LrSingle\LRCenter\csmodules\Lr.Intero.Center.Modules.StoreValueForCard.dll";
        var bytes = File.ReadAllBytes(path);
        var uni = Encoding.Unicode.GetString(bytes);
        Extract(uni, "AppKey");
        Extract(uni, "SysConf");
        Extract(uni, "SELECT");
        Extract(Encoding.Default.GetString(bytes), "AppKey");
    }

    static void Extract(string text, string key)
    {
        int i = 0, n = 0;
        while ((i = text.IndexOf(key, i, StringComparison.OrdinalIgnoreCase)) >= 0 && n < 10)
        {
            int start = Math.Max(0, i - 50);
            int len = Math.Min(180, text.Length - start);
            var frag = text.Substring(start, len);
            var clean = new StringBuilder();
            foreach (var ch in frag)
            {
                if (ch >= 32 && ch < 127 || ch > 127) clean.Append(ch);
                else clean.Append(' ');
            }
            Console.WriteLine(".." + clean + "..");
            i += key.Length;
            n++;
        }
    }
}
