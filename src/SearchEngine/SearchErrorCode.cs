namespace SearchEngine;

public enum SearchErrorCode
{
  EmptyQuery,
  QueryHasNoSearchableTerms,
  IndexNotBuilt,
  IndexIsEmpty,
  InvalidSearchOptions,
  InvalidSearchRequest,
  InvalidSourceRecord,
  InvalidDelimitedSourceFormat,
  InvalidIdFormat,
  ManualIndexingAlreadyStarted,
  ManualIndexingNotStarted,
  ResourceInitializationFailed,
  IndexBuildFailed,
  SearchExecutionFailed
}