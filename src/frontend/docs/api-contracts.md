# API Contracts & Data Structures

This document defines the data structures used between the content extractor (backend) and the frontend.

## The `Entry` Base Interface

Every piece of game content (Spell, Class, Talent, etc.) must implement the following base interface defined in `src/data/repo.ts`.

```typescript
export interface Entry {
    slug: string; // Permanent URL-friendly identifier
    name: string; // Human-readable name (Russian)
    description?: string; // Content in HTML format
    sources: Array<{
        // List of books/sources
        abbr: string; // Short code (e.g., PC, TOV)
        name: string; // Full name of the source
    }>;
}
```

## Extended Types

### 1. Spells

```typescript
interface SpellEntry extends Entry {
    circle: number; // 0 for cantrips
    school: string; // Magic school (e.g., Evocation)
    isRitual?: boolean; // Ritual tag
    castingTime?: string;
    range?: string;
    components?: string;
    duration?: string;
    classes?: string[]; // List of classes that can cast this
}
```

### 2. Classes

```typescript
interface ClassEntry extends Entry {
    hitDie: string;
    savingThrows: string[];
    proficiencies: {
        armor: string[];
        weapons: string[];
        tools: string[];
        skills: {
            granted?: string[];
            choose?: number;
            from?: string[];
        };
    };
    levels: Array<{
        level: number;
        features: string[];
    }>;
}
```

## Sync Manifest (`site-manifest.json`)

The frontend's `loader.ts` expects a manifest file at the root to determine what to download.

```json
{
  "build": "20251229",
  "categories": {
    "spells": {
      "hash": "abc12345",
      "indexHash": "xyz67890",
      "count": 450
    },
    ...
  }
}
```

-   **hash**: Hash of the data JSON (`data/spells-[hash].json`).
-   **indexHash**: Hash of the search index JSON (`index/spells-[hash].minisearch.json`).
