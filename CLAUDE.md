# ShopFlow – Claude Code Guide

## What This Project Is

ShopFlow is a smart shopping assistant. The user speaks, types, or scans a barcode; the app adds structured items to a shared shopping list. It works offline-first and syncs to AWS when connected.

- **Frontend**: .NET MAUI 10 (`ShopFlow/`) — iOS is the primary target
- **Backend**: AWS Serverless (`backend/`) — CDK-defined infrastructure, AppSync GraphQL, 4 Lambda functions, DynamoDB
- **CI/CD**: GitHub Actions (`ios-deploy.yml`) builds and publishes the iOS `.ipa` to Apple TestFlight on every push to `main`

## Repository Layout

```
smart-shopping/
├── ShopFlow/                    # .NET MAUI mobile app
│   ├── App.xaml / App.xaml.cs   # App entry, window creation
│   ├── AppShell.xaml            # Navigation shell, route registration
│   ├── Views/                   # XAML pages + partial views + popups
│   ├── ViewModels/              # MVVM view models (CommunityToolkit.Mvvm)
│   ├── Services/                # All service interfaces + implementations
│   ├── Repositories/            # ShoppingRepository facade
│   ├── Models/                  # Data models (ShoppingList, ShoppingItem, etc.)
│   ├── Helpers/                 # Utility helpers
│   ├── Converters/              # XAML value converters
│   ├── Messages/                # MVVM messenger messages
│   ├── Platforms/iOS/           # iOS-specific code (AppDelegate, Siri, signing)
│   ├── Resources/               # Images, fonts, strings, styles, splash
│   └── ShopFlow.csproj          # Project file (multi-target)
│
├── backend/
│   ├── src/Backend/             # CDK stack definition (C#)
│   │   ├── BackendStack.cs      # ALL AWS resources defined here
│   │   ├── schema.graphql       # AppSync GraphQL schema
│   │   └── resolvers/           # VTL resolver templates (DynamoDB mappings)
│   └── src/Lambdas/
│       ├── SmartParser/         # Node.js 18 — Gemini AI text parsing
│       ├── ProductSearch/       # .NET 8 — OpenFoodFacts barcode lookup
│       ├── InviteUser/          # .NET 8 — DynamoDB stream → SES email + FCM push
│       └── SmartSuggestions/    # .NET 8 — Stub/placeholder (not integrated)
│
├── .github/workflows/
│   └── ios-deploy.yml           # GitHub Actions: build → sign → TestFlight
│
├── ShopFlow.sln                 # Solution (MAUI project only; backend is separate)
├── Directory.Build.targets      # iOS Info.plist path override
├── nuget.config                 # Points only to nuget.org
└── CLAUDE.md                    # This file
```

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Mobile UI | .NET MAUI 10, XAML |
| MVVM | CommunityToolkit.Mvvm 8.4 (source generators) |
| Local DB | SQLite via sqlite-net-pcl |
| Cloud API | AWS AppSync (GraphQL) |
| Auth | AWS Cognito User Pool + Identity Pool |
| Push Notifications | Firebase Cloud Messaging (Plugin.Firebase) |
| Barcode Scanning | ZXing.Net.MAUI |
| Voice Input | CommunityToolkit.Maui speech-to-text + Siri (iOS) |
| Translation | AWS Translate (smart matching) |
| Infrastructure | AWS CDK (.NET) |
| AI Parsing | Google Gemini 2.5 Flash (SmartParser Lambda) |
| Product DB | OpenFoodFacts public API |
| Email | AWS SES |
| CI/CD | GitHub Actions → Apple TestFlight |
| AWS Region | us-west-2 |

## Key Configuration

### AWS Endpoints (hardcoded in source)
File: `ShopFlow/Services/AwsConfig.cs`
```
UserPoolId:      us-west-2_mQ39tQDFE
UserPoolClientId: 61fcskms6g0o2epem6lrbvge58
IdentityPoolId:  us-west-2:4e930d96-c00e-4d93-aa32-8b0a2f3d76df
GraphQL URL:     https://msd5yv5s7fdevbcgwt56giiyy4.appsync-api.us-west-2.amazonaws.com/graphql
Region:          us-west-2
```

