# ShopFlow – Architecture Document

## 1. System Overview

ShopFlow is a mobile-first, offline-capable shopping assistant with an AWS serverless backend. The system is split into two independently deployable components: a .NET MAUI mobile app and an AWS CDK-defined cloud backend.

```
┌─────────────────────────────────────┐
│           Mobile App (MAUI)          │
│  ┌──────────┐  ┌──────────────────┐ │
│  │  SQLite  │  │  AWS AppSync     │ │
│  │ (local)  │←→│  (GraphQL API)   │ │
│  └──────────┘  └──────────────────┘ │
│                         ↑           │
│  AWS Cognito ───────────┘           │
│  (auth tokens)                      │
└─────────────────────────────────────┘
                    │ GraphQL (HTTPS + JWT)
                    ▼
┌─────────────────────────────────────────────────────┐
│                   AWS Cloud (us-west-2)              │
│                                                      │
│  ┌──────────────────────────────────────────────┐   │
│  │              AWS AppSync                      │   │
│  │          (GraphQL API Gateway)                │   │
│  │  • Cognito JWT authorization                  │   │
│  │  • VTL resolvers → DynamoDB                  │   │
│  │  • Lambda resolvers for smart features        │   │
│  │  • WebSocket subscriptions                    │   │
│  └──────────┬────────────────────────────────────┘   │
│             │                                         │
│    ┌────────┴──────────────────────────────────┐     │
│    │              DynamoDB Tables               │     │
│    │  • ShopFlow_Items_v2                       │     │
│    │  • ShopFlow_Lists  ──(stream)──┐           │     │
│    │  • ShopFlow_GlobalProducts     │           │     │
│    │  • ShopFlow_DeviceTokens       │           │     │
│    │  • ShopFlow_BarcodeMetrics     │           │     │
│    └────────────────────────────────────────────┘     │
│                                    │                  │
│    ┌───────────────────────────────▼───────────┐     │
│    │           Lambda: InviteUser               │     │
│    │  DynamoDB stream trigger on Lists table    │     │
│    │  → AWS SES (email invitation)              │     │
│    │  → Firebase FCM (push notification)        │     │
│    └────────────────────────────────────────────┘     │
│                                                       │
│    ┌──────────────────┐  ┌─────────────────────┐     │
│    │ Lambda:          │  │ Lambda:              │     │
│    │ ProductSearch    │  │ SmartParser          │     │
│    │ .NET 8           │  │ Node.js 18           │     │
│    │ → OpenFoodFacts  │  │ → Gemini 2.5 Flash   │     │
│    │ → BarcodeMetrics │  │                      │     │
│    └──────────────────┘  └─────────────────────┘     │
│                                                       │
│    ┌──────────────────────────────────────────────┐   │
│    │  AWS Cognito User Pool + Identity Pool        │   │
│    │  (Authentication & Authorization)             │   │
│    └──────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────┘
```

## 2. Mobile Application Architecture

### 2.1 Layer Structure

```
┌──────────────────────────────────────────┐
│              Views (XAML)                │  ← UI only, no logic
│  Pages, Partial Views, Popups            │
├──────────────────────────────────────────┤
│           ViewModels (C#)                │  ← Commands, bindings, orchestration
│  CommunityToolkit.Mvvm source generators │
├──────────────────────────────────────────┤
│            Repository Layer              │  ← Facade: delegates to DB + API
│  ShoppingRepository                      │
├──────────────────────────────────────────┤
│              Services Layer              │  ← Business logic + external integrations
│  Auth, GraphQL, Database, Smart, Voice,  │
│  Barcode, Translation, Notifications...  │
├──────────────────────────────────────────┤
│         Local Storage (SQLite)           │  ← Offline-first persistent store
│  Lists, Items, PurchaseHistory,          │
│  CachedProducts, BarcodeMetrics          │
└──────────────────────────────────────────┘
```

### 2.2 Navigation Flow

```
App Start
    │
    ▼
LoadingPage (async auth check)
    │
    ├── Not authenticated ──→ LoginPage
    │                              │
    │     ┌─── Login ─────────────┤
    │     │    SignUp → Verify ───┤
    │     └───────────────────────┘
    │
    └── Authenticated ──→ AppShell / MainContainerPage
                                │
                    ┌───────────┴────────────┐
                    │                        │
               DashboardView            ReportsView
                    │
                    ▼
              ActiveListPage
             ┌──────┬──────┐
             │      │      │
          Voice   Scan  ItemDetail
```

