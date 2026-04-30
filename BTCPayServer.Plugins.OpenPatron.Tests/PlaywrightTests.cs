using System;
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
    /// <summary>
    /// After creating an OpenPatron app, the settings page should show the page
    /// type picker with Personal and Project radio buttons, and a Continue button.
    /// No other settings should be visible yet.
    /// </summary>
    [Fact]
    public async Task NewAppShowsPageTypePicker()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();

        var (_, appId) = await s.CreateApp("OpenPatron");

        // Should show the page type picker
        await Expect(s.Page.Locator("label[for='pageTypePersonal']")).ToBeVisibleAsync();
        await Expect(s.Page.Locator("label[for='pageTypeProject']")).ToBeVisibleAsync();
        await Expect(s.Page.Locator("button[type='submit']:has-text('Continue')")).ToBeVisibleAsync();

        // Full editor fields should NOT be visible
        await Expect(s.Page.Locator("[name='AppName']")).Not.ToBeVisibleAsync();
        await Expect(s.Page.Locator("[name='HeroTitle']")).Not.ToBeVisibleAsync();
        await Expect(s.Page.Locator("[name='DisplayName']")).Not.ToBeVisibleAsync();
    }

    /// <summary>
    /// The default selection should be Project (server‐rendered as checked).
    /// Clicking Personal should visually highlight that option.
    /// </summary>
    [Fact]
    public async Task PageTypePickerDefaultsToProject()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();

        await s.CreateApp("OpenPatron");

        // Project should be checked by default
        await Expect(s.Page.Locator("#pageTypeProject")).ToBeCheckedAsync();
        await Expect(s.Page.Locator("#pageTypePersonal")).Not.ToBeCheckedAsync();
    }

    /// <summary>
    /// Clicking a radio option should update the visual highlight:
    /// the selected option gets border-primary, the other loses it.
    /// </summary>
    [Fact]
    public async Task PageTypePickerHighlightFollowsSelection()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();

        await s.CreateApp("OpenPatron");

        // Initially Project is selected and highlighted
        var projectLabel = s.Page.Locator("label[for='pageTypeProject']");
        var personalLabel = s.Page.Locator("label[for='pageTypePersonal']");
        var projectClasses = await projectLabel.GetAttributeAsync("class") ?? "";
        Assert.Contains("border-primary", projectClasses);

        // Click Personal
        await personalLabel.ClickAsync();
        await Expect(s.Page.Locator("#pageTypePersonal")).ToBeCheckedAsync();
        var personalClasses = await personalLabel.GetAttributeAsync("class") ?? "";
        Assert.Contains("border-primary", personalClasses);
    }

    /// <summary>
    /// Clicking Continue with Project selected should confirm the type,
    /// redirect to the full editor, and show a Project badge in the header.
    /// </summary>
    [Fact]
    public async Task ContinueWithProjectShowsFullEditor()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();

        await s.CreateApp("OpenPatron");

        // Project is default — click Continue
        await s.Page.Locator("button[type='submit']:has-text('Continue')").ClickAsync();

        // Should show success message
        await s.FindAlertMessage(partialText: "Page type saved");

        // Should now be on the full editor
        await Expect(s.Page.Locator("[name='AppName']")).ToBeVisibleAsync();
        await Expect(s.Page.Locator("[name='HeroTitle']")).ToBeVisibleAsync();

        // Badge in header should say "Project"
        await Expect(s.Page.Locator(".sticky-header .badge")).ToContainTextAsync("Project");

        // The page type picker should no longer be visible
        await Expect(s.Page.Locator("#pageTypePersonal")).Not.ToBeVisibleAsync();
    }

    /// <summary>
    /// Selecting Personal and clicking Continue should confirm Personal type,
    /// show badge "Personal", and display the projects section.
    /// </summary>
    [Fact]
    public async Task ContinueWithPersonalShowsPersonalEditor()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();

        await s.CreateApp("OpenPatron");

        // Select Personal
        await s.Page.Locator("label[for='pageTypePersonal']").ClickAsync();
        await s.Page.Locator("button[type='submit']:has-text('Continue')").ClickAsync();

        await s.FindAlertMessage(partialText: "Page type saved");

        // Badge should say "Personal"
        await Expect(s.Page.Locator(".sticky-header .badge")).ToContainTextAsync("Personal");

        // Projects section should be visible for Personal pages
        await Expect(s.Page.Locator("#projectsSection")).ToBeVisibleAsync();

        // Funding goal section should be hidden for Personal pages
        await Expect(s.Page.Locator("#fundingGoalSection")).Not.ToBeVisibleAsync();
    }

    /// <summary>
    /// On a Project page, the funding goal section should be visible;
    /// on a Personal page, it should be hidden.
    /// </summary>
    [Fact]
    public async Task FundingGoalOnlyVisibleForProjectPages()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();

        // Create a Project page
        var (_, appId) = await s.CreateApp("OpenPatron");
        await s.Page.Locator("button[type='submit']:has-text('Continue')").ClickAsync();
        await s.FindAlertMessage(partialText: "Page type saved");

        // Funding goal should be visible
        await Expect(s.Page.Locator("#fundingGoalSection")).ToBeVisibleAsync();
    }

    /// <summary>
    /// After confirming page type, filling in settings and saving should persist
    /// the values and show them again on reload.
    /// </summary>
    [Fact]
    public async Task CanSaveAndReloadSettings()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();

        await s.CreateApp("OpenPatron");

        // Confirm Project type
        await s.Page.Locator("button[type='submit']:has-text('Continue')").ClickAsync();
        await s.FindAlertMessage(partialText: "Page type saved");

        // Fill in some settings
        await s.Page.Locator("[name='AppName']").ClearAsync();
        await s.Page.Locator("[name='AppName']").FillAsync("My Test Project");
        await s.Page.Locator("[name='HeroTitle']").ClearAsync();
        await s.Page.Locator("[name='HeroTitle']").FillAsync("Support My Work");
        await s.Page.Locator("[name='DisplayName']").FillAsync("Jane Dev");
        await s.Page.Locator("[name='AccentColor']").ClearAsync();
        await s.Page.Locator("[name='AccentColor']").FillAsync("#ff5500");
        await s.Page.Locator("[name='FundingGoal']").FillAsync("1000");

        // Save
        await s.Page.Locator("button[type='submit']:has-text('Save changes')").ClickAsync();
        await s.FindAlertMessage(partialText: "updated");

        // Values should be persisted
        await Expect(s.Page.Locator("[name='AppName']")).ToHaveValueAsync("My Test Project");
        await Expect(s.Page.Locator("[name='HeroTitle']")).ToHaveValueAsync("Support My Work");
        await Expect(s.Page.Locator("[name='DisplayName']")).ToHaveValueAsync("Jane Dev");
        await Expect(s.Page.Locator("[name='AccentColor']")).ToHaveValueAsync("#ff5500");
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
        await s.FindAlertMessage(partialText: "Page type saved");

        // Fill social links
        await s.Page.Locator("[name='SocialX']").FillAsync("janedev");
        await s.Page.Locator("[name='SocialNostr']").FillAsync("npub1abc123");

        // Save
        await s.Page.Locator("button[type='submit']:has-text('Save changes')").ClickAsync();
        await s.FindAlertMessage(partialText: "updated");

        // Verify persisted
        await Expect(s.Page.Locator("[name='SocialX']")).ToHaveValueAsync("janedev");
        await Expect(s.Page.Locator("[name='SocialNostr']")).ToHaveValueAsync("npub1abc123");
    }

    /// <summary>
    /// The page type should be locked after the first save: reloading the page
    /// should not show radio buttons but should show the badge.
    /// </summary>
    [Fact]
    public async Task PageTypeIsLockedAfterFirstSave()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();

        await s.CreateApp("OpenPatron");

        // Select Personal and confirm
        await s.Page.Locator("label[for='pageTypePersonal']").ClickAsync();
        await s.Page.Locator("button[type='submit']:has-text('Continue')").ClickAsync();
        await s.FindAlertMessage(partialText: "Page type saved");

        // Reload
        await s.Page.ReloadAsync();

        // Radio buttons should not exist
        await Expect(s.Page.Locator("#pageTypePersonal")).Not.ToBeVisibleAsync();
        await Expect(s.Page.Locator("#pageTypeProject")).Not.ToBeVisibleAsync();

        // Badge should say Personal
        await Expect(s.Page.Locator(".sticky-header .badge")).ToContainTextAsync("Personal");

        // Full editor should be visible
        await Expect(s.Page.Locator("[name='AppName']")).ToBeVisibleAsync();
    }

    /// <summary>
    /// Adding a project to the project list (via "+ Add project manually")
    /// should persist after save and be visible after reload.
    /// </summary>
    [Fact]
    public async Task CanAddAndSaveProject()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();

        await s.CreateApp("OpenPatron");

        // Choose Personal so the projects section is visible
        await s.Page.Locator("label[for='pageTypePersonal']").ClickAsync();
        await s.Page.Locator("button[type='submit']:has-text('Continue')").ClickAsync();
        await s.FindAlertMessage(partialText: "Page type saved");

        // Add a project row
        await s.Page.Locator("#addProjectBtn").ClickAsync();
        await s.Page.Locator("[name='Projects[0].Name']").FillAsync("My Project");
        await s.Page.Locator("[name='Projects[0].Url']").FillAsync("https://example.com/myproject");
        await s.Page.Locator("[name='Projects[0].Description']").FillAsync("Cool project");

        // Save
        await s.Page.Locator("button[type='submit']:has-text('Save changes')").ClickAsync();
        await s.FindAlertMessage(partialText: "updated");

        // Project should still be there after reload
        await Expect(s.Page.Locator("[name='Projects[0].Name']")).ToHaveValueAsync("My Project");
        await Expect(s.Page.Locator("[name='Projects[0].Url']")).ToHaveValueAsync("https://example.com/myproject");
        await Expect(s.Page.Locator("[name='Projects[0].Description']")).ToHaveValueAsync("Cool project");
    }

    /// <summary>
    /// Removing a project from the middle of the list should renumber the
    /// remaining rows so they keep contiguous indices and survive a save.
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
        await s.FindAlertMessage(partialText: "Page type saved");

        // Add three projects
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

        // Remove the middle one (B)
        await s.Page.Locator(".project-row").Nth(1).Locator(".project-remove-btn").ClickAsync();

        // After renumbering, A is [0] and C is [1]
        await Expect(s.Page.Locator("[name='Projects[0].Name']")).ToHaveValueAsync("A");
        await Expect(s.Page.Locator("[name='Projects[1].Name']")).ToHaveValueAsync("C");

        await s.Page.Locator("button[type='submit']:has-text('Save changes')").ClickAsync();
        await s.FindAlertMessage(partialText: "updated");

        // Both A and C should survive the save
        await Expect(s.Page.Locator("[name='Projects[0].Name']")).ToHaveValueAsync("A");
        await Expect(s.Page.Locator("[name='Projects[1].Name']")).ToHaveValueAsync("C");
        await Expect(s.Page.Locator(".project-row")).ToHaveCountAsync(2);
    }

    /// <summary>
    /// The sponsor wall toggle should be off by default and togglable.
    /// </summary>
    [Fact]
    public async Task SponsorWallToggleWorks()
    {
        await using var s = CreatePlaywrightTester();
        await s.StartAsync();
        await s.RegisterNewUser(true);
        await s.CreateNewStore();

        await s.CreateApp("OpenPatron");
        await s.Page.Locator("button[type='submit']:has-text('Continue')").ClickAsync();
        await s.FindAlertMessage(partialText: "Page type saved");

        // Should be unchecked by default
        await Expect(s.Page.Locator("[name='ShowSponsorWall']")).Not.ToBeCheckedAsync();

        // Toggle it on
        await s.Page.Locator("[name='ShowSponsorWall']").CheckAsync();

        // Save
        await s.Page.Locator("button[type='submit']:has-text('Save changes')").ClickAsync();
        await s.FindAlertMessage(partialText: "updated");

        // Should be checked after save
        await Expect(s.Page.Locator("[name='ShowSponsorWall']")).ToBeCheckedAsync();
    }
}
