using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

namespace site.Composers;

public class SiteSettingsDocumentTypeSeeder : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private const string ContentTypeAlias = "siteSettings";

    private readonly IContentTypeService _contentTypeService;
    private readonly IDataTypeService _dataTypeService;
    private readonly IShortStringHelper _shortStringHelper;
    private readonly ILogger<SiteSettingsDocumentTypeSeeder> _logger;

    public SiteSettingsDocumentTypeSeeder(
        IContentTypeService contentTypeService,
        IDataTypeService dataTypeService,
        IShortStringHelper shortStringHelper,
        ILogger<SiteSettingsDocumentTypeSeeder> logger)
    {
        _contentTypeService = contentTypeService;
        _dataTypeService = dataTypeService;
        _shortStringHelper = shortStringHelper;
        _logger = logger;
    }

    public Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        if (_contentTypeService.Get(ContentTypeAlias) is not null)
        {
            return Task.CompletedTask;
        }

        IDataType textBox = GetRequiredDataType("Umbraco.TextBox", "Textstring");
        IDataType textArea = GetRequiredDataType("Umbraco.TextArea", "Textarea");
        IDataType mediaPicker = GetRequiredDataType("Umbraco.MediaPicker3", "Media Picker");
        IDataType label = GetRequiredDataType("Umbraco.Label", "Label");

        var contentType = new ContentType(_shortStringHelper, -1)
        {
            Alias = ContentTypeAlias,
            Name = "Site Settings",
            Icon = "icon-settings-alt",
            AllowedAsRoot = true,
            IsElement = false,
            Variations = ContentVariation.Nothing,
            Description = "Global website settings for shared header, footer, and site metadata."
        };

        AddTab(contentType, "General", "general");
        AddTab(contentType, "Header", "header");
        AddTab(contentType, "Footer", "footer");

        AddProperty(contentType, textBox, "general", "favicon", "Favicon", 0,
            "A favicon is a small image displayed next to the page title in the browser tab.");
        AddProperty(contentType, textBox, "general", "siteTitle", "Site Title", 1,
            "Defines the title of the document, use {0} to insert the current page name and {1} for the company name below.");
        AddProperty(contentType, textBox, "general", "companyName", "Company Name", 2,
            "The name of the company.");
        AddProperty(contentType, mediaPicker, "general", "siteLogo", "Site Logo", 3,
            "Global site logo.");

        AddProperty(contentType, label, "header", "utilityNavigation", "Utility Navigation", 0, string.Empty);
        AddProperty(contentType, label, "header", "mainNavigation", "Main Navigation", 1, string.Empty);

        AddProperty(contentType, label, "footer", "footerNavigation", "Footer Navigation", 0, string.Empty);
        AddProperty(contentType, label, "footer", "socialLinks", "Social Links", 1, string.Empty);
        AddProperty(contentType, textArea, "footer", "copyright", "Copyright", 2,
            "Plain text copyright notice.");

        _contentTypeService.Save(contentType, Constants.Security.SuperUserId);
        _logger.LogInformation("Created document type {DocumentTypeAlias} for uSync export.", ContentTypeAlias);

        return Task.CompletedTask;
    }

    private void AddTab(ContentType contentType, string name, string alias)
    {
        if (contentType.PropertyGroups.Any(x => x.Alias == alias))
        {
            return;
        }

        contentType.AddPropertyGroup(name, alias);
    }

    private void AddProperty(
        ContentType contentType,
        IDataType dataType,
        string tabAlias,
        string alias,
        string name,
        int sortOrder,
        string description)
    {
        var propertyType = new PropertyType(_shortStringHelper, dataType, alias)
        {
            Name = name,
            SortOrder = sortOrder,
            Description = description,
            Variations = ContentVariation.Nothing
        };

        contentType.AddPropertyType(propertyType, tabAlias);
    }

    private IDataType GetRequiredDataType(string editorAlias, string fallbackName)
    {
        var dataType = _dataTypeService.GetByEditorAlias(editorAlias).FirstOrDefault()
            ?? _dataTypeService.GetDataType(fallbackName);

        if (dataType is null)
        {
            throw new InvalidOperationException(
                $"Required data type was not found. Editor alias: {editorAlias}, fallback name: {fallbackName}.");
        }

        return dataType;
    }
}
