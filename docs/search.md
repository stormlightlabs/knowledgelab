# Search Syntax

Full-text search uses BM25 ranking with filters.

## Basic Search

```text
query text
```

## Tag Filters

```text
#tag
#project/active
```

## Path Filters

Filter by folder:

```text
path:daily/
path:projects/
```

## Date Filters

```text
created:2025-01-01..2025-01-31
modified:2025-01-15
```

## Combined Queries

```text
query text #tag path:folder/ created:2025-01-01..
```
