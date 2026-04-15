using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Authentication;

namespace CodexFixAgent
{
    // ─────────────────────────────────────────────
    //  ENTRY POINT
    // ─────────────────────────────────────────────
    class Program
    {
        static void Main(string[] args)
        {
            // .NET 4.0 defaults to TLS 1.0 — Azure requires TLS 1.2
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; // Tls12

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔══════════════════════════════╗");
            Console.WriteLine("║      Codex Fix Agent         ║");
            Console.WriteLine("║    (Azure OpenAI)            ║");
            Console.WriteLine("╚══════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();

            string filePath  = null;
            string errorMsg  = null;
            bool   autoApply = false;

            for (int i = 0; i < args.Length; i++)
            {
                if ((args[i] == "--error" || args[i] == "-e") && i + 1 < args.Length)
                    errorMsg = args[++i];
                else if (args[i] == "--auto")
                    autoApply = true;
                else if (filePath == null)
                    filePath = args[i].Trim('"');
            }

            if (string.IsNullOrEmpty(filePath))
            {
                Console.Write("Enter path to code file: ");
                filePath = (Console.ReadLine() ?? "").Trim().Trim('"');
            }

            if (!File.Exists(filePath))
            {
                PrintError("File not found: " + filePath);
                Pause(); return;
            }

            if (string.IsNullOrEmpty(errorMsg))
            {
                Console.WriteLine("Paste the error / exception, then type END on a new line:");
                var sb = new StringBuilder();
                string line;
                while ((line = Console.ReadLine()) != null && line.Trim() != "END")
                    sb.AppendLine(line);
                errorMsg = sb.ToString().Trim();
            }

            new FixAgent().Run(filePath, errorMsg, autoApply);
            Pause();
        }

        public static void PrintError(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[ERROR] " + msg);
            Console.ResetColor();
        }

        static void Pause()
        {
            try
            {
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
            }
            catch { /* non-interactive terminal — skip */ }
        }
    }

    // ─────────────────────────────────────────────
    //  AGENT ORCHESTRATOR
    // ─────────────────────────────────────────────
    class FixAgent
    {
        public void Run(string filePath, string error, bool autoApply)
        {
            if (!ApiKeyStore.Load())
            {
                Program.PrintError("Missing Azure credentials in keys.config.");
                Console.WriteLine("  Config file: " + ApiKeyStore.ConfigPath);
                Console.WriteLine("  Required fields:");
                Console.WriteLine("    AZURE_OPENAI_ENDPOINT=https://...openai.azure.com/openai/responses?api-version=...");
                Console.WriteLine("    AZURE_OPENAI_KEY=your-key-here");
                Console.WriteLine("    AZURE_DEPLOYMENT=gpt-4o");
                return;
            }

            string originalCode = File.ReadAllText(filePath, Encoding.UTF8);
            string fileName     = Path.GetFileName(filePath);

            Console.WriteLine("[Agent] File       : " + filePath);
            Console.WriteLine("[Agent] Endpoint   : " + ApiKeyStore.Endpoint);
            Console.WriteLine("[Agent] Deployment : " + ApiKeyStore.Deployment);
            Console.WriteLine("[Agent] Calling Azure OpenAI...\n");

            string fixedCode = AzureOpenAIClient.GetFix(fileName, originalCode, error);
            if (fixedCode == null) return;

            bool hasChanges = DiffHelper.Show(originalCode, fixedCode, fileName);
            if (!hasChanges)
            {
                Console.WriteLine("[Agent] LLM returned no changes.");
                return;
            }

            bool apply = autoApply;
            if (!autoApply)
            {
                Console.Write("\nApply this fix? (y/n): ");
                apply = Console.ReadKey().KeyChar == 'y';
                Console.WriteLine();
            }

            if (apply)
            {
                string backup = filePath + ".bak";
                File.WriteAllText(backup, originalCode, Encoding.UTF8);
                Console.WriteLine("[Agent] Backup saved : " + backup);

                File.WriteAllText(filePath, fixedCode, Encoding.UTF8);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[Agent] Fix applied  : " + filePath);
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine("[Agent] Fix discarded — no changes made.");
            }
        }
    }

