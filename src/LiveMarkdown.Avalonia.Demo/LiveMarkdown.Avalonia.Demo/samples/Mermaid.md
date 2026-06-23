# Mermaid Samples

This page collects representative Mermaid diagrams for the native renderer. Some diagram types are intentionally included before their Avalonia renderer is complete, so this file can work as a living implementation checklist.

## Flowchart

```mermaid
---
title: Native Flowchart
---
%%{init: {"theme": "base", "title": "Flowchart Init Title"}}%%
flowchart TD
    accTitle: Flowchart sample
    accDescr: A small flowchart with node shapes, edge labels, styles, and markdown labels.

    Start([Start])
    Parse["`**Parse** markdown`"]
    Cache{Cache hit?}
    Render[/Render native/]
    Error((Error))
    Done([Done])

    Start --> Parse
    Parse -->|yes| Cache
    Cache -->|hit| Done
    Cache -.->|miss| Render
    Render ==> Done
    Parse -->|invalid| Error

    subgraph Pipeline [Rendering Pipeline]
        Parse
        Cache
        Render
    end

    classDef accent fill:#e8f3ff,stroke:#2f80ed,color:#123;
    classDef danger fill:#ffe8e8,stroke:#d64545,color:#3a1111;
    class Parse,Render accent
    class Error danger
```

## State Diagram

```mermaid
stateDiagram-v2
    [*] --> Idle
    Idle --> Loading : request
    Loading --> Ready : success
    Loading --> Error : failure
    Error --> Loading : retry
    Ready --> [*]

    classDef hot fill:#fff4cc,stroke:#c49000,color:#2f2400
    class Loading hot
```

## Sequence Diagram

```mermaid
sequenceDiagram
    actor User as User
    participant UI as LiveMarkdown UI
    participant Renderer as MermaidPresenter
    participant Parser as Mermaider

    User->>UI: Open mermaid.md
    UI->>Renderer: Set Text
    Renderer->>Parser: Preprocess and parse
    Parser-->>Renderer: Positioned diagram
    Renderer-->>UI: Draw to DrawingContext

    alt Parse succeeds
        UI-->>User: Native diagram
    else Parse fails
        UI-->>User: Error fallback
    end

    Note over Renderer,Parser: init/frontmatter/accessibility lines are stripped before parsing
```

## Class Diagram

```mermaid
classDiagram
    namespace LiveMarkdown {
        class MermaidPresenter {
            +Text string
            +NodeLabelFontSize double
            +Render(DrawingContext) void
        }

        class MermaidTextRenderer {
            <<internal>>
            +DrawInlineText() void
            +DrawTextWithBackground() void
        }

        class MermaidInlineTextParser {
            <<internal>>
            +ParseMarkdown() MermaidTextLayout
            +ParseMermaiderHtmlLike() MermaidTextLayout
        }
    }

    MermaidPresenter --> MermaidTextRenderer : uses
    MermaidTextRenderer --> MermaidInlineTextParser : parses labels
```

## ER Diagram

```mermaid
erDiagram
    CUSTOMER ||--o{ ORDER : places
    ORDER ||--|{ ORDER_ITEM : contains
    PRODUCT ||--o{ ORDER_ITEM : appears_in

    CUSTOMER {
        string id PK
        string name
        string email UK "contact email"
    }

    ORDER {
        string id PK
        date createdAt
        decimal total
    }

    ORDER_ITEM {
        string id PK
        int quantity
    }

    PRODUCT {
        string id PK
        string name
        decimal price
    }
```

## Pie Chart

```mermaid
pie showData
    title Renderer Work Split
    "Flowchart and State" : 35
    "Shared helpers" : 20
    "Sequence/Class/ER" : 25
    "Other charts" : 20
```

## Quadrant Chart

```mermaid
quadrantChart
    title Renderer Priorities
    x-axis Low complexity --> High complexity
    y-axis Low visual risk --> High visual risk
    quadrant-1 Polish later
    quadrant-2 Design carefully
    quadrant-3 Quick wins
    quadrant-4 Implement first

    Flowchart: [0.25, 0.35]
    Sequence: [0.55, 0.75]
    Class: [0.65, 0.70]
    ER: [0.60, 0.65]
    Pie: [0.30, 0.25]
```

## Timeline

```mermaid
timeline
    title Native Mermaid Renderer Roadmap
    section Foundation
    Step 1 : Preprocessing : Presenter state
    Step 2 : Styled font sizes : Shared text helpers
    section Renderers
    Step 3 : Sequence : Class : ER
    Step 4 : Pie : Quadrant : Timeline
    section Compatibility
    Step 5 : Theme directives : Automation metadata
```

## Git Graph

```mermaid
gitGraph
    commit id: "init"
    branch native-renderer order: 2
    checkout native-renderer
    commit id: "flowchart"
    commit id: "text" tag: "helpers"
    checkout main
    commit id: "docs"
    merge native-renderer id: "merge-native" tag: "demo"
```

## Radar Chart

```mermaid
radar-beta
    title Renderer Coverage
    axis flow["Flowchart"], state["State"], seq["Sequence"], cls["Class"], er["ER"], charts["Charts"]
    min 0
    max 100
    graticule polygon
    curve current["Current"]{90, 80, 15, 10, 10, 5}
    curve target["Target"]{100, 100, 90, 90, 85, 80}
```

## Treemap

```mermaid
treemap-beta
  "Mermaid Native Renderer"
    "Foundation"
      "Preprocessor": 20
      "Presenter state": 20
      "Styles": 15
    "Text"
      "Markdown parser": 12
      "HTML-like parser": 10
      "FormattedText": 8
    "Diagram Renderers"
      "Sequence": 15
      "Class": 12
      "ER": 10
```

## Venn Diagram

```mermaid
venn-beta
    set Markdig["Markdown"]: 100
    set Mermaider["Mermaid model"]: 100
    set Avalonia["DrawingContext"]: 100
    union Markdig, Mermaider["Preprocessed labels"]
    union Mermaider, Avalonia["Native layout"]
    union Markdig, Avalonia["FormattedText spans"]
```
