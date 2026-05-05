using System.Threading.Tasks;
using BTCPayServer.Tests;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;
using static Microsoft.Playwright.Assertions;

namespace BTCPayServer.Plugins.OpenPatron.Tests;

[Trait("Playwright", "Playwright")]
[Collection(nameof(NonParallelizableCollectionDefinition))]
public class PlaywrightTests(ITestOutputHelper helper) : UnitTestBase(helper)
{
    // ── Template picker ──

    [Fact]
    public async Task NewAppShowsTemplatePicker()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();
        await s.CreateApp("OpenPatron");

        await Expect(s.Page.Locator("button[name='template'][value='personal']")).ToBeVisibleAsync();
        await Expect(s.Page.Locator("button[name='template'][value='project']")).ToBeVisibleAsync();
        await Expect(s.Page.Locator("button[name='template'][value='empty']")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task ProjectTemplatePopulatesBlocks()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();
        await s.CreateApp("OpenPatron");

        await s.Page.Locator("button[name='template'][value='project']").ClickAsync();
        await s.FindAlertMessage(partialText: "Template applied");

        await Expect(s.Page.Locator("#blockList .block-row")).ToHaveCountAsync(6);
        await Expect(s.Page.Locator("button[name='template']")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task PersonalTemplatePopulatesBlocks()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();
        await s.CreateApp("OpenPatron");

        await s.Page.Locator("button[name='template'][value='personal']").ClickAsync();
        await s.FindAlertMessage(partialText: "Template applied");

        await Expect(s.Page.Locator("#blockList .block-row")).ToHaveCountAsync(7);
        await Expect(s.Page.Locator("button[name='template']")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task EmptyTemplateShowsNoBlocks()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();
        await s.CreateApp("OpenPatron");

        await s.Page.Locator("button[name='template'][value='empty']").ClickAsync();
        await s.FindAlertMessage(partialText: "Template applied");

        await Expect(s.Page.Locator("#blockList .block-row")).ToHaveCountAsync(0);
        await Expect(s.Page.Locator("button[name='template']")).ToHaveCountAsync(0);
    }

    // ── Block add / remove ──

    [Fact]
    public async Task CanAddBlockFromPicker()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();
        await s.CreateApp("OpenPatron");

        await s.Page.Locator("button[name='template'][value='project']").ClickAsync();
        await s.FindAlertMessage(partialText: "Template applied");

        var initial = await s.Page.Locator("#blockList .block-row").CountAsync();
        await s.Page.Locator(".block-picker-item[data-block-type='sponsor-wall']").ClickAsync();
        await Expect(s.Page.Locator("#blockList .block-row")).ToHaveCountAsync(initial + 1);
    }

    [Fact]
    public async Task CanRemoveBlockAndSave()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();
        await s.CreateApp("OpenPatron");

        await s.Page.Locator("button[name='template'][value='project']").ClickAsync();
        await s.FindAlertMessage(partialText: "Template applied");

        // Add a block first so we can remove it
        await s.Page.Locator(".block-picker-item[data-block-type='sponsor-wall']").ClickAsync();

        var initial = await s.Page.Locator("#blockList .block-row").CountAsync();
        await s.Page.Locator("#blockList .block-row").First.Locator(".block-remove-btn").ClickAsync();
        await Expect(s.Page.Locator("#blockList .block-row")).ToHaveCountAsync(initial - 1);

        await s.Page.Locator("#saveBtn").ClickAsync();
        await s.FindAlertMessage(partialText: "updated");
        await Expect(s.Page.Locator("#blockList .block-row")).ToHaveCountAsync(initial - 1);
    }

    // ── Per-block inline config ──

    [Fact]
    public async Task CanExpandBlockAndEditSettings()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();
        await s.CreateApp("OpenPatron");

        await s.Page.Locator("button[name='template'][value='project']").ClickAsync();
        await s.FindAlertMessage(partialText: "Template applied");

        // Click the first block header to expand it
        await s.Page.Locator("#blockList .block-header").First.ClickAsync();

