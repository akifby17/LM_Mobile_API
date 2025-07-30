# LmMobileApi Configuration Guide

## Production Configuration

### Environment Variables

For production deployment, set the following environment variables or update the `appsettings.Production.json` file:

#### Database Configuration
bash
DB_SERVER=10.0.3.203
DB_NAME=TEKSDATA_LM_HARPUT  
DB_USER=sa
DB_PASSWORD=MC1453

#### JWT Configuration
```bash
JWT_SECRET_KEY=your-very-long-and-secure-jwt-secret-key-here
```
⚠️ **Important**: Use a strong, unique secret key (at least 256 bits/32 characters) for JWT signing.

#### External API Configuration
bash
DATAMAN_API_BASE_URL=http://127.0.0.1:5100/api/DataMan/

### Configuration File Structure

The `appsettings.json` now uses placeholder values that should be replaced in production:

json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server={DB_SERVER};Database={DB_NAME};User Id={DB_USER};Password={DB_PASSWORD};..."
  },
  "JwtSettings": {
    "SecretKey": "{JWT_SECRET_KEY}",
    "Issuer": "Teksdata",
    "Audience": "Teksdata",
    "ExpirationMinutes": 60
  },
  "DataManApiOptions": {
    "BaseUrl": "{DATAMAN_API_BASE_URL}"
  }
}

### Development vs Production

- **Development**: Uses `appsettings.Development.json` with actual values for local development
- **Production**: Uses environment variables or `appsettings.Production.json` with secure values

### Security Best Practices

1. Never commit real credentials to version control
2. Use strong, unique JWT secret keys  
3. Enable HTTPS in production
4. Consider using Azure Key Vault or similar for credential management
5. Regular rotation of secrets

## Recent Fixes Applied

✅ **Program.cs Issues Fixed:**
- Removed duplicate `UseAuthorization()` calls
- Fixed Swagger configuration to work in all environments
- Improved middleware pipeline ordering

✅ **LoomEndpoints.cs Issues Fixed:**  
- Fixed `Produces<r>()` compilation errors
- Corrected return type declarations

✅ **SignalR Issues Fixed:**
- **MAJOR FIX**: Removed incorrect singleton registration of Hub
- Fixed "Object reference not set to an instance of an object" error
- Updated `LoomCurrentlyStatusDependency` to use `IHubContext<T>` instead of direct Hub injection
- Improved async/await patterns and error handling
- Added comprehensive logging with ILogger
- Added proper disposal patterns and connection cleanup
- Added resilience features (reconnection, retry delays)

✅ **Dashboard API Issues Fixed:**
- **MAJOR FIX**: Fixed ActiveShiftPieChart returning zero values
- Fixed Dapper mapping issue by making properties public settable
- Made constructor public to allow proper instantiation
- Added comprehensive logging for stored procedure debugging
- Enhanced error handling and detailed diagnostics

✅ **Security Improvements:**
- Moved hard-coded credentials to configuration
- Created proper JWT settings structure
- Separated development and production configurations

✅ **Code Quality:**
- Fixed typos in Result.cs Match methods
- Improved code organization and consistency
- Enhanced error handling and logging throughout

## SignalR Implementation Details

### How It Works
1. **Hub Registration**: `LoomsCurrentlyStatusHub` is registered via `AddSignalR()` (no singleton needed)
2. **Dependency Injection**: `LoomCurrentlyStatusDependency` uses `IHubContext<LoomsCurrentlyStatusHub>`
3. **Real-time Updates**: SQL Server change notifications trigger SignalR messages to all connected clients
4. **Error Resilience**: Automatic reconnection and comprehensive error handling

### Client Connection
Connect to SignalR hub at: `/loomsCurrentlyStatus`
Listen for: `LoomCurrentlyStatusChanged` events

### Troubleshooting SignalR
- Check database connection string
- Ensure SQL Server Service Broker is enabled
- Monitor application logs for detailed error information
- Verify client connection to hub endpoint

## Dashboard API Details

### ActiveShiftPieChart Endpoint
- **URL**: `GET /api/dashboard/activeShiftPieChart`
- **Stored Procedure**: `dbo.tsp_GetActiveShiftPieChart`
- **Response Model**: Contains metrics like Efficiency, WeftStop, WarpStop, etc.

### Troubleshooting Dashboard APIs
- Check console logs for detailed stored procedure output
- Verify stored procedure exists and returns data
- Ensure column names match model properties (case-sensitive)
- Monitor for database connection issues 