### 2.3 Key Pages and Responsibilities

| Page | ViewModel | Responsibility |
|------|-----------|----------------|
| `LoadingPage` | — | Auth check; routes to Login or Main |
| `LoginPage` | `LoginViewModel` | SignUp / Login / Verify flows |
| `MainContainerPage` | `MainContainerViewModel` | Tab host: Lists + Reports |
| `DashboardView` (partial) | `DashboardViewModel` | List cards, CRUD on lists |
| `ActiveListPage` | `ActiveListViewModel` | Item list with voice/scan FABs |
| `ItemDetailPage` | `ItemDetailViewModel` | Add/edit item form |
| `ListFormPage` | `ListFormViewModel` | Create/edit list form |
| `ScanPage` | (inline) | ZXing barcode camera feed |
| `SettingsPage` | `SettingsViewModel` | Language, data, notifications |
| `ReportsView` (partial) | `ReportsViewModel` | Purchase stats |
| `LogsPage` | `LogsViewModel` | Debug log viewer |

### 2.4 Services Map

| Service | Interface | Key Responsibility |
|---------|-----------|-------------------|
| `AuthService` | `IAuthService` | Cognito SRP auth, token management |
| `GraphQLService` | `IGraphQLService` | All AppSync API calls |
| `DatabaseService` | `IDatabaseService` | SQLite CRUD (offline store) |
| `ShoppingRepository` | `IShoppingRepository` | Facade: combines DB + GraphQL |
| `SmartService` | `ISmartService` | Regex parsing + suggestion logic |
| `VoiceService` | `IVoiceService` | Speech-to-text, microphone |
| `OpenFoodFactsService` | `IProductService` | Barcode product lookup (via backend) |
| `BarcodeWorkflowService` | `IBarcodeWorkflowService` | Full scan orchestration |
| `AwsTranslateService` | `ITranslationService` | Text translation for smart matching |
| `NotificationService` | `IAppNotificationService` | Local notifications |
| `PushService` | `IPushService` | FCM device registration |
| `LocalizationService` | `ILocalizationService` | Language/culture switching |
| `FileLoggerService` | `ILoggerService` | File-based debug logging |
| `SyncService` | `ISyncService` | Cloud sync (partially implemented) |
| `DialogService` | `IDialogService` | Shell-based alert/prompt dialogs |

### 2.5 Data Models

#### ShoppingList
```csharp
Id (GUID), Name, OwnerId, IsDefault (bool?), Color (hex string), Icon (emoji)
CreatedAt, IsDirty, IsDeleted
// Computed: Initials, PrimaryColor, SecondaryColor, TotalItemsCount, BoughtItemsCount, Progress
```

#### ShoppingItem
```csharp
Id (GUID), ListId, Name, Quantity (double), Unit, Category, Price (double?)
IsPurchased, CreatedAt, PurchasedAt, LastUpdated
IsDirty, IsDeleted, IsParsed, OriginalText
```

#### PurchaseHistory
```csharp
Id (int, PK), ItemName, Category, DatePurchased
```

#### CachedProduct
```csharp
Barcode (PK), Name, Category, ImageUrl, LastUpdated
```

### 2.6 Offline-First Strategy

1. **Reads**: Always from SQLite first; AppSync refreshes the local cache
2. **Writes**: Written to SQLite immediately; `IsDirty = true` flags pending sync
3. **Sync**: `SyncService` pushes dirty records to AppSync when connected
4. **Auth**: Refresh token cached in Secure Storage; app works offline if token is present
5. **Degradation**: Barcode lookup, AI parsing, and sharing require connectivity; shown as unavailable when offline

### 2.7 iOS-Specific Features

- **Siri Integration**: `AppDelegate` registers `INAddTasksIntent` handler; voice command "Add [item] to ShopFlow" adds to default list
- **AVAudioSession**: Configured in `FinishedLaunching` for voice capture in silent mode
- **Firebase Init**: `Firebase.Core.App.Configure()` called at startup for FCM
- **Push Entitlements**: APS production environment enabled in `Entitlements.plist`
- **Siri Vocabulary**: `AppIntentVocabulary.plist` files for en/pt provide Siri with app-specific terms

---

## 3. Backend Architecture

### 3.1 Infrastructure (AWS CDK)

All infrastructure is defined as code in `backend/src/Backend/BackendStack.cs` and deployed with `cdk deploy`.

**CDK Stack: `BackendStack`**

