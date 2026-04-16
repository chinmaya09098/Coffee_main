using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;

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
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔══════════════════════════════════════╗");
            Console.WriteLine("║         Codex Fix Agent              ║");
            Console.WriteLine("║  Azure OpenAI + Git + GitHub PR      ║");
            Console.WriteLine("╚══════════════════════════════════════╝");
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

        public static void PrintSuccess(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(msg);
            Console.ResetColor();
        }

        public static void PrintInfo(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(msg);
            Console.ResetColor();
        }

        static void Pause()
        {
            try { Console.WriteLine("\nPress any key to exit..."); Console.ReadKey(); }
            catch { }
        }
    }

    // ─────────────────────────────────────────────
    //  AGENT ORCHESTRATOR
    //  Full workflow:
    //    1. Create fix branch from main
    //    2. Call LLM to fix the file
    //    3. Show diff  → apply
    //    4. Commit + push branch
    //    5. Create PR to main via GitHub API
    // ─────────────────────────────────────────────
    class FixAgent
    {
        public void Run(string filePath, string error, bool autoApply)
        {
            if (!ApiKeyStore.Load())
            {
                Program.PrintError("Missing Azure credentials in keys.config.");
                Console.WriteLine("  Config file: " + ApiKeyStore.ConfigPath);
                return;
            }

            // ── 1. Find repo root ──────────────────────
            if (!GitHelper.FindRepoRoot(filePath))
            {
                Program.PrintError("Could not find a git repository for: " + filePath);
                return;
            }
            Console.WriteLine("[Agent] Repo root  : " + GitHelper.RepoRoot);

            // ── 2. Create fix branch from main ─────────
            string branchName;
            try
            {
                branchName = GitHelper.CreateFixBranch(error);
                Program.PrintInfo("[Agent] Branch     : " + branchName);
            }
            catch (Exception ex)
            {
                Program.PrintError("Git branch failed: " + ex.Message);
                return;
            }

            // ── 3. Call LLM ────────────────────────────
            string originalCode = File.ReadAllText(filePath, Encoding.UTF8);
            string fileName     = Path.GetFileName(filePath);

            Console.WriteLine("[Agent] File       : " + filePath);
            Console.WriteLine("[Agent] Calling Azure OpenAI...\n");

            string rawFix    = AzureOpenAIClient.GetFix(fileName, originalCode, error);
            if (rawFix == null)
            {
                GitHelper.Run("checkout main");
                GitHelper.Run("branch -D " + branchName);
                return;
            }

            // Apply smart merge — only accept changes near the error line
            int    errorLine = AzureOpenAIClient.ParseErrorLine(error);
            string fixedCode = AzureOpenAIClient.SmartMerge(originalCode, rawFix, errorLine);

            // ── 4. Show diff ───────────────────────────
            bool hasChanges = DiffHelper.Show(originalCode, fixedCode, fileName);
            if (!hasChanges)
            {
                Console.WriteLine("[Agent] LLM returned no changes.");
                GitHelper.Run("checkout main");
                GitHelper.Run("branch -D " + branchName);
                return;
            }

            // ── 5. Confirm ─────────────────────────────
            bool apply = autoApply;
            if (!autoApply)
            {
                Console.Write("\nApply fix, commit, push and create PR? (y/n): ");
                apply = Console.ReadKey().KeyChar == 'y';
                Console.WriteLine();
            }

            if (!apply)
            {
                Console.WriteLine("[Agent] Cancelled — switching back to main.");
                GitHelper.Run("checkout main");
                GitHelper.Run("branch -D " + branchName);
                return;
            }

            // ── 6. Write fix ───────────────────────────
            File.WriteAllText(filePath + ".bak", originalCode, Encoding.UTF8);
            File.WriteAllText(filePath, fixedCode, Encoding.UTF8);
            Program.PrintSuccess("[Agent] Fix applied: " + filePath);

            // ── 7. Commit + push ───────────────────────
            try
            {
                string summary = GitHelper.ErrorSummary(error);
                GitHelper.CommitAndPush(filePath, branchName, summary);
                Program.PrintSuccess("[Agent] Pushed     : " + branchName);
            }
            catch (Exception ex)
            {
                Program.PrintError("Git push failed: " + ex.Message);
                return;
            }

            // ── 8. Create PR ───────────────────────────
            if (string.IsNullOrEmpty(ApiKeyStore.GithubToken) ||
                ApiKeyStore.GithubToken == "your-github-token-here")
            {
                Console.WriteLine("\n[Agent] No GITHUB_TOKEN in keys.config.");
                Console.WriteLine("[Agent] Create PR manually at:");
                string[] ownerRepo = GitHelper.GetOwnerRepo();
                Program.PrintInfo(string.Format(
                    "  https://github.com/{0}/{1}/compare/{2}",
                    ownerRepo[0], ownerRepo[1], branchName));
            }
            else
            {
                try
                {
                    string[] ownerRepo = GitHelper.GetOwnerRepo();
                    string   summary   = GitHelper.ErrorSummary(error);
                    string   prUrl     = GitHubClient.CreatePR(
                        ApiKeyStore.GithubToken,
                        ownerRepo[0], ownerRepo[1],
                        branchName,
                        "Fix: " + summary,
                        "**Auto-fixed by Codex Fix Agent**\n\n" +
                        "**File:** `" + fileName + "`\n\n" +
                        "**Error:**\n```\n" + error + "\n```");

                    Program.PrintSuccess("[Agent] PR created : " + prUrl);

                    // Send email notification
                    EmailNotifier.Send(
                        subject : "PR Created: Fix for " + GitHelper.ErrorSummary(error),
                        body    : "A bug fix PR has been created automatically by Codex Fix Agent.\n\n" +
                                  "File    : " + fileName + "\n" +
                                  "Branch  : " + branchName + "\n" +
                                  "PR Link : " + prUrl + "\n\n" +
                                  "Error:\n" + error);
                }
                catch (Exception ex)
                {
                    Program.PrintError("PR creation failed: " + ex.Message);
                    Console.WriteLine("[Agent] Push succeeded — create PR manually on GitHub.");
                }
            }
        }
    }

    // ─────────────────────────────────────────────
    //  GIT HELPER
    // ─────────────────────────────────────────────
    static class GitHelper
    {
        public static string RepoRoot { get; private set; }

        public static bool FindRepoRoot(string filePath)
        {
            string dir = Path.GetDirectoryName(Path.GetFullPath(filePath));
            while (!string.IsNullOrEmpty(dir))
            {
                if (Directory.Exists(Path.Combine(dir, ".git")))
                {
                    RepoRoot = dir;
                    return true;
                }
                dir = Path.GetDirectoryName(dir);
            }
            return false;
        }

        public static string Run(string gitArgs)
        {
            var psi = new ProcessStartInfo("git", gitArgs)
            {
                WorkingDirectory       = RepoRoot,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true
            };

            var p = Process.Start(psi);
            string stdout = p.StandardOutput.ReadToEnd();
            string stderr = p.StandardError.ReadToEnd();
            p.WaitForExit();

            if (p.ExitCode != 0)
                throw new Exception(stderr.Trim());

            return stdout.Trim();
        }

        public static string CreateFixBranch(string errorMessage)
        {
            // Switch to main and pull latest
            Run("checkout main");
            Run("pull origin main");

            // Build branch name — append timestamp to guarantee uniqueness
            string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmm");
            string name      = "fix/" + SanitizeBranch(errorMessage) + "-" + timestamp;
            Run("checkout -b " + name);
            return name;
        }

        public static void CommitAndPush(string filePath, string branch, string summary)
        {
            string rel = GetRelativePath(filePath);
            Run("add " + rel);
            Run("commit -m \"Fix: " + summary.Replace("\"", "'") + "\"");
            Run("push -u origin " + branch);
        }

        public static string[] GetOwnerRepo()
        {
            string url = Run("remote get-url origin");
            // https://github.com/owner/repo.git  OR  git@github.com:owner/repo.git
            var m = Regex.Match(url, @"github\.com[:/]([^/]+)/([^/\.]+)");
            if (!m.Success) return new[] { "owner", "repo" };
            return new[] { m.Groups[1].Value, m.Groups[2].Value };
        }

        public static string ErrorSummary(string error)
        {
            // Take first meaningful line, cap at 60 chars
            string first = error.Split(new[] { '\n', '\r' },
                StringSplitOptions.RemoveEmptyEntries)[0].Trim();
            return first.Length > 60 ? first.Substring(0, 60) : first;
        }

        private static string SanitizeBranch(string error)
        {
            string first = error.Split(new[] { '\n', '\r' },
                StringSplitOptions.RemoveEmptyEntries)[0].Trim();
            string lower = first.ToLower();
            lower = Regex.Replace(lower, @"[^a-z0-9\s]", " ");
            var words    = lower.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string name  = string.Join("-", words, 0, Math.Min(words.Length, 6));
            return name.Length > 50 ? name.Substring(0, 50) : name;
        }

        private static string GetRelativePath(string filePath)
        {
            string full = Path.GetFullPath(filePath);
            string root = RepoRoot.TrimEnd('\\', '/') + Path.DirectorySeparatorChar;
            return full.StartsWith(root)
                ? full.Substring(root.Length).Replace('\\', '/')
                : full;
        }
    }

    // ─────────────────────────────────────────────
    //  GITHUB API CLIENT  — creates PRs
    // ─────────────────────────────────────────────
    static class GitHubClient
    {
        public static string CreatePR(string token, string owner, string repo,
                                      string branch, string title, string body)
        {
            string url  = string.Format(
                "https://api.github.com/repos/{0}/{1}/pulls", owner, repo);

            string json = string.Format(
                "{{\"title\":\"{0}\",\"body\":\"{1}\",\"head\":\"{2}\",\"base\":\"main\"}}",
                Escape(title), Escape(body), branch);

            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Method      = "POST";
            req.ContentType = "application/json";
            req.Headers.Add("Authorization", "Bearer " + token);
            req.UserAgent   = "CodexFixAgent/1.0";
            req.Timeout     = 30000;

            byte[] bytes = Encoding.UTF8.GetBytes(json);
            req.ContentLength = bytes.Length;
            using (var s = req.GetRequestStream())
                s.Write(bytes, 0, bytes.Length);

            using (var res = (HttpWebResponse)req.GetResponse())
            using (var rdr = new StreamReader(res.GetResponseStream(), Encoding.UTF8))
            {
                string response = rdr.ReadToEnd();
                var m = Regex.Match(response, @"""html_url""\s*:\s*""([^""]+)""");
                return m.Success ? m.Groups[1].Value : "(PR created — check GitHub)";
            }
        }

        private static string Escape(string s)
        {
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"")
                    .Replace("\r\n", "\\n").Replace("\n", "\\n")
                    .Replace("\r", "\\n").Replace("\t", "\\t");
        }
    }

    // ─────────────────────────────────────────────
    //  AZURE OPENAI CLIENT
    // ─────────────────────────────────────────────
    static class AzureOpenAIClient
    {
        public static string GetFix(string fileName, string code, string error)
        {
            int errorLine = ParseErrorLine(error);

            string system =
                "You are a code repair agent. " +
                "Fix ONLY the specific bug causing the error. " +
                "Do NOT reformat, reorder, or change any other lines. " +
                "Return the COMPLETE file with ONLY the minimum change needed. " +
                "No explanations. No markdown. No code fences. Raw code only.";

            string user = errorLine > 0
                ? string.Format(
                    "File: {0}\nError on line {1}:\n{2}\n\nFix ONLY line {1}. Return the full file unchanged except that line.\n\nCode:\n{3}",
                    fileName, errorLine, error, code)
                : string.Format(
                    "File: {0}\n\nError:\n{1}\n\nCode:\n{2}",
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
                    return ExtractText(rdr.ReadToEnd());
            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                    using (var rdr = new StreamReader(ex.Response.GetResponseStream()))
                        Program.PrintError("Azure API error: " + rdr.ReadToEnd());
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

        private static string BuildRequestJson(string deployment, string system, string user)
        {
            return string.Format(
                "{{\"model\":\"{0}\",\"input\":[" +
                "{{\"role\":\"system\",\"content\":\"{1}\"}}," +
                "{{\"role\":\"user\",\"content\":\"{2}\"}}" +
                "]}}",
                deployment, EscapeJson(system), EscapeJson(user));
        }

        // Parse "line 268" or ":268" from error message
        public static int ParseErrorLine(string error)
        {
            var m = Regex.Match(error, @"(?:line\s+|:)(\d+)", RegexOptions.IgnoreCase);
            return m.Success ? int.Parse(m.Groups[1].Value) : 0;
        }

        // Only accept LLM changes within ±window lines of the error line
        // Everything else is restored from the original
        public static string SmartMerge(string original, string fixed_, int errorLine, int window = 20)
        {
            if (errorLine <= 0) return fixed_;

            string[] origLines = original.Replace("\r\n", "\n").Split('\n');
            string[] fixLines  = fixed_.Replace("\r\n",  "\n").Split('\n');

            // If line counts differ, only keep the changed region
            if (origLines.Length != fixLines.Length)
            {
                Console.WriteLine(string.Format(
                    "[Agent] Smart merge: line count changed ({0} → {1}), applying targeted patch.",
                    origLines.Length, fixLines.Length));
                // Rebuild: keep orig lines outside window, use fix lines inside window
                int start  = Math.Max(0, errorLine - window - 1);
                int end    = Math.Min(origLines.Length - 1, errorLine + window - 1);
                var result = new System.Collections.Generic.List<string>();
                for (int i = 0; i < start; i++)
                    result.Add(origLines[i]);
                for (int i = 0; i < fixLines.Length; i++)
                    result.Add(fixLines[i]);
                for (int i = end + 1; i < origLines.Length; i++)
                    result.Add(origLines[i]);
                return string.Join("\n", result);
            }

            // Same line count — cherry-pick only changes near error line
            string[] merged = new string[origLines.Length];
            int accepted = 0, rejected = 0;
            for (int i = 0; i < origLines.Length; i++)
            {
                int lineNum   = i + 1;
                bool nearError = Math.Abs(lineNum - errorLine) <= window;
                if (origLines[i] != fixLines[i] && !nearError)
                {
                    merged[i] = origLines[i]; // restore original
                    rejected++;
                }
                else
                {
                    merged[i] = fixLines[i];
                    if (origLines[i] != fixLines[i]) accepted++;
                }
            }

            Console.WriteLine(string.Format(
                "[Agent] Smart merge: {0} line(s) fixed near line {1}, {2} unrelated change(s) ignored.",
                accepted, errorLine, rejected));

            return string.Join("\n", merged);
        }

        private static string EscapeJson(string s)
        {
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"")
                    .Replace("\r\n", "\\n").Replace("\n", "\\n")
                    .Replace("\r", "\\n").Replace("\t", "\\t");
        }

        private static string ExtractText(string json)
        {
            var m = Regex.Match(json,
                @"""text""\s*:\s*""((?:[^""\\]|\\.)*)""", RegexOptions.Singleline);
            if (!m.Success) return null;
            return m.Groups[1].Value
                .Replace("\\n", "\n").Replace("\\r", "\r")
                .Replace("\\t", "\t").Replace("\\\"", "\"")
                .Replace("\\\\", "\\");
        }
    }

    // ─────────────────────────────────────────────
    //  API KEY STORE
    // ─────────────────────────────────────────────
    static class ApiKeyStore
    {
        public static string ConfigPath   { get; private set; }
        public static string Endpoint     { get; private set; }
        public static string Key          { get; private set; }
        public static string Deployment   { get; private set; }
        public static string GithubToken  { get; private set; }
        public static string SmtpHost     { get; private set; }
        public static int    SmtpPort     { get; private set; }
        public static string SmtpUser     { get; private set; }
        public static string SmtpPass     { get; private set; }
        public static string NotifyEmail  { get; private set; }

        public static bool Load()
        {
            ConfigPath = FindConfigFile();
            if (ConfigPath != null)
            {
                Console.WriteLine("[Agent] Keys from  : " + ConfigPath);
                ParseFile(ConfigPath);
            }

            if (string.IsNullOrEmpty(Endpoint))
                Endpoint     = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
            if (string.IsNullOrEmpty(Key))
                Key          = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY");
            if (string.IsNullOrEmpty(Deployment))
                Deployment   = Environment.GetEnvironmentVariable("AZURE_DEPLOYMENT") ?? "gpt-4o";
            if (string.IsNullOrEmpty(GithubToken))
                GithubToken  = Environment.GetEnvironmentVariable("GITHUB_TOKEN");

            return !string.IsNullOrEmpty(Endpoint) && !string.IsNullOrEmpty(Key);
        }

        private static string FindConfigFile()
        {
            string custom = Environment.GetEnvironmentVariable("CODEX_KEYS_CONFIG");
            if (!string.IsNullOrEmpty(custom) && File.Exists(custom)) return custom;

            string exe = AppDomain.CurrentDomain.BaseDirectory;
            foreach (string rel in new[] {
                "keys.config",
                @"..\..\keys.config",
                @"..\..\..\keys.config" })
            {
                string full = Path.GetFullPath(Path.Combine(exe, rel));
                if (File.Exists(full)) return full;
            }
            return null;
        }

        private static void ParseFile(string path)
        {
            foreach (var raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (line.StartsWith("#") || !line.Contains("=")) continue;
                int eq = line.IndexOf('=');
                string k = line.Substring(0, eq).Trim();
                string v = line.Substring(eq + 1).Trim();

                if (k == "AZURE_OPENAI_ENDPOINT") Endpoint    = v;
                if (k == "AZURE_OPENAI_KEY")      Key         = v;
                if (k == "AZURE_DEPLOYMENT")      Deployment  = v;
                if (k == "GITHUB_TOKEN")          GithubToken = v;
                if (k == "SMTP_HOST")             SmtpHost    = v;
                if (k == "SMTP_PORT")             SmtpPort    = int.TryParse(v, out int p) ? p : 587;
                if (k == "SMTP_USER")             SmtpUser    = v;
                if (k == "SMTP_PASS")             SmtpPass    = v;
                if (k == "NOTIFY_EMAIL")          NotifyEmail = v;
            }
        }
    }

    // ─────────────────────────────────────────────
    //  EMAIL NOTIFIER
    //  Uses System.Net.Mail — no NuGet needed
    // ─────────────────────────────────────────────
    static class EmailNotifier
    {
        public static void Send(string subject, string body)
        {
            string host    = ApiKeyStore.SmtpHost;
            string user    = ApiKeyStore.SmtpUser;
            string pass    = ApiKeyStore.SmtpPass;
            string to      = ApiKeyStore.NotifyEmail;
            int    port    = ApiKeyStore.SmtpPort;

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(user) ||
                string.IsNullOrEmpty(pass) || string.IsNullOrEmpty(to))
            {
                Console.WriteLine("[Email] SMTP not configured in keys.config — skipping email.");
                return;
            }

            try
            {
                var msg = new MailMessage
                {
                    From       = new MailAddress(user, "Codex Fix Agent"),
                    Subject    = subject,
                    Body       = body,
                    IsBodyHtml = false
                };
                msg.To.Add(to);

                var smtp = new SmtpClient(host, port)
                {
                    EnableSsl             = true,
                    Credentials           = new System.Net.NetworkCredential(user, pass),
                    DeliveryMethod        = SmtpDeliveryMethod.Network,
                    Timeout               = 30000
                };

                smtp.Send(msg);
                Program.PrintSuccess("[Email] Notification sent to: " + to);
            }
            catch (Exception ex)
            {
                Program.PrintError("Email failed: " + ex.Message);
            }
        }
    }

    // ─────────────────────────────────────────────
    //  DIFF HELPER
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

            if (changed == 0) { Console.WriteLine("(no differences)"); return false; }
            Console.WriteLine(string.Format("\n{0} line(s) changed.", changed));
            return true;
        }
    }
}
