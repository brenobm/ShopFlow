# ShopFlow – Functional Requirements

## 1. Overview

ShopFlow is a smart shopping assistant mobile application. Its core purpose is to make building, sharing, and executing a shopping trip as fast and frictionless as possible — through voice input, barcode scanning, AI-powered text parsing, and real-time list sharing.

**Target Platform**: iOS (primary), Android (secondary)
**User Base**: Individual users and small groups (families, housemates)
**Languages**: English (en-CA) and Portuguese (pt-BR)

---

## 2. User Authentication

### FR-AUTH-01: Sign Up
- A new user can register with a username, password, and email address.
- The system sends a verification code to the provided email.
- The user must enter the verification code to activate the account.
- Passwords must satisfy AWS Cognito's default password policy.

### FR-AUTH-02: Login
- A registered user can log in using their username and password.
- The app uses SRP (Secure Remote Password) authentication — passwords are never sent in plaintext.
- On successful login, ID and refresh tokens are stored in the device's secure storage.

### FR-AUTH-03: Session Persistence
- The app remembers the user's session across app restarts via a stored refresh token.
- If the device is online, the session is validated with Cognito on app launch.
- If the device is offline and a cached refresh token exists, the user is considered authenticated (offline-first).
- If the refresh token is expired, the user is redirected to the login screen.

### FR-AUTH-04: Sign Out
- The user can sign out from the Settings screen.
- On sign out, all tokens are cleared from secure storage.

---

## 3. Shopping Lists

### FR-LIST-01: View All Lists
- After login, the user sees all their shopping lists on the Dashboard.
- The user sees lists they own AND lists shared with them (where their email is in the members array).
- Each list card displays: name, icon (emoji), color accent, item count, purchase progress (bought/total).

### FR-LIST-02: Create a List
- The user can create a new shopping list by tapping the "+" button on the Dashboard.
- Required: list name.
- Optional: icon (emoji picker), color accent, set as default list.
- The new list is saved locally (SQLite) and synced to the cloud (DynamoDB via AppSync).

### FR-LIST-03: Edit a List
- The user can edit a list's name, icon, color, and default status by swiping on the list card.
- Changes are saved locally and synced to the cloud.

### FR-LIST-04: Delete a List
- The user can delete a list by swiping on the list card and confirming.
- Deletion is immediate in the UI; the record is soft-deleted locally and removed from the cloud.

### FR-LIST-05: Default List
- One list can be marked as the default list.
- The default list is the target when items are added via Siri shortcuts.
- Only one list can be the default at a time.

### FR-LIST-06: Share a List
- The list owner can share the list with another user by entering their email address.
- The system sends an invitation email (via AWS SES) to the invited user.
- The system sends a push notification (via FCM) to all registered devices of the invited user.
- The invited user sees the shared list in their Dashboard after accepting (by being added to members).

### FR-LIST-07: Real-Time Sync
- Changes to a list (items added/removed/updated) are broadcast to all members via AppSync subscriptions.
- A member viewing a list sees updates in real time without refreshing.

---

## 4. Shopping Items

### FR-ITEM-01: View Items
- Tapping a list opens the Active List screen.
- Items are displayed grouped by category (e.g., Produce, Dairy & Eggs, Meat & Poultry, Bakery, Frozen Foods, Pantry, Beverages, Household, Personal Care, Other).
- Each item shows: name, quantity, unit, optional price, category emoji, and purchased status.

### FR-ITEM-02: Add an Item (Manual)
- The user can tap "+" in the Active List to add an item.
- A dialog prompt accepts free-form text (e.g., "milk 2l", "3 apples").
- The SmartService parses the text locally (regex) to extract name, quantity, and unit.
- The item is saved locally and synced to the cloud.

### FR-ITEM-03: Smart Item Parsing (AI)
- Items entered as free-form text are sent to the SmartParser Lambda (Google Gemini 2.5 Flash) for structured parsing.
- The Lambda returns: name (capitalized), quantity (number), unit (kg/l/un/etc.), category, and emoji.
- Parsed results update the stored item record.
- Supports both English and Portuguese input.