```
BackendStack (us-west-2)
├── Cognito
│   ├── User Pool (ShopFlowUserPool)
│   ├── User Pool Client (ShopFlowClient)  — SRP auth
│   └── Identity Pool (ShopFlowIdentityPool)  — AWS credential exchange
│
├── AppSync API (ShopFlowApi)
│   ├── Auth: Cognito User Pool
│   ├── Schema: schema.graphql
│   ├── Data Sources
│   │   ├── Items DynamoDB source
│   │   ├── Lists DynamoDB source
│   │   ├── GlobalProducts DynamoDB source
│   │   ├── DeviceTokens DynamoDB source
│   │   ├── ProductSearch Lambda source
│   │   └── SmartParser Lambda source
│   └── Resolvers (VTL for DynamoDB, Lambda for smart features)
│
├── DynamoDB
│   ├── ShopFlow_Items_v2 (PK: userId, SK: id)
│   ├── ShopFlow_Lists (PK: id) — DynamoDB Streams enabled
│   ├── ShopFlow_GlobalProducts (PK: barcode)
│   ├── ShopFlow_DeviceTokens (PK: userId, GSI: EmailIndex on email)
│   └── ShopFlow_BarcodeMetrics (PK: barcode, SK: timestamp)
│
├── Lambda Functions
│   ├── SmartParser (Node.js 18, 30s timeout)
│   ├── ProductSearch (.NET 8, 256MB, 10s timeout)
│   ├── InviteUser (.NET 8, 30s timeout, DynamoDB stream trigger)
│   └── SmartSuggestions (.NET 8, 30s timeout) — stub
│
├── SES Identity (sender email)
└── IAM Roles & Policies (least-privilege per function)
```

### 3.2 GraphQL API Design

**Endpoint**: `https://msd5yv5s7fdevbcgwt56giiyy4.appsync-api.us-west-2.amazonaws.com/graphql`
**Auth**: Bearer token (Cognito ID Token) in `Authorization` header

#### Schema Summary

```graphql
type Query {
  getLists: [ShoppingList]
  getList(id: ID!): ShoppingList
  getItems(listId: ID!): [ShoppingItem]
  getGlobalProduct(barcode: String!): GlobalProduct
  productSearch(barcode: String!): Product           # → ProductSearch Lambda
  parseItem(text: String!): SmartParseResult         # → SmartParser Lambda
  getSuggestions: [String]                           # → SmartSuggestions (stub)
}

type Mutation {
  createList(name: String!, isDefault: Boolean, color: String, icon: String): ShoppingList
  updateList(id: ID!, name: String, isDefault: Boolean, color: String, icon: String): ShoppingList
  deleteList(id: ID!): ShoppingList
  shareList(id: ID!, email: String!): ShoppingList   # → triggers InviteUser via stream
  addItem(id: ID, listId: ID!, name: String!, quantity: Float, unit: String, category: String, price: Float): ShoppingItem
  updateItem(listId: ID!, id: ID!, name: String, quantity: Float, price: Float, unit: String, category: String, isPurchased: Boolean): ShoppingItem
  deleteItem(listId: ID!, id: ID!): ShoppingItem
  createGlobalProduct(barcode: String!, name: String!, category: String): GlobalProduct
  registerDevice(token: String!, platform: String!): DeviceToken
}

type Subscription {
  onUpdateList(id: ID!): ShoppingList
  onUpdateItem(listId: ID!): ShoppingItem
}
```

#### Resolver Architecture

| Operation | Resolver Type | Target |
|-----------|--------------|--------|
| `getLists` | VTL | DynamoDB Scan (filter: userId == sub OR email in members) |
| `createList` | VTL | DynamoDB PutItem (auto ID, set owner = sub) |
| `updateList` | VTL | DynamoDB UpdateItem (ownership check) |
| `deleteList` | VTL | DynamoDB DeleteItem (ownership check) |
| `shareList` | VTL | DynamoDB UpdateItem (append email to members list) |
| `getItems` | VTL | DynamoDB Query (GSI on userId) |
| `addItem` | VTL | DynamoDB PutItem |
| `updateItem` | VTL | DynamoDB UpdateItem |
| `deleteItem` | VTL | DynamoDB DeleteItem |
| `getGlobalProduct` | VTL | DynamoDB GetItem by barcode |
| `createGlobalProduct` | VTL | DynamoDB PutItem |
| `registerDevice` | VTL | DynamoDB PutItem (userId, token, platform, email, updatedAt) |
| `productSearch` | Lambda | → ProductSearch Lambda |
| `parseItem` | Lambda | → SmartParser Lambda |

