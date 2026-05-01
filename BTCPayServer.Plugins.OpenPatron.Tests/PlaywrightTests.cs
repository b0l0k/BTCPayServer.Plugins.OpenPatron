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

        await Expect(s.Page.Locator("label[for='pageTypePersonal']")).ToBeVisibleAsync();
        await Expect(s.Page.Locator("label[for='pageTypeProject']")).ToBeVisibleAsync();
        await Expect(s.Page.Locator("#blockList")).Not.ToBeVisibleAsync();
    }

    [Fact]
    public async Task ContinueWithProjectShowsBlockEditor()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();
        await s.CreateApp("OpenPatron");

        await s.Page.Locator("button[type='submit']:has-text('Continue')").ClickAsync();
        await s.FindAlertMessage(partialText: "Template selected");

        await Expect(s.Page.Locator("#blockList")).ToBeVisibleAsync();
        await Expect(s.Page.Locator(".sticky-header .badge")).ToContainTextAsync("Project");
        await Expect(s.Page.Locator("#blockList .block-row")).ToHaveCountAsync(6);
    }

    [Fact]
    public async Task ContinueWithPersonalShowsBlockEditor()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();
        await s.CreateApp("OpenPatron");

        await s.Page.Locator("label[for='pageTypePersonal']").ClickAsync();
        await s.Page.Locator("button[type='submit']:has-text('Continue')").ClickAsync();
        await s.FindAlertMessage(partialText: "Template selected");

        await Expect(s.Page.Locator(".sticky-header .badge")).ToContainTextAsync("Personal");
        await Expect(s.Page.Locator("#blockList .block-row")).ToHaveCountAsync(7);
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
        await s.Page.Locator("button[type='submit']:has-text('Continue')").ClickAsync();
        await s.FindAlertMessage(partialText: "Template selected");

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
        await s.Page.Locator("button[type='submit']:has-text('Continue')").ClickAsync();
        await s.FindAlertMessage(partialText: "Template selected");

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
        await s.Page.Locator("button[type='submit']:has-text('Continue')").ClickAsync();
        await s.FindAlertMessage(partialText: "Template selected");

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
        await s.Page.Locator("button[type='submit']:has-text('Continue')").ClickAsync();
        await s.FindAlertMessage(partialText: "Template selected");

        // Expand the project-hero block (first block in Project template)
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
        await s.Page.Locator("button[type='submit']:has-text('Continue')").ClickAsync();
        await s.FindAlertMessage(partialText: "Template selected");

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

        await s.Page.Locator("button[type='submit']:has-text('Continue')").ClickAsync();
        await s.FindAlertMessage(partialText: "Template selected");

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

        await s.Page.Locator("button[type='submit']:has-text('Continue')").ClickAsync();
        await s.FindAlertMessage(partialText: "Template selected");

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

        await s.Page.Locator("button[type='submit']:has-text('Continue')").ClickAsync();
        await s.FindAlertMessage(partialText: "Template selected");

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
        await s.Page.Locator("button[type='submit']:has-text('Continue')").ClickAsync();
        await s.FindAlertMessage(partialText: "Template selected");

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
}