    // ─────────────────────────────────────────────
    //  AZURE OPENAI CLIENT
    //  Uses the Responses API  (/openai/responses)
    //  Auth header : api-key  (not Bearer)
    // ─────────────────────────────────────────────
    static class AzureOpenAIClient
    {
        public static string GetFix(string fileName, string code, string error)
        {
            string system =
                "You are a code repair agent. " +
                "Return ONLY the complete fixed code file. " +
                "No explanations. No markdown. No code fences. Raw code only.";

            string user = string.Format(
                "File: {0}\n\nError/Exception:\n{1}\n\nOriginal Code:\n{2}",
                fileName, error, code);

            string body = BuildRequestJson(ApiKeyStore.Deployment, system, user);

            try
            {
                var req = (HttpWebRequest)WebRequest.Create(ApiKeyStore.Endpoint);
                req.Method      = "POST";
                req.ContentType = "application/json";
                req.Headers.Add("Authorization", "Bearer " + ApiKeyStore.Key);
                req.Timeout     = 90000;

                byte[] bytes = Encoding.UTF8.GetBytes(body);
                req.ContentLength = bytes.Length;
                using (var s = req.GetRequestStream())
                    s.Write(bytes, 0, bytes.Length);

                using (var res = (HttpWebResponse)req.GetResponse())
                using (var rdr = new StreamReader(res.GetResponseStream(), Encoding.UTF8))
                {
                    string json = rdr.ReadToEnd();
                    return ExtractText(json);
                }
            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                {
                    using (var rdr = new StreamReader(ex.Response.GetResponseStream()))
                        Program.PrintError("Azure API error: " + rdr.ReadToEnd());
                }
                else
                    Program.PrintError("Network error: " + ex.Message);
                return null;
            }
            catch (Exception ex)
            {
                Program.PrintError("Unexpected error: " + ex.Message);
                return null;
            }
        }

        // Azure OpenAI Responses API request body
        // POST /openai/responses?api-version=2025-04-01-preview
        private static string BuildRequestJson(string deployment, string system, string user)
        {
            return string.Format(
                "{{" +
                "\"model\":\"{0}\"," +
                "\"input\":[" +
                "{{\"role\":\"system\",\"content\":\"{1}\"}}," +
                "{{\"role\":\"user\",\"content\":\"{2}\"}}" +
                "]" +
                "}}",
                deployment,
                EscapeJson(system),
                EscapeJson(user));
        }

        private static string EscapeJson(string s)
        {
            return s.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\r\n", "\\n")
                    .Replace("\n",   "\\n")
                    .Replace("\r",   "\\n")
                    .Replace("\t",   "\\t");
        }

        // Responses API returns:
        // { "output": [{ "content": [{ "type":"output_text", "text":"..." }] }] }
        private static string ExtractText(string json)
        {
            var m = Regex.Match(json,
                @"""text""\s*:\s*""((?:[^""\\]|\\.)*)""",
                RegexOptions.Singleline);
            if (!m.Success) return null;

            string raw = m.Groups[1].Value;
            return raw.Replace("\\n",  "\n")
                      .Replace("\\r",  "\r")
                      .Replace("\\t",  "\t")
                      .Replace("\\\"", "\"")
                      .Replace("\\\\", "\\");
        }
    }

    // ─────────────────────────────────────────────
    //  API KEY STORE
    //  Search order for keys.config:
    //    1. Path in CODEX_KEYS_CONFIG env variable  ← custom location
    //    2. Next to the .exe          (bin\Debug\)
    //    3. CodexFixAgent project folder (two levels up)
    //    4. Solution root             (three levels up)
    //  keys.config is git-ignored — never committed
    // ─────────────────────────────────────────────
    static class ApiKeyStore
    {
        public static string ConfigPath { get; private set; }

        public static string Endpoint   { get; private set; }
        public static string Key        { get; private set; }
        public static string Deployment { get; private set; }

