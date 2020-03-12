using Octokit;
using System;
using System.IO;
using System.Threading.Tasks;

namespace KInvRelease
{
    class Program
    {        
        static void Main(string[] args)
        {
            string token = string.Empty, user = string.Empty, repo = string.Empty;

            string name = string.Empty, description = "kInv release", assetDir = string.Empty;

            foreach(var arg in args)
            {
                if (arg.StartsWith("--token="))
                {
                    token = arg.Remove(0, "--token=".Length);
                }
                else if (arg.StartsWith("--user="))
                {
                    user = arg.Remove(0, "--user=".Length);
                }
                else if (arg.StartsWith("--repo="))
                {
                    repo = arg.Remove(0, "--repo=".Length);
                }
                else if (arg.StartsWith("--name="))
                {
                    name = arg.Remove(0, "--name=".Length);
                }
                else if (arg.StartsWith("--desc="))
                {
                    description = arg.Remove(0, "--desc=".Length);
                }
                else if (arg.StartsWith("--asset-dir="))
                {
                    assetDir = arg.Remove(0, "--asset-dir=".Length);
                }
            }

            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(repo))
            {
                Console.Error.Write("credentials missing");
                return;
            }

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(assetDir))
            {
                Console.Error.Write("release name or asset path is empty");
                return;
            }

            if (!Directory.Exists(assetDir))
            {
                Console.Error.Write("asset path does not exist");
                return;
            }

            Task.Run(async () =>
            {
                var gh = new ReleaseManager(token, user, repo);
                await gh.Create(name, description, assetDir);
            }).Wait();
        }
    }

    
}
