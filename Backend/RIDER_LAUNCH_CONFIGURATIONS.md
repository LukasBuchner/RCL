# JetBrains Rider Launch Configurations

This document describes the Rider launch configurations available for the Freydis GraphQL Server with different agent type combinations.

## Available Configurations

### 🔧 Launch Settings Profiles

These configurations use the `launchSettings.json` profiles and are the recommended way to run the application:

1. **GraphQLServer - Dummy Agents** 
   - Environment: `Development`
   - Agents: Alice, Bob, Charlie, Robot
   - URL: http://localhost:5095/graphql
   - Auto-loads dummy agents from `dummy-agents-config.json`

2. **GraphQLServer - Real Agents**
   - Environment: `Production` 
   - Agents: Real agent discovery (placeholder)
   - URL: http://localhost:5095/graphql
   - Attempts real agent discovery

3. **GraphQLServer - Dummy Agents (HTTPS)**
   - Environment: `Development`
   - Agents: Alice, Bob, Charlie, Robot
   - URL: https://localhost:7095/graphql
   - HTTPS with SSL certificate

4. **GraphQLServer - Real Agents (HTTPS)**
   - Environment: `Production`
   - Agents: Real agent discovery
   - URL: https://localhost:7095/graphql
   - HTTPS with SSL certificate

5. **GraphQLServer - Hybrid Agents**
   - Environment: `Hybrid`
   - Agents: Dummy + KUKA agents
   - URL: http://localhost:5095/graphql
   - Mixed agent pool for integration testing

6. **GraphQLServer - Hybrid Agents (Network)**
   - Environment: `Hybrid`
   - Agents: Dummy + KUKA agents
   - URL: http://0.0.0.0:5095/graphql
   - Network-accessible binding for physical device connections

7. **GraphQLServer - Hybrid Agents (HTTPS)**
   - Environment: `Hybrid`
   - Agents: Dummy + KUKA agents
   - URL: https://localhost:7095/graphql
   - HTTPS with SSL certificate and network binding

8. **GraphQLServer - KUKA Agents**
   - Environment: `Kuka`
   - Agents: KUKA iiwa 14 robots via OPC UA
   - URL: http://localhost:5095/graphql
   - Connects to OptX OPC UA server for robot communication

9. **GraphQLServer - KUKA Agents (HTTPS)**
   - Environment: `Kuka`
   - Agents: KUKA iiwa 14 robots via OPC UA
   - URL: https://localhost:7095/graphql
   - HTTPS with SSL certificate

### 🐛 Debug Configurations

For debugging with breakpoints:

5. **Debug - Dummy Agents**
   - Environment: `Development`
   - Direct .NET Core debugging
   - Full debugging capabilities
   - Working directory: Project root

6. **Debug - Real Agents**
   - Environment: `Production`
   - Direct .NET Core debugging
   - Full debugging capabilities
   - Working directory: Project root

### 📜 Legacy Configuration

7. **Development (Legacy)**
   - Original configuration for backward compatibility
   - Environment: `Development`
   - Basic setup without agent specification

## How to Use in Rider

### 1. Using Run/Debug Dropdown

1. Open JetBrains Rider
2. Look for the run/debug configuration dropdown in the toolbar
3. Select one of the configurations:
   - For development/testing: **GraphQLServer - Dummy Agents**
   - For production testing: **GraphQLServer - Real Agents**
   - For debugging: **Debug - Dummy Agents** or **Debug - Real Agents**
4. Click the ▶️ Run or 🐛 Debug button

### 2. Using Run Configurations Window

1. Go to `Run` → `Edit Configurations...`
2. You'll see all the configurations listed under `.NET Launch Settings Profile` and `.NET Project`
3. Select a configuration and click `OK`
4. Use `Run` → `Run 'Configuration Name'` or `Run` → `Debug 'Configuration Name'`

### 3. Using Keyboard Shortcuts

- **Shift + F10**: Run selected configuration
- **Shift + F9**: Debug selected configuration
- **Ctrl + Shift + F10**: Run context configuration
- **Ctrl + Shift + F9**: Debug context configuration

## Configuration Details

### Environment Variables

| Configuration | ASPNETCORE_ENVIRONMENT | Enabled Agent Types | Auto-Load |
|---|---|---|---|
| Dummy Agents | Development | Dummy | ✅ Yes |
| Real Agents | Production | Real | ✅ Yes |
| Hybrid Agents | Hybrid | Dummy + KUKA | ✅ Yes |
| Hybrid Agents (Network) | Hybrid | Dummy + KUKA | ✅ Yes |
| KUKA Agents | Kuka | KUKA | ✅ Yes |
| Debug Dummy | Development | Dummy | ✅ Yes |
| Debug Real | Production | Real | ✅ Yes |

### URLs and Ports

- **HTTP**: http://localhost:5095
- **HTTPS**: https://localhost:7095  
- **GraphQL Playground**: `/graphql`
- **Health Check**: `/health`
- **Status**: `/status`

### Working Directory

