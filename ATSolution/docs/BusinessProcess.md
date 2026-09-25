flowchart TD
    A[Inventory Item Master] --> B[Cost Price + Optional Buying Price]
    B --> C[Pricing Rule → Selling Price]
    C --> D[Custom Lighting Product]
    D --> E[Product Version V1]
    E --> F[Specifications]
    F --> G[BOM with Inventory Components]
    G --> H[Manufacturing Operations]
    H --> I[Estimated Costing]
    I --> J[Product Selling Price + Margin]
    J --> K{Margin < Minimum?}
    K -->|Yes| L[Extended Approval Path]
    K -->|No| M[Standard Approval Path]
    L --> N[Dynamic Approval Workflow]
    M --> N
    N --> O{Decision}
    O -->|Approve| P[RELEASED - Locked Version]
    O -->|Reject| Q[REJECTED]
    O -->|Request Changes| R[REVISION_REQUIRED → New Version]
    P --> S[Sales Order selects Released Version]
    S --> T[Snapshot BOM + Costing on Order]
    T --> U[Confirm Order → Reserve Materials]
    U --> V{Shortage?}
    V -->|Yes| W[Purchase / Transfer / Alt Component]
    V -->|No| X[Production Job]
    W --> X
    X --> Y[Material Issue + Operation Tracking]
    Y --> Z[Actual Cost Rollup]
    Z --> AA[QC]
    AA -->|Pass| AB[Finished Product Ready]
    AA -->|Fail| AC[Rework → Y]
