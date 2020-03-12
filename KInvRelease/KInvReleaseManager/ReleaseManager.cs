using Octokit;
using System;
using System.IO;
using System.Threading.Tasks;


namespace KInvReleaseManager
{

    public class ReleaseManager
    {
        public static void ParseAndRun(string[] args)
        {
            string token = string.Empty, user = string.Empty, repo = string.Empty;

            string name = string.Empty, description = "kInv release", assetDir = string.Empty;

            foreach (var arg in args)
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

        public string ghToken { get; private set; }
        public string ghUser { get; private set; }
        public string ghRepo { get; private set; }

        public ReleaseManager(string token, string user, string repo)
        {
            ghToken = token; ghUser = user; ghRepo = repo;
        }

        public async Task Create(string name, string description, string assetDir)
        {
            int createdReleaseId = -1;
            var client = new GitHubClient(new ProductHeaderValue("kinv-release-app"));
            try
            {
                var tokenAuth = new Credentials(ghToken); // NOTE: not real token
                client.Credentials = tokenAuth;

                var newRelease = new NewRelease(name);
                newRelease.Name = "v" + name;
                newRelease.Body = description;
                newRelease.Draft = false;
                newRelease.Prerelease = false;

                var result = await client.Repository.Release.Create(ghUser, ghRepo, newRelease);
                Console.WriteLine("Created release id {0}", result.Id);
                createdReleaseId = result.Id;
                var release = await client.Repository.Release.Get(ghUser, ghRepo, result.Id);

                var files = Directory.GetFiles(assetDir);
                foreach (var file in files)
                {
                    using (var contents = File.OpenRead(file))
                    {
                        var assetUpload = new ReleaseAssetUpload()
                        {
                            FileName = Path.GetFileName(file),
                            ContentType = file.EndsWith(".exe") ? "application/binary" : "application/zip",
                            RawData = contents
                        };

                        var asset = await client.Repository.Release.UploadAsset(release, assetUpload);
                    }
                }
            }
            catch (Exception exc)
            {
                // if created and failed, delete it
                Console.Error.Write("Failed to create release: " + exc.Message);
                if (createdReleaseId != -1)
                {
                    try
                    {
                        await client.Repository.Release.Delete(ghUser, ghRepo, createdReleaseId);
                    }
                    catch (Exception exc2)
                    {
                        Console.Error.Write("Failed to delete release: " + exc2.Message);
                    }
                }
            }
        }
    }
}