All configurations use the GraphQLServer directory as working directory to ensure:
- `dummy-agents-config.json` is found (copied to GraphQLServer directory)
- Configuration files (`appsettings.json`) are accessible
- Logging output goes to the correct location

## Testing Your Configuration

### 1. Verify Agent Loading

After starting any configuration, check the console output for:

```
[DBG] Initializing agents (Dummy: True, Kuka: False, Real: False, DigitalTwin: True)
[DBG] Loading Dummy agents from Config/dummy-agents-config.json
[INFO] Loaded dummy agent: Alice (ID: cdef1234-5678-4cde-89ab-34567890abcd)
[INFO] Loaded dummy agent: Bob (ID: def12345-6789-4def-89ab-4567890abcde)
[INFO] Loaded dummy agent: Charlie (ID: ef123456-7890-4ef0-89ab-567890abcdef)
[INFO] Loaded dummy agent: Robot (ID: f1234567-8901-4f01-89ab-67890abcdef0)
```

### 2. Test GraphQL Endpoint

Navigate to http://localhost:5095/graphql and run:

```graphql
query {
  getRuntimeAgents {
    id
    name
    isActive
    agentType
    healthStatus {
      isHealthy
      statusMessage
      activeExecutions
    }
  }
}
```

Expected result for dummy agents:
```json
{
  "data": {
    "getRuntimeAgents": [
      {
        "id": "cdef1234-5678-4cde-89ab-34567890abcd",
        "name": "Alice",
        "isActive": true,
        "agentType": "DummyRuntimeAgent",
        "healthStatus": {
          "isHealthy": true,
          "statusMessage": "Idle - ready for work",
          "activeExecutions": 0
        }
      }
      // ... more agents
    ]
  }
}
```

### 3. Verify Environment

Check that the correct environment is loaded:

```graphql
query {
  getRuntimeAgentByName(agentName: "Alice") {
    name
    isActive
    availableSkills {
      name
      description
    }
  }
}
```

## Debugging Tips

### Setting Breakpoints

1. Use **Debug - Dummy Agents** or **Debug - Real Agents** configurations
2. Set breakpoints in:
   - `AgentStartupService.cs` - Agent initialization
   - `UnifiedAgentManager.cs` - Agent management
   - `RuntimeAgentService.cs` - GraphQL queries
   - `Query.cs` - GraphQL resolvers

### Common Debug Scenarios

1. **Agent Loading Issues**
   - Breakpoint in `AgentStartupService.InitializeDummyAgentsAsync()`
   - Check `_agentsConfig` values
   - Verify file paths

2. **GraphQL Query Issues**
   - Breakpoint in `RuntimeAgentService.GetAllRuntimeAgentsAsync()`
   - Inspect `_agentManager.ActiveAgents`
   - Check agent health status

3. **Configuration Issues**
   - Breakpoint in `AgentServiceExtensions.AddAgentServices()`
   - Verify configuration binding
   - Check environment variables

## Troubleshooting

### Configuration Not Found

If Rider doesn't show the configurations:
1. Restart Rider
2. Check that files exist in `.idea/.idea.Freydis/.idea/runConfigurations/`
3. Verify XML format is correct
4. Try `File` → `Reload from Disk`

### Agent Loading Fails

1. Check working directory is set to GraphQLServer directory
2. Verify `dummy-agents-config.json` exists in GraphQLServer directory
3. Check console output for specific error messages
4. Ensure `ASPNETCORE_ENVIRONMENT` is set correctly
5. If PostgreSQL connection errors occur, verify the working directory is correct

### Port Already in Use

If port 5095 is busy:
1. Stop other instances of the application
2. Change port in `applicationUrl` in `launchSettings.json`
3. Update the configurations accordingly

### SSL Certificate Issues (HTTPS)

For HTTPS configurations, if you see certificate errors:
1. Run `dotnet dev-certs https` to generate certificate
2. Run `dotnet dev-certs https --trust` to trust certificate
3. Restart browser
4. Accept certificate warnings in development

**Note**: The development certificate has already been generated and trusted for this project.

## Creating Custom Configurations

To create additional configurations:

1. Copy an existing XML file in `runConfigurations/`
2. Modify the `name` attribute
3. Change environment variables as needed
4. Update launch profile name if using Launch Settings
5. Restart Rider to see the new configuration

Example custom configuration:
```xml
<configuration default="false" name="Custom - My Test" type="LaunchSettings" factoryName=".NET Launch Settings Profile">
  <option name="LAUNCH_PROFILE_PROJECT_FILE_PATH" value="$PROJECT_DIR$/GraphQLServer/GraphQLServer.csproj" />
  <option name="LAUNCH_PROFILE_NAME" value="GraphQLServer - Dummy Agents" />
  <envs>
    <env name="ASPNETCORE_ENVIRONMENT" value="Development" />
    <env name="CUSTOM_SETTING" value="value" />
  </envs>
</configuration>
```

## Next Steps

- Start with **GraphQLServer - Dummy Agents** for development
- Use **Debug - Dummy Agents** when you need to debug
- Switch to **Real Agents** configurations when real agent implementation is ready
- Create custom configurations for specific testing scenarios