### Secrets (not in repo)
| Secret | Where Needed |
|--------|-------------|
| `GoogleService-Info.plist` | `ShopFlow/Platforms/iOS/` — Firebase iOS config |
| `google-services.json` | `ShopFlow/Platforms/Android/` — Firebase Android config |
| Apple P12 certificate | GitHub Actions secret `BUILD_CERTIFICATE_BASE64` |
| `GEMINI_API_KEY` | SmartParser Lambda environment variable |
| `FIREBASE_SERVICE_ACCOUNT_JSON` | InviteUser Lambda environment variable |
| App Store Connect API key | GitHub Actions secrets `APP_STORE_CONNECT_*` |
| `GOOGLE_API_KEY` | Injected into SmartService.cs by GitHub Actions (sed) |

## Build & Deploy

### Frontend (iOS) — How it ships
Push to `main` → GitHub Actions runs `ios-deploy.yml` → Builds `net10.0-ios` Release → Signs with Apple Distribution cert → Uploads `.ipa` to TestFlight.

To build locally (publish for App Store):
```powershell
dotnet publish ShopFlow/ShopFlow.csproj -f net10.0-ios -c Release -p:ArchiveOnBuild=true
```
Output: `ShopFlow/bin/Release/net10.0-ios/ios-arm64/publish/ShopFlow.ipa`

Bundle ID: `com.brenomachado.shopflow`
Min iOS version: 15.0

### Backend (AWS) — How it ships
Deploy manually via CDK from the `backend/` directory:
```bash
cd backend
cdk deploy
```
CDK exports: UserPoolId, UserPoolClientId, IdentityPoolId, Region, GraphQLEndpoint

## Development Patterns

### MVVM Architecture
```
View (XAML) → ViewModel → Repository → Services → (SQLite | GraphQL | AWS)
```
- All ViewModels extend `BaseViewModel` (provides `IsBusy`, `IsNotBusy`, `Title`)
- Use `[ObservableProperty]` and `[RelayCommand]` attributes (CommunityToolkit source generators)
- Collections use `ObservableCollection<T>`

### Service Registration
All services are registered in `MauiProgram.cs` as singletons or transients. When adding a new service:
1. Create interface in `Services/`
2. Create implementation in `Services/`
3. Register in `MauiProgram.cs`
4. Inject via constructor in the ViewModel

### Offline-First Data Flow
1. All reads/writes go to local SQLite first (`DatabaseService`)
2. Cloud sync via `ShoppingRepository` delegates to `GraphQLService` for cloud operations
3. Items have `IsDirty` and `IsDeleted` flags for deferred sync
4. Auth: if offline and cached refresh token exists, the app proceeds without network

### Navigation
- Route-based: `Shell.Current.GoToAsync("route?param=value")`
- Routes registered in `AppShell.xaml.cs`
- Query parameters received via `[QueryProperty]` in ViewModels

### Adding a GraphQL Operation
1. Add to `schema.graphql` in the backend
2. Add resolver VTL template in `backend/src/Backend/resolvers/`
3. Wire resolver in `BackendStack.cs`
4. Add method to `IGraphQLService` interface and `GraphQLService` implementation
5. Expose via `IShoppingRepository` / `ShoppingRepository` if needed

## DynamoDB Tables

| Table | Partition Key | Sort Key | Purpose |
|-------|--------------|----------|---------|
| `ShopFlow_Items_v2` | `userId` | `id` | Shopping items |
| `ShopFlow_Lists` | `id` | — | Shopping lists (stream enabled) |
| `ShopFlow_GlobalProducts` | `barcode` | — | Global product catalog |
| `ShopFlow_DeviceTokens` | `userId` | — | Push notification tokens |
| `ShopFlow_BarcodeMetrics` | `barcode` | `timestamp` | Barcode search analytics |

