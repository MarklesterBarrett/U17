using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Site.SubsidiarySites;

public sealed class SubsidiarySiteGeneratorComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddUnique<ISubsidiarySiteThemeService, SubsidiarySiteThemeService>();
        builder.Services.AddUnique<ISubsidiarySiteGeneratorService, SubsidiarySiteGeneratorService>();
    }
}