### 3.3 Lambda Functions – Detail

#### SmartParser (Node.js 18)

```
Input:  { arguments: { text: "milk 2l" } }
Output: { name: "Milk", quantity: 2, unit: "l", category: "Dairy & Eggs", emoji: "🥛" }

Flow:
  1. Build Gemini prompt with JSON schema constraint
  2. POST to generativelanguage.googleapis.com (gemini-2.5-flash model)
  3. Specify response_mime_type: "application/json"
  4. Parse candidates[0].content.parts[0].text
  5. Return parsed JSON to AppSync

Env: GEMINI_API_KEY
Timeout: 30s
```

#### ProductSearch (.NET 8)

```
Input:  { barcode: "7891000244400" }
Output: { name: "Leite Integral Nestlé", category: "Groceries", imageUrl: "..." }
        null if not found

Flow:
  1. GET https://world.openfoodfacts.org/api/v0/product/{barcode}.json
  2. Parse JSON (Newtonsoft.Json)
  3. Extract product_name + image_front_url if status == 1
  4. Write metric to ShopFlow_BarcodeMetrics (barcode, timestamp, status: Hit/Miss/Error)
  5. Return ProductResult or null

Env: METRICS_TABLE_NAME (default: ShopFlow_BarcodeMetrics)
Memory: 256 MB, Timeout: 10s
```

#### InviteUser (.NET 8)

```
Trigger: DynamoDB stream on ShopFlow_Lists (INSERT + MODIFY events)

Flow:
  1. Receive stream record (oldImage + newImage)
  2. Diff members arrays to find newly added emails
  3. For each new member email:
     a. Send HTML email via AWS SES (from fEMAIL_SOURCE)
     b. Query ShopFlow_DeviceTokens by email (GSI: EmailIndex)
     c. Send FCM push notification to each registered device token
        via Firebase Admin SDK

Env:
  fEMAIL_SOURCE            — SES sender address
  fDEVICE_TOKENS_TABLE_NAME — DeviceTokens table name
  FIREBASE_SERVICE_ACCOUNT_JSON — Firebase Admin credentials
Timeout: 30s
```

#### SmartSuggestions (.NET 8) — Stub

```
Status: Not integrated into AppSync
Current behavior: Returns ToUpper() of input string (placeholder)
Future intent: ML-based shopping suggestions from purchase patterns
```

### 3.4 Authentication & Authorization Flow

```
1. App → Cognito User Pool (SRP auth with username + password)
2. Cognito → App: ID Token (JWT), Access Token, Refresh Token
3. App stores: Refresh Token in Secure Storage, ID Token in memory
4. API call: App → AppSync with Header: Authorization: {ID Token}
5. AppSync validates JWT signature with Cognito User Pool
6. AppSync extracts `sub` (userId) from JWT claims
7. VTL resolvers use $context.identity.sub as the userId for data scoping
8. Identity Pool (optional): exchanges Cognito JWT for temporary AWS credentials
   → Used for direct AWS SDK calls (Translate, etc.)
```

### 3.5 Data Access Patterns

| Access Pattern | DynamoDB Operation | Key |
|---------------|-------------------|-----|
| Get user's lists | Scan with filter | userId == sub OR email in members |
| Get list by ID | GetItem | PK: id |
| Get items for list | Query | PK: userId, filter listId |
| Lookup barcode | GetItem | PK: barcode |
| Get device tokens by email | Query GSI | EmailIndex: email |
| Log barcode metric | PutItem | PK: barcode, SK: timestamp |
| Register device | PutItem | PK: userId |

---

## 4. CI/CD Pipeline

### 4.1 Frontend Pipeline (GitHub Actions)

**File**: `.github/workflows/ios-deploy.yml`
**Runner**: `macos-26`
**Trigger**: Push to `main` OR manual `workflow_dispatch`

