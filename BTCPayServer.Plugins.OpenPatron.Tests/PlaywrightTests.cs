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
    // ── New app starts with block editor ──

    [Fact]
    public async Task NewAppShowsBlockEditor()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();
        await s.CreateApp("OpenPatron");

        await Expect(s.Page.Locator("#sectionPreview")).ToBeVisibleAsync();
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

        // Add a block so we can expand it
        await s.Page.Locator(".block-picker-item[data-block-type='description']").ClickAsync();

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

        // Add a description block so we can edit it
        await s.Page.Locator(".block-picker-item[data-block-type='description']").ClickAsync();

        // Expand the description block
        await s.Page.Locator("#blockList .block-header").First.ClickAsync();

        // Fill in the heading field
        var titleField = s.Page.Locator("#blockList .block-row").First.Locator(".block-field[data-key='heading']");
        await titleField.ClearAsync();
        await titleField.FillAsync("My Great Project");

        // Save
        await s.Page.Locator("#saveBtn").ClickAsync();
        await s.FindAlertMessage(partialText: "updated");

        // Expand the block again and verify the value persisted
        await s.Page.Locator("#blockList .block-header").First.ClickAsync();
        var titleFieldAfter = s.Page.Locator("#blockList .block-row").First.Locator(".block-field[data-key='heading']");
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

        // Add a description block with content
        // Add a description block
        await s.Page.Locator(".block-picker-item[data-block-type='description']").ClickAsync();
        await s.Page.Locator("#blockList .block-header").First.ClickAsync();
        var headingField = s.Page.Locator("#blockList .block-row").First.Locator(".block-field[data-key='heading']");
        await headingField.ClearAsync();
        await headingField.FillAsync("Test Project");

        var contentField = s.Page.Locator("#blockList .block-row").First.Locator(".block-field[data-key='content']");
        await contentField.FillAsync("A great project");

        await s.Page.Locator("[name='Visibility']").SelectOptionAsync("1");
        await s.Page.Locator("#saveBtn").ClickAsync();
        await s.FindAlertMessage(partialText: "updated");

        await s.GoToUrl($"/apps/{appId}/openpatron");

        var blocks = s.Page.Locator("[data-block-type]");
        var count = await blocks.CountAsync();
        Assert.True(count > 0);
        Assert.Equal("description", await blocks.First.GetAttributeAsync("data-block-type"));
    }

    [Fact]
    public async Task PublicPageAppliesTheme()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();
        var (_, appId) = await s.CreateApp("OpenPatron");

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

        // Add two blocks so we can reorder
        await s.Page.Locator(".block-picker-item[data-block-type='description']").ClickAsync();
        await s.Page.Locator(".block-picker-item[data-block-type='sponsor-wall']").ClickAsync();

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
