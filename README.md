# ShopFlow

ShopFlow is a smart shopping assistant built with .NET MAUI (iOS/Android) and AWS Serverless Backend.

## 🚀 Getting Started

### Prerequisites
*   **Visual Studio 2022** (with .NET MAUI workload)
*   **.NET 8.0 SDK** (or compatible MAUI version)
*   **7-Zip** (Required for Windows build environment to extract iOS SDKs)
    *   Install to default location: `C:\Program Files\7-Zip\7z.exe`

### 🔑 Configuration (Important)
For security reasons, private configuration files are **excluded from source control**. You must obtain these manually to run the app.

#### 1. Firebase Cloud Messaging (Notifications)
To enable push notifications, you need to download the configuration files from the [Firebase Console](https://console.firebase.google.com/).

**Android:**
1.  Go to Project Settings -> Your Android App.
2.  Download `google-services.json`.
3.  Place it in: `ShopFlow\Platforms\Android\google-services.json`

**iOS:**
1.  Go to Project Settings -> Your iOS App.
2.  Download `GoogleService-Info.plist`.
3.  Place it in: `ShopFlow\Platforms\iOS\GoogleService-Info.plist`

*Note: The project is configured to automatically detect these files and set the correct Build Action. If they are missing, the app will build, but notifications will fail.*

#### 2. AWS Configuration
The app connects to an AWS backend. Ensure `AwsConfig.cs` (if present in code) or your environment is correctly pointed to your deployed stack.

## 📱 Build & Run
1.  Open `ShopFlow.sln` in Visual Studio.
2.  Select your target framework (`net10.0-android` or `net10.0-ios`).
3.  **Clean & Rebuild** the solution to ensure dependencies are restored.
4.  Deploy to your device or emulator.
