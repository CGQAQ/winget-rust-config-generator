using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Channels;
using System.Threading.Tasks;
using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;

namespace winget_rust_autoupdate.cs
{
    class RustCompilerArchInfo
    {
        public string Version { get; set; }
        public string GnuX86 { get; set; }
        public string GnuX64 { get; set; }
        public string MsvcX86 { get; set; }
        public string MsvcX64 { get; set; }
    }

    class RustCompilerBranchInfo
    {
        public RustCompilerBranchInfo()
        {
            Stable = new RustCompilerArchInfo();
            Beta = new RustCompilerArchInfo();
            Nightly = new RustCompilerArchInfo();
        }

        public RustCompilerArchInfo Stable { get; set; }
        public RustCompilerArchInfo Beta { get; set; }
        public RustCompilerArchInfo Nightly { get; set; }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            var compiler = await FetchMetaInfo();
            await WriteToFile(compiler.Stable.GnuX86, "x86", compiler.Stable.Version, @".\gnu-template.yaml",
                $@".\{compiler.Stable.Version}-gnu86.yaml");
            await WriteToFile(compiler.Stable.MsvcX86, "x86", compiler.Stable.Version, @".\msvc-template.yaml",
                $@".\{compiler.Stable.Version}-msvc86.yaml");
            await WriteToFile(compiler.Stable.GnuX64, "x64", compiler.Stable.Version, @".\gnu-template.yaml",
                $@".\{compiler.Stable.Version}-gnu64.yaml");
            await WriteToFile(compiler.Stable.MsvcX64, "x64", compiler.Stable.Version, @".\gnu-template.yaml",
                $@".\{compiler.Stable.Version}-msvc64.yaml");
            return;
        }

        static async Task WriteToFile(string url, string arch, string version, string templateFile, string dest)
        {
            var template = await File.ReadAllTextAsync(templateFile);
            template = template.Replace("<VERSION>", version);
            template = template.Replace("<ARCH>", arch);
            template = template.Replace("<URL>", url);
            template = template.Replace("<HASH>", await GetNetFileHash(url));
            await File.WriteAllTextAsync(dest, template);
        }

        static async Task<string> GetNetFileHash(string url)
        {
            var http = new HttpClient();
            var res = await http.GetStreamAsync(url);
            var result = "";
            using (var sha256 = new SHA256Managed())
            {
                sha256.Initialize();
                var hash = sha256.ComputeHash(res);
                var str = (from b in hash
                        select b.ToString("x2"))
                    .Aggregate((a, b) => a + b).ToUpper();
                result = str;
            }

            return result;
        }

        static async Task<RustCompilerBranchInfo> FetchMetaInfo()
        {
            var compiler = new RustCompilerBranchInfo();
            var url = "https://forge.rust-lang.org/infra/other-installation-methods.html";
            var http = new HttpClient();
            var res = await http.GetStringAsync(url);
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(res);
            var documentNode = htmlDocument.DocumentNode;
            var version =
                documentNode.QuerySelector("#content > main > table:nth-child(23) > thead > tr > th:nth-child(2)");
            // Console.Out.WriteLine(version.InnerText.Split(" ")[1].TrimStart('(').TrimEnd(')'));
            compiler.Stable.Version = version.InnerText.Split(" ")[1].TrimStart('(').TrimEnd(')');
            var table = documentNode.QuerySelectorAll("#content > main > table:nth-child(23) > tbody > tr");
            var trs = from tr in table
                where tr.InnerText.Contains("windows")
                select (tr.QuerySelector("td > code").InnerText, tr.QuerySelector("td > a").Attributes["href"].Value);

            var download_info = trs.ToList();
            compiler.Stable.GnuX86 = download_info[0].Value;
            compiler.Stable.MsvcX86 = download_info[1].Value;
            compiler.Stable.GnuX64 = download_info[2].Value;
            compiler.Stable.MsvcX64 = download_info[3].Value;
            return compiler;
        }
    }
}