### FR-ITEM-04: Add an Item via Voice
- The user can hold the microphone FAB (floating action button) to record voice input.
- Speech is transcribed to text using the device's speech recognition engine.
- The transcribed text is processed by SmartService (same parsing pipeline as manual entry).
- Releasing the button finalizes the recording and adds the item.

### FR-ITEM-05: Add an Item via Siri (iOS)
- The user can say "Hey Siri, add [item] to ShopFlow" without opening the app.
- The `INAddTasksIntent` is received by `AppDelegate.ContinueUserActivity()`.
- The item is added to the user's default list.

### FR-ITEM-06: Add an Item via Barcode Scan
- The user taps the camera FAB to open the barcode scanner.
- The ZXing scanner reads the product barcode.
- The system looks up the product in this order:
  1. Local SQLite cache (`CachedProduct` table)
  2. Global product catalog (`ShopFlow_GlobalProducts` DynamoDB table)
  3. OpenFoodFacts public API (via `ProductSearch` Lambda)
- If found, the product name, category, and image are used to pre-fill the item.
- If not found, the user is prompted to manually register the product in the global catalog.
- Barcode search metrics (hit/miss) are logged to `ShopFlow_BarcodeMetrics`.

### FR-ITEM-07: Mark Item as Purchased
- The user can tap a checkbox or the item row to toggle the "purchased" state.
- Purchased items are visually distinguished (e.g., strikethrough, muted color).
- The change syncs to the cloud in real time.

### FR-ITEM-08: Edit an Item
- The user can swipe an item to edit its name.
- A dialog prompt appears pre-filled with the current name.
- Full item editing (name, quantity, unit, price, category) is available via the Item Detail screen.

### FR-ITEM-09: Delete an Item
- The user can swipe an item and confirm to delete it.
- Deletion is immediate in the UI and synced to the cloud.

### FR-ITEM-10: Sort Items
- The user can sort items within a list by: Category, Name, Date Added, or Status (purchased/unpurchased).
- Sort preference is applied immediately in the UI.

### FR-ITEM-11: Quick Quantity Adjustment
- A long-press or swipe gesture on an item can open the Quick Tune popup.
- The popup allows incrementing/decrementing quantity and selecting a unit quickly.

### FR-ITEM-12: Shopping Mode
- The Active List screen has a "Shopping Mode" toggle.
- In shopping mode, the list is optimized for in-store use (larger touch targets, item focus).

---

## 5. Smart Suggestions

### FR-SUGG-01: Purchase History-Based Suggestions
- The app tracks purchase history locally (`PurchaseHistory` table) when items are marked as purchased.
- On the Active List screen, a suggestion bar shows frequently purchased items not yet on the current list.
- Tapping a suggestion adds it to the list instantly.

### FR-SUGG-02: Suggestion Generation
- Suggestions are derived from the `GetSuggestionsAsync()` method in SmartService.
- Logic is based on local purchase history frequency.
- The backend `SmartSuggestions` Lambda is reserved for future cloud-based suggestions.

---

## 6. Barcode Product Catalog

### FR-PROD-01: Global Product Lookup
- Any barcode searched across all users contributes to the global product catalog.
- If OpenFoodFacts returns a result, it is cached in `ShopFlow_GlobalProducts`.
- Future scans of the same barcode retrieve from the global catalog without hitting OpenFoodFacts again.

### FR-PROD-02: User Product Registration
- If a barcode is not found anywhere, the user is prompted to enter the product name and category.
- The new product is saved to the `ShopFlow_GlobalProducts` catalog for all users to benefit from.

---

## 7. Push Notifications

### FR-NOTIF-01: Device Registration
- On first launch (after login), the app registers the device's FCM token with the backend via `registerDevice(token, platform)`.
- Multiple devices per user are supported.

### FR-NOTIF-02: List Invitation Notification
- When a user is added to a shared list, all their registered devices receive a push notification.
- Notification content: "[Owner name] shared [List name] with you."

