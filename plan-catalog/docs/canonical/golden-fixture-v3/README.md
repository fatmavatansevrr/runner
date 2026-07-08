
# Appsel Canonical Source Pack

This directory contains the approved source material used for Appsel Plan Catalog domain-content authoring.

Only files explicitly designated as canonical may be used to classify catalog values as `CANONICAL_CONFIRMED`.

Passing tests, existing catalog JSON files, implementation defaults, filenames such as `Canonical` or `Reconciled`, and archived documents are not canonical evidence by themselves.

---

## Directory structure

```text
docs/
├── archive/
│   └── Historical or superseded source material
│
├── canonical/
│   ├── README.md
│   └── golden-fixture-v3/
│       ├── golden-10k-intermediate-4d-12w.v3.md
│       ├── golden-10k-intermediate-4d-12w.v3.decisiontrace.json
│       ├── golden-10k-intermediate-4d-12w.v3.plandocument.json
│       └── progression_rules_v2.yaml
│
├── pending/
│   └── Candidate sources that have not yet been approved as canonical
│
└── specifications/
    └── Supporting specifications and architecture documents
```
