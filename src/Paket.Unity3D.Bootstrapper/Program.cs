using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;

namespace Paket.Unity3D.Bootstrapper
{
    internal class Program
    {
        const string PaketUnity3DVersionEnv = "PAKET.UNITY3D.VERSION";

        private static IWebProxy GetDefaultWebProxyFor(String url)
        {
            var result = WebRequest.GetSystemWebProxy();
            var uri = new Uri(url);
            var address = result.GetProxy(uri);

            if (address == uri)
                return null;

            return new WebProxy(address)
            {
                Credentials = CredentialCache.DefaultCredentials,
                BypassProxyOnLocal = true
            };
        }

        private static void Main(string[] args)
        {
            var folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var target = Path.Combine(folder, "paket.unity3d.exe");

            try
            {
                var latestVersion = Environment.GetEnvironmentVariable(PaketUnity3DVersionEnv) ?? ""; ;
                var ignorePrerelease = true;

                if (args.Length >= 1)
                {
                    if (args[0] == "prerelease")
                    {
                        ignorePrerelease = false;
                        latestVersion = "";
                        Console.WriteLine("Prerelease requested. Looking for latest prerelease.");
                    }
                    else
                    {
                        latestVersion = args[0];
                        Console.WriteLine("Version {0} requested.", latestVersion);
                    }
                }
                else if (!String.IsNullOrWhiteSpace(latestVersion))
                    Console.WriteLine("Version {0} requested.", latestVersion);
                else Console.WriteLine("No version specified. Downloading latest stable.");
                var localVersion = "";

                if (File.Exists(target))
                {
                    try
                    {
                        var fvi = FileVersionInfo.GetVersionInfo(target);
                        if (fvi.FileVersion != null)
                            localVersion = fvi.FileVersion;
                    }
                    catch (Exception)
                    {
                    }
                }

                if (latestVersion == "")
                {
                    using (var client = new WebClient())
                    {
                        const string releasesUrl = "https://github.com/wooga/Paket.Unity3D/releases";

                        client.Headers.Add("user-agent", "Paket.Unity3D.Bootstrapper");
                        client.UseDefaultCredentials = true;
                        client.Proxy = GetDefaultWebProxyFor(releasesUrl);

                        var data = client.DownloadString(releasesUrl);
                        var start = 0;
                        while (latestVersion == "")
                        {
                            start = data.IndexOf("Paket.Unity3D/tree/", start) + 19;
                            var end = data.IndexOf("\"", start);
                            latestVersion = data.Substring(start, end - start);
                            if (latestVersion.Contains("-") && ignorePrerelease)
                                latestVersion = "";
                        }
                    }
                }

                if (!localVersion.StartsWith(latestVersion))
                {
                    var url =
                        String.Format(
                            "https://github.com/wooga/Paket.Unity3D/releases/download/{0}/paket.unity3d.exe",
                            latestVersion);

                    Console.WriteLine("Starting download from {0}", url);

                    var request = (HttpWebRequest) WebRequest.Create(url);

                    request.UseDefaultCredentials = true;
                    request.Proxy = GetDefaultWebProxyFor(url);

                    request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                    using (var httpResponse = (HttpWebResponse) request.GetResponse())
                    {
                        using (var httpResponseStream = httpResponse.GetResponseStream())
                        {
                            const int bufferSize = 4096;
                            var buffer = new byte[bufferSize];
                            var bytesRead = 0;

                            using (var fileStream = File.Create(target))
                            {
                                while ((bytesRead = httpResponseStream.Read(buffer, 0, bufferSize)) != 0)
                                {
                                    fileStream.Write(buffer, 0, bytesRead);
                                }
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Paket.Unity3D.exe {0} is up to date.", localVersion);
                }
            }
            catch (Exception exn)
            {
                if (!File.Exists(target))
                    Environment.ExitCode = 1;
                var oldColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(exn.Message);
                Console.ForegroundColor = oldColor;
            }
        }
    }
}