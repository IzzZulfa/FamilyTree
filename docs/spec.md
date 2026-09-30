# Family Tree App — Spec

## Overview
A web app for building and viewing a family tree: add people, connect them as parents, children and partners, and explore ancestors and descendants visually.

## Data model

People are nodes; relationships are edges stored in their own tables. Derived relationships (siblings, grandparents, cousins) are **never stored** — they are computed from the edges.

### FamilyTree
| Field | Type | Notes |
|---|---|---|
| Id | int | PK |
| Name | string | required |

### Person
| Field | Type | Notes |
|---|---|---|
| Id | int | PK |
| TreeId | int | FK → FamilyTree |
| GivenName | string | required |
| Surname | string | |
| BirthSurname | string? | e.g. maiden name |
| Sex | enum | Male, Female, Unknown |
| BirthDate | FuzzyDate? | |
| BirthPlace | string? | |
| DeathDate | FuzzyDate? | |
| Notes | string? | |
| PhotoUrl | string? | Phase 4 |

### ParentChild
| Field | Type | Notes |
|---|---|---|
| Id | int | PK |
| ParentId | int | FK → Person |
| ChildId | int | FK → Person |
| Type | enum | Biological, Adoptive, Step, Foster |

Unique on (ParentId, ChildId).

### Partnership
| Field | Type | Notes |
|---|---|---|
| Id | int | PK |
| Person1Id | int | FK → Person |
| Person2Id | int | FK → Person |
| Type | enum | Married, Partner, Engaged |
| StartDate | FuzzyDate? | |
| EndDate | FuzzyDate? | |
| EndReason | enum? | Divorce, Death |

### FuzzyDate (EF Core owned type)
| Field | Type | Notes |
|---|---|---|
| Value | DateOnly | |
| Precision | enum | Exact, Month, Year, Circa |

## Validation rules
1. **No cycles:** a person cannot be their own ancestor. Before adding a ParentChild edge, reject it if the proposed parent is already a descendant of the child. *(error)*
2. **Max two biological parents** per person. Other parent types are unlimited. *(error)*
3. **No self-relationships:** a person cannot be their own parent or partner. *(error)*
4. **No duplicate partnerships** between the same pair with overlapping dates. *(error)*
5. **Date sanity:** a parent should be born before their child, and death should not precede birth. *(warning — returned in the response but does not block the save, since real records are messy)*
6. All people in a relationship must belong to the same tree. *(error)*

## API

```
GET    /api/trees
POST   /api/trees
GET    /api/trees/{treeId}/people
POST   /api/trees/{treeId}/people
GET    /api/people/{id}
PUT    /api/people/{id}
DELETE /api/people/{id}                 removes the person and their edges

POST   /api/people/{id}/parents         { parentId, type }
POST   /api/people/{id}/partnerships    { partnerId, type, startDate?, endDate?, endReason? }
DELETE /api/parent-child/{id}
DELETE /api/partnerships/{id}

GET    /api/people/{id}/ancestors?generations=4
GET    /api/people/{id}/descendants?generations=4
GET    /api/people/{a}/relationship-to/{b}     Phase 4
GET    /api/trees/{treeId}/graph               all people + all edges, for rendering
```

Validation failures return `400` with ProblemDetails. Date-sanity warnings are returned in a `warnings` array on successful responses.

## Build phases

### Phase 1 — Data & CRUD
Entities, EF Core configuration, migrations, CRUD endpoints for trees, people and relationships, all validation rules, and a seed loader for `/seed` data.
**Done when:** everything works via Swagger, and every validation rule has passing tests.

### Phase 2 — Traversal
Ancestor and descendant queries (start with in-memory BFS over the loaded tree; later add a recursive-CTE version and compare), plus the `/graph` endpoint.
**Done when:** traversal is correct on the seed data, including remarriage, adoption and pedigree collapse, with tests.

### Phase 3 — Frontend
Next.js app that renders the tree with React Flow + ELK. Couples are shown side by side. A focused view shows N generations up and down from the selected person. Click a person to view/edit details or add a parent, child or partner.
**Done when:** the seed family renders readably and relatives can be added from the UI.

### Phase 4 — Extras
Relationship calculator (lowest common ancestor → "first cousin once removed" etc.), photo uploads, search by name.

### Phase 5 — GEDCOM
Import and export of GEDCOM files.

## Out of scope (until explicitly requested)
- Authentication, user accounts and sharing
- Multiple users editing the same tree
- Sources/citations for records
- Deployment and hosting
- Mobile app
