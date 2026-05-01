using System;
using System.Text.RegularExpressions;
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
    // ── Template picker (replaces the old page type picker) ──

    /// <summary>
    /// After creating an OpenPatron app, the settings page should show the
    /// template picker with Personal and Project radio buttons.
    /// </summary>
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
        await Expect(s.Page.Locator("button[type='submit']:has-text('Continue')")).ToBeVisibleAsync();

        // Block editor should NOT be visible yet
        await Expect(s.Page.Locator("#blockList")).Not.ToBeVisibleAsync();
        await Expect(s.Page.Locator("[name='AppName']")).Not.ToBeVisibleAsync();
    }

    /// <summary>
    /// The default selection should be Project.
    /// </summary>
    [Fact]
    public async Task TemplatePickerDefaultsToProject()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();

        await s.CreateApp("OpenPatron");

        await Expect(s.Page.Locator("#pageTypeProject")).ToBeCheckedAsync();
        await Expect(s.Page.Locator("#pageTypePersonal")).Not.ToBeCheckedAsync();
    }

    /// <summary>
    /// Clicking a radio option should update the visual highlight.
    /// </summary>
    [Fact]
    public async Task TemplatePickerHighlightFollowsSelection()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();

        await s.CreateApp("OpenPatron");

        var projectLabel = s.Page.Locator("label[for='pageTypeProject']");
        var personalLabel = s.Page.Locator("label[for='pageTypePersonal']");
        var projectClasses = await projectLabel.GetAttributeAsync("class") ?? "";
        Assert.Contains("border-primary", projectClasses);

        await personalLabel.ClickAsync();
        await Expect(s.Page.Locator("#pageTypePersonal")).ToBeCheckedAsync();
        var personalClasses = await personalLabel.GetAttributeAsync("class") ?? "";
        Assert.Contains("border-primary", personalClasses);
    }

    /// <summary>
    /// Choosing Project template should redirect to block editor with default blocks.
    /// </summary>
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

        // Block editor should now be visible
        await Expect(s.Page.Locator("#blockList")).ToBeVisibleAsync();
        await Expect(s.Page.Locator("[name='AppName']")).ToBeVisibleAsync();

        // Badge should say "Project"
        await Expect(s.Page.Locator(".sticky-header .badge")).ToContainTextAsync("Project");

        // Template picker should be gone
        await Expect(s.Page.Locator("#pageTypePersonal")).Not.ToBeVisibleAsync();

        // Default project blocks should be loaded (6 blocks)
        await Expect(s.Page.Locator("#blockList .block-row")).ToHaveCountAsync(6);
    }

    /// <summary>
    /// Choosing Personal template should load personal default blocks.
    /// </summary>
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

        // Default personal blocks should be loaded (7 blocks)
        await Expect(s.Page.Locator("#blockList .block-row")).ToHaveCountAsync(7);
    }

    // ── Block editor: add, remove, reorder ──

    /// <summary>
    /// Clicking an item in the block picker should add a new block to the list.
    /// </summary>
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

        var initialCount = await s.Page.Locator("#blockList .block-row").CountAsync();

        // Add a "Sponsor Wall" block from the picker
        await s.Page.Locator(".block-picker-item[data-block-type='sponsor-wall']").ClickAsync();

        await Expect(s.Page.Locator("#blockList .block-row")).ToHaveCountAsync(initialCount + 1);
    }

    /// <summary>
    /// Clicking remove on a block should remove it, and saving should persist the removal.
    /// </summary>
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

        var initialCount = await s.Page.Locator("#blockList .block-row").CountAsync();
        Assert.True(initialCount > 0);

        // Remove the first block
        await s.Page.Locator("#blockList .block-row").First.Locator(".block-remove-btn").ClickAsync();
        await Expect(s.Page.Locator("#blockList .block-row")).ToHaveCountAsync(initialCount - 1);

        // Save
        await s.Page.Locator("#saveBtn").ClickAsync();
        await s.FindAlertMessage(partialText: "updated");

        // After reload, count should still be reduced
        await Expect(s.Page.Locator("#blockList .block-row")).ToHaveCountAsync(initialCount - 1);
    }

    /// <summary>
    /// Reordering blocks via JS simulation should persist after save.
    /// </summary>
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

        // Read the current first block's text
        var firstBlockText = await s.Page.Locator("#blockList .block-row").First.Locator(".fw-semibold").TextContentAsync();

        // Use JS to simulate moving the first block to the end via the data model
        await s.Page.EvaluateAsync(@"() => {
            var input = document.getElementById('pageLayoutJson');
            var blocks = JSON.parse(input.value);
            var first = blocks.shift();
            blocks.push(first);
            input.value = JSON.stringify(blocks);
            // Trigger a re-render by dispatching change
            input.dispatchEvent(new Event('change'));
        }");

        // Save
        await s.Page.Locator("#saveBtn").ClickAsync();
        await s.FindAlertMessage(partialText: "updated");

        // After reload, the first block should now be different
        var newFirstBlockText = await s.Page.Locator("#blockList .block-row").First.Locator(".fw-semibold").TextContentAsync();
        Assert.NotEqual(firstBlockText, newFirstBlockText);

        // The old first block should now be last
        var lastBlockText = await s.Page.Locator("#blockList .block-row").Last.Locator(".fw-semibold").TextContentAsync();
        Assert.Equal(firstBlockText!.Trim(), lastBlockText!.Trim());
    }

    // ── Settings persistence ──

    /// <summary>
    /// Core page content fields should save and persist after reload.
    /// </summary>
    [Fact]
    public async Task CanSaveAndReloadSettings()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();

        await s.CreateApp("OpenPatron");
        await s.Page.Locator("button[type='submit']:has-text('Continue')").ClickAsync();
        await s.FindAlertMessage(partialText: "Template selected");

        await s.Page.Locator("[name='AppName']").ClearAsync();
        await s.Page.Locator("[name='AppName']").FillAsync("My Test Project");
        await s.Page.Locator("[name='HeroTitle']").ClearAsync();
        await s.Page.Locator("[name='HeroTitle']").FillAsync("Support My Work");
        await s.Page.Locator("[name='DisplayName']").FillAsync("Jane Dev");
        await s.Page.Locator("[name='FundingGoal']").FillAsync("1000");

        await s.Page.Locator("#saveBtn").ClickAsync();
        await s.FindAlertMessage(partialText: "updated");

        await Expect(s.Page.Locator("[name='AppName']")).ToHaveValueAsync("My Test Project");
        await Expect(s.Page.Locator("[name='HeroTitle']")).ToHaveValueAsync("Support My Work");
        await Expect(s.Page.Locator("[name='DisplayName']")).ToHaveValueAsync("Jane Dev");
        await Expect(s.Page.Locator("[name='FundingGoal']")).ToHaveValueAsync("1000");
    }

    /// <summary>
    /// Social link inputs should accept and persist values.
    /// </summary>
    [Fact]
    public async Task CanSaveSocialLinks()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();

        await s.CreateApp("OpenPatron");
        await s.Page.Locator("button[type='submit']:has-text('Continue')").ClickAsync();
        await s.FindAlertMessage(partialText: "Template selected");

        await s.Page.Locator("[name='SocialX']").FillAsync("janedev");
        await s.Page.Locator("[name='SocialNostr']").FillAsync("npub1abc123");

        await s.Page.Locator("#saveBtn").ClickAsync();
        await s.FindAlertMessage(partialText: "updated");

        await Expect(s.Page.Locator("[name='SocialX']")).ToHaveValueAsync("janedev");
        await Expect(s.Page.Locator("[name='SocialNostr']")).ToHaveValueAsync("npub1abc123");
    }

    // ── Theme settings ──

    /// <summary>
    /// Theme accent color, border radius, and block spacing should persist.
    /// </summary>
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
        await s.Page.Locator("[name='ThemeBlockSpacing']").SelectOptionAsync("2rem");

        await s.Page.Locator("#saveBtn").ClickAsync();
        await s.FindAlertMessage(partialText: "updated");

        await Expect(s.Page.Locator("[name='ThemeAccentColor']")).ToHaveValueAsync("#ff5500");
        await Expect(s.Page.Locator("[name='ThemeBorderRadius']")).ToHaveValueAsync("2rem");
        await Expect(s.Page.Locator("[name='ThemeBlockSpacing']")).ToHaveValueAsync("2rem");
    }

    // ── Public page rendering ──

    /// <summary>
    /// The public page should render blocks in layout order with data-block-type attributes.
    /// </summary>
    [Fact]
    public async Task PublicPageRendersBlocksInOrder()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();

        var (_, appId) = await s.CreateApp("OpenPatron");

        // Set up as Project
        await s.Page.Locator("button[type='submit']:has-text('Continue')").ClickAsync();
        await s.FindAlertMessage(partialText: "Template selected");

        // Fill required fields and publish
        await s.Page.Locator("[name='HeroTitle']").ClearAsync();
        await s.Page.Locator("[name='HeroTitle']").FillAsync("Test Project");
        await s.Page.Locator("[name='Description']").FillAsync("A great project");
        await s.Page.Locator("[name='Visibility']").SelectOptionAsync("1");
        await s.Page.Locator("#saveBtn").ClickAsync();
        await s.FindAlertMessage(partialText: "updated");

        // Visit public page
        await s.GoToUrl($"/apps/{appId}/openpatron");

        // Verify blocks are rendered
        var blocks = s.Page.Locator("[data-block-type]");
        var count = await blocks.CountAsync();
        Assert.True(count > 0, "Expected at least one block on the public page");

        // First block should be project-hero
        var firstType = await blocks.First.GetAttributeAsync("data-block-type");
        Assert.Equal("project-hero", firstType);
    }

    /// <summary>
    /// The public page should apply theme CSS variables.
    /// </summary>
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

        // Set custom accent color
        await s.Page.Locator("[name='ThemeAccentColor']").ClearAsync();
        await s.Page.Locator("[name='ThemeAccentColor']").FillAsync("#ff5500");
        await s.Page.Locator("[name='Visibility']").SelectOptionAsync("1");
        await s.Page.Locator("#saveBtn").ClickAsync();
        await s.FindAlertMessage(partialText: "updated");

        // Visit public page
        await s.GoToUrl($"/apps/{appId}/openpatron");

        // Check that the accent CSS variable is applied
        var pageSource = await s.Page.ContentAsync();
        Assert.Contains("--op-accent: #ff5500", pageSource);
    }

    // ── Projects ──

    /// <summary>
    /// Adding and saving a project should persist after reload.
    /// </summary>
    [Fact]
    public async Task CanAddAndSaveProject()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();

        await s.CreateApp("OpenPatron");

        await s.Page.Locator("label[for='pageTypePersonal']").ClickAsync();
        await s.Page.Locator("button[type='submit']:has-text('Continue')").ClickAsync();
        await s.FindAlertMessage(partialText: "Template selected");

        await s.Page.Locator("#addProjectBtn").ClickAsync();
        await s.Page.Locator("[name='Projects[0].Name']").FillAsync("My Project");
        await s.Page.Locator("[name='Projects[0].Url']").FillAsync("https://example.com/myproject");
        await s.Page.Locator("[name='Projects[0].Description']").FillAsync("Cool project");

        await s.Page.Locator("#saveBtn").ClickAsync();
        await s.FindAlertMessage(partialText: "updated");

        await Expect(s.Page.Locator("[name='Projects[0].Name']")).ToHaveValueAsync("My Project");
        await Expect(s.Page.Locator("[name='Projects[0].Url']")).ToHaveValueAsync("https://example.com/myproject");
        await Expect(s.Page.Locator("[name='Projects[0].Description']")).ToHaveValueAsync("Cool project");
    }

    /// <summary>
    /// Removing the middle project should renumber correctly and survive a save.
    /// </summary>
    [Fact]
    public async Task RemovingMiddleProjectKeepsOthersAfterSave()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();

        await s.CreateApp("OpenPatron");

        await s.Page.Locator("label[for='pageTypePersonal']").ClickAsync();
        await s.Page.Locator("button[type='submit']:has-text('Continue')").ClickAsync();
        await s.FindAlertMessage(partialText: "Template selected");

        for (var i = 0; i < 3; i++)
        {
            await s.Page.Locator("#addProjectBtn").ClickAsync();
        }
        await s.Page.Locator("[name='Projects[0].Name']").FillAsync("A");
        await s.Page.Locator("[name='Projects[0].Url']").FillAsync("https://example.com/a");
        await s.Page.Locator("[name='Projects[1].Name']").FillAsync("B");
        await s.Page.Locator("[name='Projects[1].Url']").FillAsync("https://example.com/b");
        await s.Page.Locator("[name='Projects[2].Name']").FillAsync("C");
        await s.Page.Locator("[name='Projects[2].Url']").FillAsync("https://example.com/c");

        await s.Page.Locator(".project-row").Nth(1).Locator(".project-remove-btn").ClickAsync();

        await Expect(s.Page.Locator("[name='Projects[0].Name']")).ToHaveValueAsync("A");
        await Expect(s.Page.Locator("[name='Projects[1].Name']")).ToHaveValueAsync("C");

        await s.Page.Locator("#saveBtn").ClickAsync();
        await s.FindAlertMessage(partialText: "updated");

        await Expect(s.Page.Locator("[name='Projects[0].Name']")).ToHaveValueAsync("A");
        await Expect(s.Page.Locator("[name='Projects[1].Name']")).ToHaveValueAsync("C");
        await Expect(s.Page.Locator(".project-row")).ToHaveCountAsync(2);
    }

    // ── Sponsor wall toggle ──

    [Fact]
    public async Task SponsorWallToggleWorks()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();

        await s.CreateApp("OpenPatron");
        await s.Page.Locator("button[type='submit']:has-text('Continue')").ClickAsync();
        await s.FindAlertMessage(partialText: "Template selected");

        await Expect(s.Page.Locator("[name='ShowSponsorWall']")).Not.ToBeCheckedAsync();

        await s.Page.Locator("[name='ShowSponsorWall']").CheckAsync();

        await s.Page.Locator("#saveBtn").ClickAsync();
        await s.FindAlertMessage(partialText: "updated");

        await Expect(s.Page.Locator("[name='ShowSponsorWall']")).ToBeCheckedAsync();
    }

    // ── Empty block list ──

    /// <summary>
    /// If all blocks are removed, the public page should render gracefully.
    /// </summary>
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

        // Remove all blocks via JS
        await s.Page.EvaluateAsync(@"() => {
            document.getElementById('pageLayoutJson').value = '[]';
        }");

        await s.Page.Locator("[name='Visibility']").SelectOptionAsync("1");
        await s.Page.Locator("#saveBtn").ClickAsync();
        await s.FindAlertMessage(partialText: "updated");

        // Visit public page -- should not error
        await s.GoToUrl($"/apps/{appId}/openpatron");
        await s.Page.AssertNoError();

        // Should show empty state or at least the sidebar
        var content = await s.Page.ContentAsync();
        Assert.Contains("openpatron-page", content);
    }
}
