using Microsoft.AspNetCore.Mvc;
using Octokit;
using System.Text;

namespace EPROM_API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EpromController : ControllerBase
    {
        public class GitHubApiService
        {
            private readonly GitHubClient _gitHubClient;

            public GitHubApiService()
            {
                _gitHubClient = new GitHubClient(new ProductHeaderValue("YourAppName"));
            }

            public async Task<string> GetFileContent(string owner, string repo, string filePath)
            {
                try
                {
                    var fileBytes = await _gitHubClient.Repository.Content.GetRawContent(owner, repo, filePath);
                    var fileContent = Encoding.UTF8.GetString(fileBytes);
                    return fileContent;
                }
                catch (NotFoundException)
                {
                    throw new Exception("File not found.");
                }
                catch (Exception ex)
                {
                    throw new Exception("An error occurred while fetching the file content.", ex);
                }
            }
        }

        [HttpGet(Name = "GetGameInfo")]
        public async Task<List<RomInfo>> Get(string mameGameDriver)
        {
            var gitHubApiService = new GitHubApiService();
            var owner = "mamedev";
            var repo = "historic-mame";
            var filePath = "src/mame/drivers/" + mameGameDriver + ".c";

            try
            {
                var fileContent = await gitHubApiService.GetFileContent(owner, repo, filePath);

                var romParser = new RomParser();
                var romInfoList = romParser.ParseRomInfo(fileContent);

                return romInfoList;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return null;
            }
        }

        public class RomParser
        {
            public List<RomInfo> ParseRomInfo(string inputString)
            {
                var gameInfo = ParseGameInfo(inputString);
                var romInfos = new List<RomInfo>();
                var lines = inputString.Split('\n');

                var currentRegion = "";
                RomInfo currentRomInfo = null;

                foreach (var line in lines)
                {
                    if (line.Contains("ROM_START"))
                    {
                        // Start a new RomInfo for each game
                        currentRomInfo = new RomInfo();
                        var gameName = line.Split(new[] { "(", ")" }, StringSplitOptions.RemoveEmptyEntries)[1].Trim();
                        currentRomInfo.GameName = gameInfo.ContainsKey(gameName) ? gameInfo[gameName].FullGameName : gameName;
                        currentRomInfo.GameYear = gameInfo.ContainsKey(gameName) ? gameInfo[gameName].GameYear : string.Empty;
                        currentRomInfo.MameSetName = gameName;
                        romInfos.Add(currentRomInfo);
                    }
                    else if (line.Contains("ROM_REGION"))
                    {
                        var parts = line.Split(new[] { "(", ")", ",", "0x", " " }, StringSplitOptions.RemoveEmptyEntries);
                        currentRegion = parts[2];
                    }
                    else if (line.Contains("ROM_LOAD") && currentRomInfo != null)
                    {
                        var rom = new Rom();
                        var parts = line.Split(new[] { "\"", ",", "(", ")", "0x", " ", "\t" }, StringSplitOptions.RemoveEmptyEntries);

                        rom.RomName = parts[1];
                        rom.RomType = LookupEpromType(int.Parse(parts[3], System.Globalization.NumberStyles.HexNumber) / 1024);
                        rom.RomSize = $"({int.Parse(parts[3], System.Globalization.NumberStyles.HexNumber) / 1024} KB)";
                        rom.RomRegion = currentRegion;

                        currentRomInfo.Roms.Add(rom);
                    }
                }

                return romInfos;
            }

            static Dictionary<string, (string FullGameName, string GameYear)> ParseGameInfo(string inputString)
            {
                var gameInfo = new Dictionary<string, (string FullGameName, string GameYear)>();

                var lines = inputString.Split('\n');

                foreach (var line in lines)
                {
                    if (line.Contains("GAME("))
                    {
                        var parts = line.Split(new[] { ",", "\"" }, StringSplitOptions.RemoveEmptyEntries);
                        var gameName = parts[1].Trim();
                        var fullGameName = parts.Length >= 11 ? parts[10].Trim() : string.Empty;
                        var gameYear = parts.Length >= 1 ? parts[0].Trim().Remove(0, 6) : string.Empty; //Fix proper line.Split so .Remove(0, 6) isnt needed.

                        if (!gameInfo.ContainsKey(gameName) && !string.IsNullOrEmpty(fullGameName))
                        {
                            gameInfo.Add(gameName, (fullGameName, gameYear));
                        }
                    }
                }

                return gameInfo;
            }

            private string LookupEpromType(int romSize)
            {
                var epromTypes = new Dictionary<int, string>
                {
                    { 1, "2708" },
                    { 2, "2716" },
                    { 4, "2732" },
                    { 8, "2764" },
                    { 16, "27128" },
                    { 32, "27256" },
                    { 64, "27512" },
                    { 128, "27010, 27C1001, 27C101" },
                    { 256, "27020, 27C2001, 27C201" },
                    { 512, "27040, 27C4001, 27C401" },
                    { 1024, "27080, 27C801" },
                    // Todo add 16bit roms
                    //{ 128, "27C1024, 27C210" },
                    //{ 256, "27C2048, 27C220" },
                    //{ 512, "27C4096, 27C240" },
                    //{ 512, "27C400" },
                    //{ 1024, "27C800" },
                    // Todo add 16bit roms
                    { 2148, "27C160" },
                    { 4096, "27C322" }
                };

                // Check if the exact value exists in the lookup table
                if (epromTypes.ContainsKey(romSize))
                {
                    return epromTypes[romSize];
                }

                // Find the next highest value
                var nextHighestSize = epromTypes.Keys.Where(key => key > romSize).DefaultIfEmpty(epromTypes.Keys.Max()).Min();
                return epromTypes[nextHighestSize];
            }
        }
    }
}