```
Checkout
    ↓
Setup .NET 10 + MAUI workloads (maui-ios, maui-android, maui-maccatalyst)
    ↓
Restore Firebase config files from base64 secrets
  • ShopFlow/Platforms/iOS/GoogleService-Info.plist
  • ShopFlow/Platforms/Android/google-services.json
    ↓
Install Apple P12 signing certificate (apple-actions/import-codesign-certs)
    ↓
Download Provisioning Profile (apple-actions/download-provisioning-profiles)
  Profile: GitHubAppleStore (IOS_APP_STORE)
    ↓
Select Xcode 26.2
    ↓
Inject GOOGLE_API_KEY into SmartService.cs (sed replacement)
    ↓
dotnet publish -f net10.0-ios -c Release -p:ArchiveOnBuild=true
  CodesignKey: "Apple Distribution: Breno Batista Machado (8L386BMNSL)"
  CodesignProvision: "GitHubAppleStore"
  → Output: ShopFlow/bin/Release/net10.0-ios/ios-arm64/publish/ShopFlow.ipa
    ↓
Upload to TestFlight (apple-actions/upload-testflight-build)
```

### 4.2 Backend Pipeline

Backend is deployed manually:
```bash
cd backend
cdk deploy
```

No automated CI/CD currently configured for backend.

### 4.3 Required GitHub Secrets

| Secret | Description |
|--------|-------------|
| `GOOGLE_SERVICE_INFO_PLIST` | Base64 `GoogleService-Info.plist` (Firebase iOS) |
| `GOOGLE_SERVICE_JSON` | Base64 `google-services.json` (Firebase Android) |
| `BUILD_CERTIFICATE_BASE64` | Base64 Apple Distribution P12 |
| `P12_PASSWORD` | P12 certificate password |
| `APP_STORE_CONNECT_ISSUER_ID` | App Store Connect API issuer UUID |
| `APP_STORE_CONNECT_API_KEY_ID` | App Store Connect API key ID |
| `APP_STORE_CONNECT_API_KEY_CONTENT` | App Store Connect API private key |
| `GOOGLE_API_KEY` | Google API key (injected into SmartService.cs) |

---

## 5. External Dependencies

| Service | Provider | Used By | Auth Method |
|---------|----------|---------|-------------|
| OpenFoodFacts | Open source | ProductSearch Lambda | None (public API) |
| Google Gemini 2.5 Flash | Google | SmartParser Lambda | API Key |
| Firebase Cloud Messaging | Google | InviteUser Lambda, Mobile app | Service Account JSON / GoogleService-Info.plist |
| AWS SES | AWS | InviteUser Lambda | IAM role |
| AWS Translate | AWS | Mobile app (BarcodeWorkflowService) | Cognito Identity Pool credentials |
| Apple TestFlight | Apple | CI/CD | App Store Connect API key |

---

## 6. Security Model

### 6.1 API Security
- All AppSync operations require a valid Cognito ID Token
- Token validated by AppSync against the User Pool JWKS
- No unauthenticated access (Identity Pool disables unauthenticated identities)

### 6.2 Data Isolation
- `userId` = Cognito `sub` claim (unique per user, immutable)
- Lists: user sees lists where `userId == sub` OR `email in members`
- Items: `userId` is both partition key and access filter
- `shareList` and `deleteList` mutations enforce ownership in VTL before write

### 6.3 Secrets Management
- API keys and certificates are GitHub Actions secrets (base64-encoded where binary)
- Firebase config files are excluded from git and restored at build time
- `GEMINI_API_KEY` and `FIREBASE_SERVICE_ACCOUNT_JSON` are Lambda environment variables set at deploy time
- `GOOGLE_API_KEY` is injected into source at CI build time (not committed)

### 6.4 Token Lifecycle
- ID Token: short-lived (1 hour), cached in memory, refreshed via refresh token
- Refresh Token: long-lived, stored in iOS Keychain via SecureStorage
- Token rotation: new tokens replace old on each refresh

---

## 7. Known Architectural Gaps

1. **Item access for shared list members**: `getItems` resolver queries by `userId` as partition key. Members of a shared list who don't own the items cannot retrieve them. Fix requires either a secondary access pattern or a GSI on `listId`.

2. **SmartSuggestions Lambda**: Registered in CDK but not wired to any AppSync resolver. `getSuggestions` query returns a stub.

3. **Sync service**: `ISyncService` and `IOfflineSyncService` are referenced but not fully implemented. Dirty-flag infrastructure is in place; sync push logic is incomplete.

4. **Direct DynamoDB SDK in MAUI**: `AWSSDK.DynamoDBv2` is a dependency of the MAUI project but all data access goes through AppSync. The SDK is unused at runtime.

5. **Pinpoint integration**: Commented out in `BackendStack.cs` — push analytics are not operational.

6. **Backend CI/CD**: No automated pipeline for CDK deployments. Changes require manual `cdk deploy`.
