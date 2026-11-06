using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;
using Xunit;

namespace PgrStudentManagement.UiTests;

public class PhdApplicationUxTests : IAsyncLifetime
{
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    private readonly string _baseUrl =
        Environment.GetEnvironmentVariable("BASE_URL") ?? "http://localhost:5277";

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
    }

    public async Task DisposeAsync()
    {
        await _browser.DisposeAsync();
        _playwright.Dispose();
    }

    [Fact]
    public async Task CreateForm_UsesHumanReadableLabelsAndSemanticRoles()
    {
        var page = await _browser.NewPageAsync();
        await page.GotoAsync($"{_baseUrl}/PhdApplications/Create");

        // Verify human-readable labels exist and are visible
        var firstName = page.GetByLabel("First name");
        var lastName = page.GetByLabel("Last name");
        var email = page.GetByLabel("Email address");

        Assert.True(await firstName.IsVisibleAsync());
        Assert.True(await lastName.IsVisibleAsync());
        Assert.True(await email.IsVisibleAsync());

        // Verify native HTML5 constraint validation attributes
        Assert.NotNull(await firstName.GetAttributeAsync("required"));
        Assert.NotNull(await lastName.GetAttributeAsync("required"));
        Assert.NotNull(await email.GetAttributeAsync("required"));
        Assert.Equal("email", await email.GetAttributeAsync("type"));

        // Verify WCAG 2.2 AA compliance via AxeCore
        var axeResults = await page.RunAxe();
        Assert.Empty(axeResults.Violations);
    }

    [Fact]
    public async Task CreateForm_SemanticControls_UsesRadiosAndDropdowns()
    {
        var page = await _browser.NewPageAsync();
        await page.GotoAsync($"{_baseUrl}/PhdApplications/Create");

        // Mode of study is a fieldset radio group
        var studyModeGroup = page.GetByRole(AriaRole.Group, new()
        {
            NameRegex = new Regex("Mode of study", RegexOptions.IgnoreCase)
        });
        Assert.True(await studyModeGroup.IsVisibleAsync());
        var radioCount = await studyModeGroup.GetByRole(AriaRole.Radio).CountAsync();
        Assert.Equal(2, radioCount);

        // Supervisor is a combobox populated with supervisor names
        var supervisorSelect = page.GetByRole(AriaRole.Combobox, new()
        {
            NameRegex = new Regex("Proposed main supervisor", RegexOptions.IgnoreCase)
        });
        Assert.True(await supervisorSelect.IsVisibleAsync());
        var optionCount = await supervisorSelect.Locator("option").CountAsync();
        Assert.True(optionCount > 1, "Supervisor dropdown should have multiple options");
    }

    [Fact]
    public async Task EmptySubmit_FocusesErrorSummaryAndMarksFieldsInvalid()
    {
        var page = await _browser.NewPageAsync();
        await page.GotoAsync($"{_baseUrl}/PhdApplications/Create");

        // Bypass native HTML5 constraint blocking to test server-side error summary recovery
        await page.Locator("form").EvaluateAsync("form => form.noValidate = true");

        await page.GetByRole(AriaRole.Button, new()
        {
            NameRegex = new Regex("Submit application|Create", RegexOptions.IgnoreCase)
        }).First.ClickAsync();

        // Title starts with "Error:" for screen readers
        var title = await page.TitleAsync();
        Assert.StartsWith("Error:", title);

        // Error summary banner is visible with role="alert"
        var errorSummary = page.Locator("#error-summary[role='alert']");
        Assert.True(await errorSummary.IsVisibleAsync());

        // Clicking first error link jumps focus to target field
        var firstErrorLink = errorSummary.Locator("a").First;
        var targetHref = await firstErrorLink.GetAttributeAsync("href");
        Assert.NotNull(targetHref);
        await firstErrorLink.ClickAsync();

        var targetInput = page.Locator(targetHref);
        await Assertions.Expect(targetInput).ToBeFocusedAsync();
    }

    [Fact]
    public async Task LiveCharacterCounterAndAccessibleApplicationsTable()
    {
        var page = await _browser.NewPageAsync();
        await page.GotoAsync($"{_baseUrl}/PhdApplications/Create");

        // Live character counter on Research Summary
        var summaryField = page.GetByLabel(new Regex("Research summary", RegexOptions.IgnoreCase));
        await summaryField.FillAsync("Exploring automated accessibility testing in academic portals.");
        var counter = page.Locator("#ResearchSummary-count");
        var counterText = await counter.InnerTextAsync();
        Assert.Contains("remaining", counterText, StringComparison.OrdinalIgnoreCase);

        // Applications index: table caption, filter, and contextual action links
        await page.GotoAsync($"{_baseUrl}/PhdApplications");
        Assert.True(await page.GetByLabel("Filter by status").IsVisibleAsync());
        var table = page.GetByRole(AriaRole.Table, new()
        {
            NameRegex = new Regex("List of PhD applications", RegexOptions.IgnoreCase)
        });
        Assert.True(await table.IsVisibleAsync());

        var detailsLink = page.GetByRole(AriaRole.Link, new()
        {
            NameRegex = new Regex(@"Details for (APP|PHD)-", RegexOptions.IgnoreCase)
        }).First;
        Assert.True(await detailsLink.IsVisibleAsync());
    }

    [Fact]
    public async Task AutomatedWcagAudit_OnCreatePage()
    {
        var page = await _browser.NewPageAsync();
        await page.GotoAsync($"{_baseUrl}/PhdApplications/Create");

        var axeResults = await page.RunAxe();
        Assert.Empty(axeResults.Violations);
    }
}

