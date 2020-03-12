using Octokit;
using System;
using System.IO;
using System.Threading.Tasks;


namespace KInvRelease
{

    public class ReleaseManager
    {
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

                var newRelease = new NewRelease("v" + name);
                newRelease.Name = name;
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
                            FileName = file,
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
