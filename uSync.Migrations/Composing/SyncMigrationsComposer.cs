﻿using Microsoft.Extensions.DependencyInjection;

using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;

using uSync.Migrations.Configuration;
using uSync.Migrations.Configuration.Models;
using uSync.Migrations.Handlers;
using uSync.Migrations.Migrators;
using uSync.Migrations.Migrators.BlockGrid.BlockMigrators;
using uSync.Migrations.Migrators.BlockGrid.Extensions;
using uSync.Migrations.Notifications;
using uSync.Migrations.Services;
using uSync.Migrations.Validation;

namespace uSync.Migrations.Composing;

public class SyncMigrationsComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AdduSyncMigrations();
    }
}

public static class SyncMigrationsBuilderExtensions
{
    /// <summary>
    ///  Add uSync Migrations to your project
    /// </summary>
    /// <remarks>
    ///  if your startup.cs has an .AddComposers() line then this will be automatically added anyway
    /// </remarks>
    public static IUmbracoBuilder AdduSyncMigrations(this IUmbracoBuilder builder)
    {
        // stop a double add. 
        if (builder.Services.Any(x => x.ServiceType == typeof(ISyncMigrationFileService)))
        {
            return builder;
        }

        builder.Services.AddSingleton<GridConventions>();

        builder
            .WithCollectionBuilder<SyncPropertyMigratorCollectionBuilder>()
                .Append(builder.TypeLoader.GetTypes<ISyncPropertyMigrator>());

        builder
            .WithCollectionBuilder<SyncBlockMigratorCollectionBuilder>()
                .Add(() => builder.TypeLoader.GetTypes<ISyncBlockMigrator>());
        

        builder.Services.AddTransient<ISyncMigrationFileService, SyncMigrationFileService>();

        builder
            .WithCollectionBuilder<SyncMigrationHandlerCollectionBuilder>()
                .Add(() => builder.TypeLoader.GetTypes<ISyncMigrationHandler>());

        builder
            .WithCollectionBuilder<SyncMigrationProfileCollectionBuilder>()
                .Add(builder.TypeLoader.GetTypes<ISyncMigrationProfile>());

        builder
            .WithCollectionBuilder<SyncMigrationValidatorCollectionBuilder>()
                .Add(() => builder.TypeLoader.GetTypes<ISyncMigrationValidator>());

        builder.Services.AddTransient<ISyncMigrationService, SyncMigrationService>();
        builder.Services.AddTransient<ISyncMigrationConfigurationService, SyncMigrationConfigurationService>();

        builder.AddNotificationHandler<ServerVariablesParsingNotification, SyncMigrationsServerVariablesParsingNotificationHandler>();

        if (builder.ManifestFilters().Has<SyncMigrationsManifestFilter>() == false)
        {
            builder.ManifestFilters().Append<SyncMigrationsManifestFilter>();
        }

        return builder;
    }
}