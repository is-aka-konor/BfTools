using System.Text.Json;
using Bfmd.Core.Config;
using Bfmd.Core.Pipeline;
using Bfmd.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using System.Linq;

namespace Bfmd.IntegrationTests;

public class TalentsIntegrationTests
{
    [Fact]
    public void Pipeline_ShouldExtractTalentsAndWriteIndex_WhenRunningTalentsStep()
    {
        var (outputRoot, code) = RunTalentsPipeline();
        Assert.Equal(0, code);

        var dir = Path.Combine(outputRoot, "data", "talents");
        Assert.True(Directory.Exists(dir));
        var files = Directory.EnumerateFiles(dir, "*.json").ToList();
        Assert.NotEmpty(files);

        var arkanist = Path.Combine(dir, "arkanist.json");
        if (File.Exists(arkanist))
        {
            var json = File.ReadAllText(arkanist);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            Assert.Equal("talent", root.GetProperty("type").GetString());
            Assert.Equal("arkanist", root.GetProperty("slug").GetString());
            Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("name").GetString()));
            Assert.True(root.TryGetProperty("description", out var descProp));
            Assert.Contains("Арканист", descProp.GetString());

            // Verify src.hash populated with a SHA-256 hex string
            var src = root.GetProperty("src");
            var hash = src.GetProperty("hash").GetString();
            Assert.False(string.IsNullOrWhiteSpace(hash));
            Assert.Equal(64, hash!.Length);
            Assert.True(hash.All(ch => Uri.IsHexDigit(ch)));
        }
    }

    [Fact]
    public void Pipeline_ShouldParseTalentRequirementAndFeatures_WhenRunningTalentsStep()
    {
        var (outputRoot, code) = RunTalentsPipeline();
        Assert.Equal(0, code);

        var dir = Path.Combine(outputRoot, "data", "talents");
        var heavyWeaponPath = FindTalentJson(dir, "Мастерство Тяжелого Оружия");
        using (var doc = JsonDocument.Parse(File.ReadAllText(heavyWeaponPath)))
        {
            var root = doc.RootElement;
            Assert.Equal("Персонаж 4-го уровня или выше", root.GetProperty("requirement").GetString());

            var features = root.GetProperty("talentFeatures");
            var intro = "Вы обладаете большим мастерством владения двуручным оружием. При владении рукопашным оружием со свойством \"Тяжелое\" двумя руками вы получаете следующие преимущества:";
            Assert.True(features.TryGetProperty(intro, out var introList));
            Assert.Equal(1, introList.GetArrayLength());
            Assert.Equal("Когда вы совершаете критическое попадание, вы можете совершить одну дополнительную атаку рукопашным оружием в рамках того же действия Атака.", introList[0].GetString());

            var followUp = "Кроме того, ваши атаки становятся сокрушительными. Бонусным действием в каждый свой ход вы можете дать себе одно из следующих преимуществ:";
            Assert.True(features.TryGetProperty(followUp, out var followUpList));
            Assert.Equal(2, followUpList.GetArrayLength());
            var followUpItems = followUpList.EnumerateArray().Select(x => x.GetString()).ToList();
            Assert.Contains("Следующая рукопашная атака, которую вы совершаете Тяжелым оружием, игнорирует сопротивление урону вашего оружия.", followUpItems);
            Assert.Contains("Ваша следующая рукопашная атака Тяжелым оружием имеет штраф -5 к броску атаки. Если атака попадает, она наносит дополнительный урон, равный половине вашего значения СИЛ.", followUpItems);
        }

        var chargePath = FindTalentJson(dir, "Яростный Натиск");
        using (var doc = JsonDocument.Parse(File.ReadAllText(chargePath)))
        {
            var root = doc.RootElement;
            Assert.True(string.IsNullOrWhiteSpace(root.GetProperty("requirement").GetString()));

            var features = root.GetProperty("talentFeatures");
            var intro = "Вы научились использовать любое преимущество и врываться во врагов. Каждый раз, когда вы перемещаетесь на 20 футов по прямой линии к существу и попадаете по нему атакой рукопашным оружием или безоружным ударом, атака получает два из следующих преимуществ по вашему выбору:";
            Assert.True(features.TryGetProperty(intro, out var introList));
            Assert.Equal(4, introList.GetArrayLength());
            var firstItem = introList[0].GetString();
            Assert.Equal("Атака наносит дополнительный урон типа урона вашего оружия, равный вашему БМ.", firstItem);
        }
    }

    private static (string OutputRoot, int Code) RunTalentsPipeline()
    {
        var cwd = Directory.GetCurrentDirectory();
        var inputRoot = cwd; // inputs are copied under test bin dir via csproj
        var outputRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var configRoot = Path.Combine(cwd, "config");

        var sources = new YamlLoader<SourcesConfig>().Load(Path.Combine(configRoot, "sources.yaml"));
        var pipe = new YamlLoader<PipelineConfig>().Load(Path.Combine(configRoot, "pipeline.yaml"));
        pipe.Steps = pipe.Steps.Where(s => string.Equals(s.Type, "talents", StringComparison.OrdinalIgnoreCase)).ToList();

        var runner = new PipelineRunner(NullLogger.Instance, new FileMarkdownLoader(), p => new YamlLoader<MappingConfig>().Load(p),
        [
            ("talents", new Extractors.TalentsExtractor())
        ]);

        var code = runner.Run(pipe, sources, (inputRoot, outputRoot, configRoot));
        return (outputRoot, code);
    }

    private static string FindTalentJson(string dir, string name)
    {
        foreach (var file in Directory.EnumerateFiles(dir, "*.json"))
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(file));
            if (doc.RootElement.TryGetProperty("name", out var nameProp) &&
                string.Equals(nameProp.GetString(), name, StringComparison.Ordinal))
            {
                return file;
            }
        }

        throw new InvalidOperationException($"Talent '{name}' not found in '{dir}'.");
    }
}