        public static bool Load()
        {
            ConfigPath = FindConfigFile();

            if (ConfigPath != null)
            {
                Console.WriteLine("[Agent] Keys loaded from: " + ConfigPath);
                ParseFile(ConfigPath);
            }

            // Fall back to environment variables
            if (string.IsNullOrEmpty(Endpoint))
                Endpoint   = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
            if (string.IsNullOrEmpty(Key))
                Key        = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY");
            if (string.IsNullOrEmpty(Deployment))
                Deployment = Environment.GetEnvironmentVariable("AZURE_DEPLOYMENT") ?? "gpt-4o";

            return !string.IsNullOrEmpty(Endpoint) && !string.IsNullOrEmpty(Key);
        }

        private static string FindConfigFile()
        {
            // 1. Custom path via environment variable
            string custom = Environment.GetEnvironmentVariable("CODEX_KEYS_CONFIG");
            if (!string.IsNullOrEmpty(custom) && File.Exists(custom))
                return custom;

            // 2. Next to the .exe  (bin\Debug\keys.config)
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] candidates = new[]
            {
                Path.Combine(exeDir, "keys.config"),                          // bin\Debug\
                Path.Combine(exeDir, @"..\..\keys.config"),                   // CodexFixAgent\
                Path.Combine(exeDir, @"..\..\..\keys.config")                 // solution root
            };

            foreach (string path in candidates)
            {
                string full = Path.GetFullPath(path);
                if (File.Exists(full)) return full;
            }

            // Not found — create a template at the project folder level
            string projectFolder = Path.GetFullPath(Path.Combine(exeDir, @"..\..\"));
            string template      = Path.Combine(projectFolder, "keys.config");
            File.WriteAllText(template,
                "# Codex Fix Agent - Azure OpenAI Credentials\n" +
                "# This file is git-ignored. Do NOT commit it.\n\n" +
                "AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com/openai/responses?api-version=2025-04-01-preview\n" +
                "AZURE_OPENAI_KEY=your-key-here\n" +
                "AZURE_DEPLOYMENT=gpt-4o\n");
            Console.WriteLine("[Agent] Created template keys.config at:\n  " + template);
            return null;
        }

        private static void ParseFile(string path)
        {
            foreach (var raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (line.StartsWith("#") || !line.Contains("=")) continue;

                int    eq = line.IndexOf('=');
                string k  = line.Substring(0, eq).Trim();
                string v  = line.Substring(eq + 1).Trim();

                if (k == "AZURE_OPENAI_ENDPOINT") Endpoint   = v;
                if (k == "AZURE_OPENAI_KEY")      Key        = v;
                if (k == "AZURE_DEPLOYMENT")      Deployment = v;
            }
        }
    }

    // ─────────────────────────────────────────────
    //  DIFF HELPER  — coloured line-level diff
    // ─────────────────────────────────────────────
    static class DiffHelper
    {
        public static bool Show(string original, string fixed_, string fileName)
        {
            string[] origLines = original.Replace("\r\n", "\n").Split('\n');
            string[] fixLines  = fixed_.Replace("\r\n",  "\n").Split('\n');

            Console.WriteLine("--- original/" + fileName);
            Console.WriteLine("+++ fixed/"    + fileName);
            Console.WriteLine();

            int changed = 0;
            int maxLen  = Math.Max(origLines.Length, fixLines.Length);

            for (int i = 0; i < maxLen; i++)
            {
                string orig = i < origLines.Length ? origLines[i] : null;
                string fix  = i < fixLines.Length  ? fixLines[i]  : null;

                if (orig == fix) continue;
                changed++;

                if (orig != null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(string.Format("- [{0,4}] {1}", i + 1, orig));
                }
                if (fix != null)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(string.Format("+ [{0,4}] {1}", i + 1, fix));
                }
                Console.ResetColor();
            }

            if (changed == 0)
            {
                Console.WriteLine("(no differences found)");
                return false;
            }

            Console.WriteLine(string.Format("\n{0} line(s) changed.", changed));
            return true;
        }
    }
}