## Lambda Functions

| Lambda | Runtime | Trigger | Purpose |
|--------|---------|---------|---------|
| `SmartParser` | Node.js 18 | AppSync query `parseItem` | Uses Gemini 2.5 Flash to parse freeform text |
| `ProductSearch` | .NET 8 | AppSync query `productSearch` | Looks up barcode on OpenFoodFacts |
| `InviteUser` | .NET 8 | DynamoDB stream (Lists table) | Sends email + FCM on list member add |
| `SmartSuggestions` | .NET 8 | Unused | Stub — not integrated |

## GraphQL API Reference

**Queries**: `getLists`, `getList(id)`, `getItems(listId)`, `getGlobalProduct(barcode)`, `productSearch(barcode)`, `parseItem(text)`

**Mutations**: `createList`, `updateList`, `deleteList`, `shareList(id, email)`, `addItem`, `updateItem`, `deleteItem`, `createGlobalProduct`, `registerDevice(token, platform)`

**Subscriptions**: `onUpdateList(id)`, `onUpdateItem(listId)`

## iOS-Specific Notes

- **Siri integration**: `AppDelegate.cs` handles `INAddTasksIntent` — "Siri, add milk to ShopFlow" → adds to default list
- **Microphone/Speech permissions**: declared in `Info.plist`, activated in `AppDelegate.FinishedLaunching`
- **Background notifications**: configured in `Entitlements.plist`
- **Signing** (Release): CodesignKey = "iPhone Distribution", Provision = "ShopFlow Deployment"
- **GitHub Actions signing**: CodesignKey = "Apple Distribution: Breno Batista Machado (8L386BMNSL)", Provision = "GitHubAppleStore"

## Common Tasks

### Add a new shopping list field
1. Add to `ShoppingList` model
2. Add to `schema.graphql` (type + mutation args)
3. Update VTL resolvers in `backend/src/Backend/resolvers/`
4. Update `DatabaseService` SQLite schema
5. Update `GraphQLService` query/mutation strings
6. Update `ListFormPage.xaml` + `ListFormViewModel`

### Add a new Lambda function
1. Create project under `backend/src/Lambdas/YourLambda/`
2. Implement handler in `Function.cs`
3. Add to `BackendStack.cs`: create Function, grant permissions, wire AppSync data source + resolver
4. Add schema entry in `schema.graphql`
5. Deploy with `cdk deploy`

### Change app language strings
Strings live in `ShopFlow/Resources/Strings/` — separate files per locale (en-CA, pt-BR).

### Test push notifications locally
Settings page → "Test Push" button → triggers `IAppNotificationService.ShowLocalNotification()`

## GitHub Actions Secrets Reference

| Secret Name | Description |
|-------------|-------------|
| `GOOGLE_SERVICE_INFO_PLIST` | Base64-encoded `GoogleService-Info.plist` |
| `GOOGLE_SERVICE_JSON` | Base64-encoded `google-services.json` |
| `BUILD_CERTIFICATE_BASE64` | Base64-encoded P12 certificate |
| `P12_PASSWORD` | P12 certificate password |
| `APP_STORE_CONNECT_ISSUER_ID` | App Store Connect issuer UUID |
| `APP_STORE_CONNECT_API_KEY_ID` | App Store Connect API key ID |
| `APP_STORE_CONNECT_API_KEY_CONTENT` | App Store Connect API private key |
| `GOOGLE_API_KEY` | Google API key (injected into SmartService.cs) |

## Known Limitations & Notes

- `SmartSuggestions` Lambda is a stub — `getSuggestions` query returns placeholder data
- Pinpoint integration is commented out in `BackendStack.cs`
- The `getItems` AppSync resolver queries by userId, not listId — shared list members can't see items they don't own (known gap)
- AWS DynamoDB SDK is imported in the MAUI project but sync is fully delegated to AppSync (the SDK is unused at runtime)
- `UseInterpreter: true` in iOS Release config for smaller binary size (trade-off: slower startup)
