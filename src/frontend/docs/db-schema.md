# Database Schema & Data Consistency

We use **Dexie.js** as a wrapper around **IndexedDB** to provide a persistent, offline-first storage engine.

## Table Definitions

The schema is defined in `src/data/db.ts` and versioned.

### 1. `meta`

A key-value store for application state and manifest data.

-   **Primary Key**: `key`
-   **Fields**: `value` (any)
-   **Common Keys**:
    -   `active`: A map of categories to their current active hashes.
    -   `manifest`: The full `site-manifest.json` object.
    -   `build`: Current build timestamp/identifier.

### 2. `datasets`

Stores the raw JSON arrays for each category.

-   **Primary Key**: `key` (format `[category]:[hash]`)
-   **Indexes**: `category`, `hash`
-   **Fields**: `data` (Array of Entries), `ts` (Sync timestamp)

### 3. `indexes`

Stores the pre-built Search Indexes for **MiniSearch**.

-   **Primary Key**: `key` (format `[category]:[hash]`)
-   **Indexes**: `category`, `hash`
-   **Fields**: `index` (Object), `ts` (Sync timestamp)

---

## Business Logic Consistency

### 1. Versioning & Migration

When the remote manifest hash changes for a category:

1.  The `loader.ts` fetches the new JSON files.
2.  The new data is inserted into `datasets` and `indexes`.
3.  The `meta` table's `active` entry is updated.
4.  The app prompts the user to refresh (via UI state in `AppRoot`).

### 2. Search Indexing

-   Searches are performed across **all active indexes** defined in the `active` map.
-   MiniSearch allows for fast, fuzzy, local searching without network dependency.
-   Rule: Always include the `category` in the search document so results can be correctly routed to the detail views.

### 3. Cleanup

To prevent the IndexedDB from growing indefinitely, a cleanup routine (future work) should remove rows from `datasets` and `indexes` that are not listed in the current `active` map.
