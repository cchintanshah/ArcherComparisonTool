# Archer Environment Comparison Tool

A powerful WPF desktop application for comparing RSA Archer environments and identifying metadata differences.

## Features

- **Environment Management**: Securely store and manage multiple Archer environment configurations with DPAPI-encrypted passwords
- **Selective Metadata Collection**: Choose specific modules and metadata types to compare
- **Deep Comparison**: Recursively compare nested structures up to 10 levels deep
- **Parallel Processing**: Optimized performance with parallel comparison execution
- **Color-Coded Results**: Easy-to-read comparison results with visual status indicators
- **Excel Export**: Generate comprehensive comparison reports in Excel format

## Supported Metadata Types

- Modules
- Fields
- Values Lists (with nested values)
- Layouts & Layout Objects
- DDE Rules & Actions
- Reports
- Dashboards
- Workspaces
- iViews
- Roles
- Security Parameters
- Notifications
- Data Feeds
- Schedules

## Requirements

- .NET 8.0 Runtime
- Windows OS (for DPAPI password encryption)
- Network access to Archer environments

## Getting Started

### Building from Source

```powershell
cd ArcherComparisonTool
dotnet restore
dotnet build
```

### Running the Application

```powershell
dotnet run --project ArcherComparisonTool.WPF
```

Or build and run the executable:

```powershell
dotnet publish -c Release
cd ArcherComparisonTool.WPF\bin\Release\net8.0\publish
.\ArcherComparisonTool.WPF.exe
```

## Usage

### 1. Add Environments

1. Click **"➕ Add Environment"** in the toolbar
2. Fill in the environment details:
   - Display Name
   - URL (e.g., `https://archer.company.com`)
   - Instance Name
   - Username
   - Password (encrypted with DPAPI)
   - User Domain (optional, for domain authentication)
3. Click **Save**

### 2. Configure Collection Options

1. Select a source environment
2. Click **"⚙ Configure Collection"**
3. Select the modules you want to compare
4. Check the metadata types to include
5. Click **Save**

### 3. Compare Environments

1. Select **Source** and **Target** environments from the dropdowns
2. Click **"🔍 Compare Environments"**
3. Wait for the comparison to complete
4. Review the color-coded results:
   - **Red**: Missing in Target
   - **Orange**: Missing in Source
   - **Yellow**: Mismatch

### 4. Export Results

1. After comparison completes, click **"📊 Export to Excel"**
2. Choose a save location
3. Open the Excel file to review detailed comparison results

## Architecture

### Core Library (`ArcherComparisonTool.Core`)

- **Models**: Data structures for Archer metadata and comparison results
- **Services**:
  - `ArcherApiClient`: SOAP authentication and REST API communication
  - `MetadataCollector`: Orchestrates metadata collection with progress reporting
  - `ComparisonEngine`: Parallel comparison with recursive nested value support
  - `EnvironmentStorage`: Secure environment configuration persistence
  - `ExcelExporter`: Color-coded Excel report generation

### WPF Application (`ArcherComparisonTool.WPF`)

- **MVVM Pattern**: Clean separation of concerns
- **Modern UI**: Dark theme with Archer orange accents
- **ViewModels**:
  - `MainViewModel`: Main application orchestration
  - `EnvironmentManagerViewModel`: Environment CRUD operations
  - `CollectionOptionsViewModel`: Module and metadata type selection
- **Views**:
  - `MainWindow`: Main comparison interface
  - `EnvironmentManagerDialog`: Environment configuration
  - `CollectionOptionsDialog`: Collection options with dual-panel selection
  - `PasswordDialog`: Secure password entry

## Security

- Passwords are encrypted using Windows DPAPI (Data Protection API)
- Encrypted passwords are stored in `%APPDATA%\ArcherComparisonTool\environments.json`
- SSL certificate validation can be customized in `ArcherApiClient`

## Logging

Application logs are stored in:
```
%APPDATA%\ArcherComparisonTool\Logs\log-YYYYMMDD.txt
```

## Dependencies

- **CommunityToolkit.Mvvm**: MVVM helpers
- **EPPlus**: Excel file generation
- **Serilog**: Structured logging
- **ModernWpfUI**: Modern WPF theming
- **System.Security.Cryptography.ProtectedData**: DPAPI encryption

## Troubleshooting

### Connection Issues

- Verify the Archer URL is correct and accessible
- Check that the instance name matches your Archer configuration
- Ensure your credentials are valid

### Comparison Errors

- Verify both environments are accessible
- Check that you have permissions to view the selected metadata
- Review logs in `%APPDATA%\ArcherComparisonTool\Logs`

### Excel Export Issues

- Ensure you have write permissions to the export location
- Close any existing Excel files with the same name
- Check available disk space

## License

This tool is provided as-is for internal use. Ensure compliance with your organization's policies and RSA Archer licensing agreements.

## Support

For issues or questions, review the application logs and consult your Archer administrator.
