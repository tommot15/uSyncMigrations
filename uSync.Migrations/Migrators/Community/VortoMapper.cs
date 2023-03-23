﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;
using uSync.Migrations.Context;
using uSync.Migrations.Migrators.Models;
using uSync.Migrations.Models;

namespace uSync.Migrations.Migrators.Community;

[SyncMigrator( "Our.Umbraco.Vorto" )]
public class VortoMapper : SyncPropertyMigratorBase,
    ISyncReplacablePropertyMigrator,
    ISyncVariationPropertyMigrator {
  private readonly IDataTypeService _dataTypeService;
  private readonly ILocalizationService _localizationService;

  public VortoMapper( IDataTypeService dataTypeService, ILocalizationService localizationService ) {
    _dataTypeService = dataTypeService;
    _localizationService = localizationService;
  }

  public override object GetConfigValues( SyncMigrationDataTypeProperty dataTypeProperty, SyncMigrationContext context )
        => string.Empty;

  public override string GetEditorAlias( SyncMigrationDataTypeProperty dataTypeProperty, SyncMigrationContext context ) {
    var wrappedDataType = GetWrappedDatatype( dataTypeProperty.PreValues );
    if ( wrappedDataType != null ) {
      return wrappedDataType.EditorAlias;
    }

    return string.Empty;
  }

  /// <summary>
  ///  vorto properties don't actually need to be on the target - the properties they wrap should already be there. 
  ///  so the migrator needs to actually tell the process what should be here.
  /// </summary>
  /// <param name="dataTypeProperty"></param>
  /// <param name="context"></param>
  /// <returns></returns>
  public ReplacementDataTypeInfo? GetReplacementEditorId( SyncMigrationDataTypeProperty dataTypeProperty, SyncMigrationContext context ) {
    var wrappedDataType = GetWrappedDatatype( dataTypeProperty.PreValues );
    if ( wrappedDataType != null ) {
      return new ReplacementDataTypeInfo( wrappedDataType.Key, wrappedDataType.EditorAlias ) {
        Variation = "Culture"
      };
    }

    return null;
  }

  private IDataType? GetWrappedDatatype( IReadOnlyCollection<PreValue> preValues ) {
    var dataType = preValues.FirstOrDefault( x => x.Alias.Equals( "dataType" ) );
    if ( dataType == null ) return null;

    var value = JsonConvert.DeserializeObject<JObject>( dataType.Value );
    if ( value is null ) return null;

    // guid is the guid of the wrapped datatype. 
    var attempt = value.Value<string>( "guid" ).TryConvertTo<Guid>();

    if ( attempt )
      return _dataTypeService.GetDataType( attempt.Result );

    return null;
  }

  public Attempt<CulturedPropertyValue> GetVariedElements( SyncMigrationContentProperty contentProperty, SyncMigrationContext context ) {
    try {
      if ( contentProperty?.Value == null ) {
        return Attempt<CulturedPropertyValue>.Fail( new Exception( "No culturedValues found on vorto property" ) );
      }

      CulturedPropertyValue? culturedValues = JsonConvert.DeserializeObject<CulturedPropertyValue>( contentProperty.Value );

      if ( culturedValues == null ) {
        return Attempt<CulturedPropertyValue>.Fail( new Exception( "No culturedValues found on vorto property" ) );
      }

      if ( culturedValues.Values != null ) {
        return Attempt.Succeed( culturedValues );
      } else {
        //Must be an issue with the data so we just add empty values for all languages
        IEnumerable<ILanguage> allLanguages = _localizationService.GetAllLanguages();
        return Attempt.Succeed( new CulturedPropertyValue() {
          DtdGuid = culturedValues.DtdGuid,
          Values = allLanguages.ToDictionary( l => l.IsoCode, l => "" ),
        } );
      }

    } catch ( Exception ex ) {
      return Attempt<CulturedPropertyValue>.Fail( ex );
    }
  }
}