### FR-NOTIF-03: Local Notifications
- The app can display local notifications (no server required) for in-app events.
- Test notification is available from the Settings screen.

---

## 8. Reports & Analytics

### FR-RPT-01: Weekly Purchase Summary
- The Reports screen shows a summary of items purchased in the current week.
- Data is sourced from the local `PurchaseHistory` SQLite table.

### FR-RPT-02: Top Items
- The Reports screen shows the most frequently purchased items across all time.
- Displayed as a ranked list with item name and purchase count.

---

## 9. Settings

### FR-SET-01: Language Selection
- The user can switch the app language between English (en-CA) and Portuguese (pt-BR).
- Changing the language updates the thread culture and reloads the app UI.

### FR-SET-02: Clear All Data
- The user can delete all local lists and items from Settings.
- A confirmation dialog is shown before deletion.
- This action clears the SQLite database (does not delete from cloud).

### FR-SET-03: Test Push Notification
- Settings includes a "Test Push" button that displays a local notification immediately.
- Used to verify notification permissions are correctly configured.

### FR-SET-04: View Logs
- Settings links to a Logs screen showing the application's debug log file.
- The log view is a dark terminal-style display.
- The user can refresh, clear, or copy the log content.

---

## 10. Offline Support

### FR-OFF-01: Full Offline Operation
- The app is fully functional without an internet connection.
- All reads and writes go to the local SQLite database first.
- Features that require the network (barcode lookup, AI parsing, list sharing) gracefully degrade with an appropriate message.

### FR-OFF-02: Authentication Offline
- If the device is offline and a valid refresh token is cached, the user is authenticated without network access.

### FR-OFF-03: Sync on Reconnect
- When the device reconnects, the app syncs dirty (changed) local records to the cloud.
- Dirty items are tracked via `IsDirty` flags on models.

---

## 11. Localization

### FR-L10N-01: Bilingual Support
- All UI strings are localized for English (en-CA) and Portuguese (pt-BR).
- String resources live in `ShopFlow/Resources/Strings/`.

### FR-L10N-02: Voice Input Language
- Voice recognition uses the app's currently selected language culture.

### FR-L10N-03: Item Name Translation (Smart Matching)
- When matching a barcode product name to existing list items, AWS Translate is used to translate the product name into both en and pt for fuzzy matching.

---

## 12. Non-Functional Requirements

### NFR-01: Platform
- iOS 15.0+ (primary, shipped via TestFlight and App Store)
- Android API 21+ (secondary, not in active CI pipeline)

### NFR-02: Performance
- App launch to list display: under 2 seconds (with cached session)
- Barcode scan to product display: under 3 seconds (network dependent)
- Voice input latency: near-real-time transcription

### NFR-03: Availability
- Backend: AWS managed services (AppSync, DynamoDB, Lambda) — target 99.9% availability
- App: offline-first, no single point of failure for core list operations

### NFR-04: Security
- Authentication: AWS Cognito SRP (passwords never transmitted in plaintext)
- API Authorization: Cognito JWT (ID token) required for all AppSync operations
- Secrets: Firebase config, signing certificates, and API keys stored as GitHub Actions secrets
- Tokens: stored in device Secure Storage (iOS Keychain)

### NFR-05: Data Privacy
- User data is scoped by `userId` in all DynamoDB queries
- Users can only access lists they own or are members of
- All API calls require authentication

### NFR-06: Scalability
- DynamoDB PAY_PER_REQUEST billing — scales to demand automatically
- Lambda functions scale independently

### NFR-07: CI/CD
- Every push to `main` triggers an automated iOS build and TestFlight upload
- Build failures block deployment

---

## 13. Out of Scope (Current Version)

- Android push notifications via FCM (compiled but not actively tested)
- `SmartSuggestions` Lambda (stub only — cloud suggestions not implemented)
- Amazon Pinpoint integration (commented out in CDK stack)
- Web application
- Payment or checkout integration
- Store location / map features
- Loyalty card management