        // The form fields should now be visible inside the expanded block
        var firstBlock = s.Page.Locator("#blockList .block-row").First;
        await Expect(firstBlock.Locator(".block-field").First).ToBeVisibleAsync();
    }

    [Fact]
    public async Task BlockSettingsPersistAfterSave()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();
        await s.CreateApp("OpenPatron");

        await s.Page.Locator("button[name='template'][value='project']").ClickAsync();
        await s.FindAlertMessage(partialText: "Template applied");

        // Expand the project-hero block (first block in template)
        await s.Page.Locator("#blockList .block-header").First.ClickAsync();

        // Fill in the title field
        var titleField = s.Page.Locator("#blockList .block-row").First.Locator(".block-field[data-key='title']");
        await titleField.ClearAsync();
        await titleField.FillAsync("My Great Project");

        // Save
        await s.Page.Locator("#saveBtn").ClickAsync();
        await s.FindAlertMessage(partialText: "updated");

        // Expand the block again and verify the value persisted
        await s.Page.Locator("#blockList .block-header").First.ClickAsync();
        var titleFieldAfter = s.Page.Locator("#blockList .block-row").First.Locator(".block-field[data-key='title']");
        await Expect(titleFieldAfter).ToHaveValueAsync("My Great Project");
    }

    // ── Theme ──

    [Fact]
    public async Task ThemeSettingsPersist()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();
        await s.CreateApp("OpenPatron");

        await s.Page.Locator("button[name='template'][value='empty']").ClickAsync();
        await s.FindAlertMessage(partialText: "Template applied");

        await s.Page.Locator("[name='ThemeAccentColor']").ClearAsync();
        await s.Page.Locator("[name='ThemeAccentColor']").FillAsync("#ff5500");
        await s.Page.Locator("[name='ThemeBorderRadius']").SelectOptionAsync("2rem");

        await s.Page.Locator("#saveBtn").ClickAsync();
        await s.FindAlertMessage(partialText: "updated");

        await Expect(s.Page.Locator("[name='ThemeAccentColor']")).ToHaveValueAsync("#ff5500");
        await Expect(s.Page.Locator("[name='ThemeBorderRadius']")).ToHaveValueAsync("2rem");
    }

    // ── Public page ──

    [Fact]
    public async Task PublicPageRendersBlocksInOrder()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();
        var (_, appId) = await s.CreateApp("OpenPatron");

        await s.Page.Locator("button[name='template'][value='project']").ClickAsync();
        await s.FindAlertMessage(partialText: "Template applied");

        // Expand project-hero and set a title so it renders
        await s.Page.Locator("#blockList .block-header").First.ClickAsync();
        var titleField = s.Page.Locator("#blockList .block-row").First.Locator(".block-field[data-key='title']");
        await titleField.ClearAsync();
        await titleField.FillAsync("Test Project");

        // Expand description block and add content
        await s.Page.Locator("#blockList .block-header").Nth(2).ClickAsync();
        var contentField = s.Page.Locator("#blockList .block-row").Nth(2).Locator(".block-field[data-key='content']");
        await contentField.FillAsync("A great project");

        await s.Page.Locator("[name='Visibility']").SelectOptionAsync("1");
        await s.Page.Locator("#saveBtn").ClickAsync();
        await s.FindAlertMessage(partialText: "updated");

        await s.GoToUrl($"/apps/{appId}/openpatron");

        var blocks = s.Page.Locator("[data-block-type]");
        var count = await blocks.CountAsync();
        Assert.True(count > 0);
        Assert.Equal("project-hero", await blocks.First.GetAttributeAsync("data-block-type"));
    }

    [Fact]
    public async Task PublicPageAppliesTheme()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();
        var (_, appId) = await s.CreateApp("OpenPatron");

        await s.Page.Locator("button[name='template'][value='empty']").ClickAsync();
        await s.FindAlertMessage(partialText: "Template applied");

        await s.Page.Locator("[name='ThemeAccentColor']").ClearAsync();
        await s.Page.Locator("[name='ThemeAccentColor']").FillAsync("#ff5500");
        await s.Page.Locator("[name='Visibility']").SelectOptionAsync("1");
        await s.Page.Locator("#saveBtn").ClickAsync();
        await s.FindAlertMessage(partialText: "updated");

        await s.GoToUrl($"/apps/{appId}/openpatron");
        var content = await s.Page.ContentAsync();
        Assert.Contains("--op-accent: #ff5500", content);
    }

    [Fact]
    public async Task EmptyBlockListRendersGracefully()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();
        var (_, appId) = await s.CreateApp("OpenPatron");

        await s.Page.Locator("button[name='template'][value='empty']").ClickAsync();
        await s.FindAlertMessage(partialText: "Template applied");

        await s.Page.EvaluateAsync("() => { document.getElementById('pageLayoutJson').value = '[]'; }");
        await s.Page.Locator("[name='Visibility']").SelectOptionAsync("1");
        await s.Page.Locator("#saveBtn").ClickAsync();
        await s.FindAlertMessage(partialText: "updated");

        await s.GoToUrl($"/apps/{appId}/openpatron");
        await s.Page.AssertNoError();
        Assert.Contains("openpatron-page", await s.Page.ContentAsync());
    }

    // ── Block reorder ──

    [Fact]
    public async Task BlockReorderPersistsAfterSave()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();
        await s.CreateApp("OpenPatron");

        await s.Page.Locator("button[name='template'][value='project']").ClickAsync();
        await s.FindAlertMessage(partialText: "Template applied");

        var firstText = await s.Page.Locator("#blockList .block-row .fw-semibold").First.TextContentAsync();

        await s.Page.EvaluateAsync(@"() => {
            var input = document.getElementById('pageLayoutJson');
            var blocks = JSON.parse(input.value);
            var first = blocks.shift();
            blocks.push(first);
            input.value = JSON.stringify(blocks);
        }");

        await s.Page.Locator("#saveBtn").ClickAsync();
        await s.FindAlertMessage(partialText: "updated");

        var newFirstText = await s.Page.Locator("#blockList .block-row .fw-semibold").First.TextContentAsync();
        Assert.NotEqual(firstText, newFirstText);
    }

    // ── JSON mode / two-way sync ──

    [Fact]
    public async Task SwitchToJsonShowsCurrentBlocks()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();
        await s.CreateApp("OpenPatron");

        await s.Page.Locator("button[name='template'][value='project']").ClickAsync();
        await s.FindAlertMessage(partialText: "Template applied");

        await s.Page.Locator("#modeJsonBtn").ClickAsync();
        var json = await s.Page.Locator("#jsonEditorTextarea").InputValueAsync();
        Assert.Contains("Sections", json);
        Assert.Contains("project-hero", json);
    }

    [Fact]
    public async Task SwitchToJsonShowsLayout()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();
        await s.CreateApp("OpenPatron");

        await s.Page.Locator("button[name='template'][value='project']").ClickAsync();
        await s.FindAlertMessage(partialText: "Template applied");

        await s.Page.Locator("#modeJsonBtn").ClickAsync();
        var json = await s.Page.Locator("#jsonEditorTextarea").InputValueAsync();
        Assert.Contains("\"Layout\"", json);
    }

    [Fact]
    public async Task JsonModeThemeCardVisible()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();
        await s.CreateApp("OpenPatron");

        await s.Page.Locator("button[name='template'][value='empty']").ClickAsync();
        await s.FindAlertMessage(partialText: "Template applied");

        await s.Page.Locator("#modeJsonBtn").ClickAsync();
        await Expect(s.Page.Locator("#themeCard")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task JsonModeBlockPickerVisible()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();
        await s.CreateApp("OpenPatron");

        await s.Page.Locator("button[name='template'][value='empty']").ClickAsync();
        await s.FindAlertMessage(partialText: "Template applied");

        await s.Page.Locator("#modeJsonBtn").ClickAsync();
        await Expect(s.Page.Locator("#blockPickerCard")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task AddBlockInJsonModeUpdatesTextarea()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();
        await s.CreateApp("OpenPatron");

        await s.Page.Locator("button[name='template'][value='empty']").ClickAsync();
        await s.FindAlertMessage(partialText: "Template applied");

        await s.Page.Locator("#modeJsonBtn").ClickAsync();
        var before = await s.Page.Locator("#jsonEditorTextarea").InputValueAsync();
        Assert.DoesNotContain("sponsor-wall", before);

        await s.Page.Locator(".block-picker-item[data-block-type='sponsor-wall']").ClickAsync();

        var after = await s.Page.Locator("#jsonEditorTextarea").InputValueAsync();
        Assert.Contains("sponsor-wall", after);
    }

    [Fact]
    public async Task ChangeAccentColorInJsonModeUpdatesTextarea()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();
        await s.CreateApp("OpenPatron");

        await s.Page.Locator("button[name='template'][value='empty']").ClickAsync();
        await s.FindAlertMessage(partialText: "Template applied");

        await s.Page.Locator("#modeJsonBtn").ClickAsync();

        // Set a non-default accent color
        await s.Page.Locator("[name='ThemeAccentColor']").ClearAsync();
        await s.Page.Locator("[name='ThemeAccentColor']").FillAsync("#ff5500");
        // Dispatch input event to trigger sync
        await s.Page.Locator("[name='ThemeAccentColor']").DispatchEventAsync("input");

        var json = await s.Page.Locator("#jsonEditorTextarea").InputValueAsync();
        Assert.Contains("#ff5500", json);
        Assert.Contains("\"Theme\"", json);
    }

    [Fact]
    public async Task DefaultThemeNotInJson()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();
        await s.CreateApp("OpenPatron");

        await s.Page.Locator("button[name='template'][value='empty']").ClickAsync();
        await s.FindAlertMessage(partialText: "Template applied");

        await s.Page.Locator("#modeJsonBtn").ClickAsync();

        var json = await s.Page.Locator("#jsonEditorTextarea").InputValueAsync();
        Assert.DoesNotContain("\"Theme\"", json);
    }

    [Fact]
    public async Task ChangeBorderRadiusInJsonModeUpdatesTextarea()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();
        await s.CreateApp("OpenPatron");

        await s.Page.Locator("button[name='template'][value='empty']").ClickAsync();
        await s.FindAlertMessage(partialText: "Template applied");

        await s.Page.Locator("#modeJsonBtn").ClickAsync();

        await s.Page.Locator("[name='ThemeBorderRadius']").SelectOptionAsync("2rem");

        var json = await s.Page.Locator("#jsonEditorTextarea").InputValueAsync();
        Assert.Contains("2rem", json);
        Assert.Contains("\"Theme\"", json);
    }

    [Fact]
    public async Task EditJsonTextareaUpdatesThemeForm()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();
        await s.CreateApp("OpenPatron");

        await s.Page.Locator("button[name='template'][value='empty']").ClickAsync();
        await s.FindAlertMessage(partialText: "Template applied");

        await s.Page.Locator("#modeJsonBtn").ClickAsync();

        // Set JSON with custom theme
        await s.Page.Locator("#jsonEditorTextarea").FillAsync(@"{
  ""Layout"": ""single"",
  ""Sections"": [{ ""Blocks"": [] }],
  ""Theme"": { ""AccentColor"": ""#00ff00"", ""BorderRadius"": ""2rem"" }
}");
        await s.Page.Locator("#jsonEditorTextarea").DispatchEventAsync("input");

        await Expect(s.Page.Locator("[name='ThemeAccentColor']")).ToHaveValueAsync("#00ff00");
        await Expect(s.Page.Locator("[name='ThemeBorderRadius']")).ToHaveValueAsync("2rem");
    }

    [Fact]
    public async Task EditJsonTextareaAndSwitchToVisualReflectsChanges()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();
        await s.CreateApp("OpenPatron");

        await s.Page.Locator("button[name='template'][value='empty']").ClickAsync();
        await s.FindAlertMessage(partialText: "Template applied");

        await s.Page.Locator("#modeJsonBtn").ClickAsync();

        // Add a block via JSON
        await s.Page.Locator("#jsonEditorTextarea").FillAsync(@"{
  ""Layout"": ""single"",
  ""Sections"": [{ ""Blocks"": [{ ""Type"": ""sponsor-wall"", ""Settings"": {} }] }]
}");

        await s.Page.Locator("#modeVisualBtn").ClickAsync();

        await Expect(s.Page.Locator("#blockList .block-row")).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task JsonModeRemoveBlockUpdatesTextarea()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();
        await s.CreateApp("OpenPatron");

        await s.Page.Locator("button[name='template'][value='project']").ClickAsync();
        await s.FindAlertMessage(partialText: "Template applied");

        await s.Page.Locator("#modeJsonBtn").ClickAsync();

        var firstType = await s.Page.Locator("#blockList .block-row .fw-semibold").First.TextContentAsync();
        await s.Page.Locator("#blockList .block-row .block-remove-btn").First.ClickAsync();

        var json = await s.Page.Locator("#jsonEditorTextarea").InputValueAsync();
        Assert.DoesNotContain(firstType!.Trim().ToLower().Replace(" ", "-"), json);
    }

    [Fact]
    public async Task SaveFromJsonModePersistsCorrectly()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();
        await s.CreateApp("OpenPatron");

        await s.Page.Locator("button[name='template'][value='empty']").ClickAsync();
        await s.FindAlertMessage(partialText: "Template applied");

        await s.Page.Locator("#modeJsonBtn").ClickAsync();

        // Set up a custom configuration via JSON
        await s.Page.Locator("#jsonEditorTextarea").FillAsync(@"{
  ""Layout"": ""single"",
  ""Sections"": [{ ""Blocks"": [{ ""Type"": ""sponsor-wall"", ""Settings"": {} }, { ""Type"": ""description"", ""Settings"": {} }] }]
}");
        await s.Page.Locator("#jsonEditorTextarea").DispatchEventAsync("input");

        await s.Page.Locator("#saveBtn").ClickAsync();
        await s.FindAlertMessage(partialText: "updated");

        // Verify blocks persisted
        await Expect(s.Page.Locator("#blockList .block-row")).ToHaveCountAsync(2);
    }

    [Fact]
    public async Task SaveFromJsonModeWithThemePersists()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();
        await s.CreateApp("OpenPatron");

        await s.Page.Locator("button[name='template'][value='empty']").ClickAsync();
        await s.FindAlertMessage(partialText: "Template applied");

        await s.Page.Locator("#modeJsonBtn").ClickAsync();

        await s.Page.Locator("#jsonEditorTextarea").FillAsync(@"{
  ""Layout"": ""single"",
  ""Sections"": [{ ""Blocks"": [] }],
  ""Theme"": { ""AccentColor"": ""#abcdef"" }
}");
        await s.Page.Locator("#jsonEditorTextarea").DispatchEventAsync("input");

        await s.Page.Locator("#saveBtn").ClickAsync();
        await s.FindAlertMessage(partialText: "updated");

        await Expect(s.Page.Locator("[name='ThemeAccentColor']")).ToHaveValueAsync("#abcdef");
    }

    [Fact]
    public async Task ChangeLayoutPresetInJsonModeUpdatesTextarea()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();
        await s.CreateApp("OpenPatron");

        await s.Page.Locator("button[name='template'][value='project']").ClickAsync();
        await s.FindAlertMessage(partialText: "Template applied");

        await s.Page.Locator("#modeJsonBtn").ClickAsync();

        // Change layout to sidebar-left
        await s.Page.Locator(".layout-preset-btn[data-preset='sidebar-left']").ClickAsync();

        var json = await s.Page.Locator("#jsonEditorTextarea").InputValueAsync();
        Assert.Contains("sidebar-left", json);
    }

    [Fact]
    public async Task SwitchToJsonAndBackPreservesState()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();
        await s.CreateApp("OpenPatron");

        await s.Page.Locator("button[name='template'][value='project']").ClickAsync();
        await s.FindAlertMessage(partialText: "Template applied");

        var blockCount = await s.Page.Locator("#blockList .block-row").CountAsync();

        // Switch to JSON and back
        await s.Page.Locator("#modeJsonBtn").ClickAsync();
        await s.Page.Locator("#modeVisualBtn").ClickAsync();

        await Expect(s.Page.Locator("#blockList .block-row")).ToHaveCountAsync(blockCount);
    }

    [Fact]
    public async Task InvalidJsonShowsError()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();
        await s.CreateApp("OpenPatron");

        await s.Page.Locator("button[name='template'][value='empty']").ClickAsync();
        await s.FindAlertMessage(partialText: "Template applied");

        await s.Page.Locator("#modeJsonBtn").ClickAsync();

        // Set invalid JSON
        await s.Page.Locator("#jsonEditorTextarea").FillAsync("{ broken json");

        // Try to switch back to visual – should show error
        await s.Page.Locator("#modeVisualBtn").ClickAsync();
        await Expect(s.Page.Locator("#jsonEditorError")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task ChangeSecondaryColorInJsonModeUpdatesTextarea()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();
        await s.CreateApp("OpenPatron");

        await s.Page.Locator("button[name='template'][value='empty']").ClickAsync();
        await s.FindAlertMessage(partialText: "Template applied");

        await s.Page.Locator("#modeJsonBtn").ClickAsync();

        await s.Page.Locator("[name='ThemeSecondaryColor']").ClearAsync();
        await s.Page.Locator("[name='ThemeSecondaryColor']").FillAsync("#112233");
        await s.Page.Locator("[name='ThemeSecondaryColor']").DispatchEventAsync("input");

        var json = await s.Page.Locator("#jsonEditorTextarea").InputValueAsync();
        Assert.Contains("#112233", json);
    }

    [Fact]
    public async Task ChangeShadowStyleInJsonModeUpdatesTextarea()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();
        await s.CreateApp("OpenPatron");

        await s.Page.Locator("button[name='template'][value='empty']").ClickAsync();
        await s.FindAlertMessage(partialText: "Template applied");

        await s.Page.Locator("#modeJsonBtn").ClickAsync();

        await s.Page.Locator("[name='ThemeShadowStyle']").SelectOptionAsync("lg");

        var json = await s.Page.Locator("#jsonEditorTextarea").InputValueAsync();
        Assert.Contains("\"ShadowStyle\": \"lg\"", json);
    }

    [Fact]
    public async Task MultipleBlocksAddedInJsonModeAllAppearInTextarea()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();
        await s.CreateApp("OpenPatron");

        await s.Page.Locator("button[name='template'][value='empty']").ClickAsync();
        await s.FindAlertMessage(partialText: "Template applied");

        await s.Page.Locator("#modeJsonBtn").ClickAsync();

        await s.Page.Locator(".block-picker-item[data-block-type='sponsor-wall']").ClickAsync();
        await s.Page.Locator(".block-picker-item[data-block-type='description']").ClickAsync();

        var json = await s.Page.Locator("#jsonEditorTextarea").InputValueAsync();
        Assert.Contains("sponsor-wall", json);
        Assert.Contains("description", json);
    }

    [Fact]
    public async Task JsonTextareaLayoutChangeReflectsInVisual()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();
        await s.CreateApp("OpenPatron");

        await s.Page.Locator("button[name='template'][value='empty']").ClickAsync();
        await s.FindAlertMessage(partialText: "Template applied");

        await s.Page.Locator("#modeJsonBtn").ClickAsync();

        // Set layout to sidebar-left via JSON
        await s.Page.Locator("#jsonEditorTextarea").FillAsync(@"{
  ""Layout"": ""sidebar-left"",
  ""Sections"": [{ ""Blocks"": [] }, { ""Blocks"": [] }]
}");

        await s.Page.Locator("#modeVisualBtn").ClickAsync();

        // The layout preset hidden input should reflect sidebar-left
        var layout = await s.Page.Locator("[name='LayoutPreset']").InputValueAsync();
        Assert.Equal("sidebar-left", layout);
    }
}
