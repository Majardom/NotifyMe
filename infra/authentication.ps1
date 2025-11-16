# ============================
# CONFIGURATION
# ============================
$apiAppName    = "notifymev2-api-app"
$clientAppName = "notifymev2-client"
$scopeName     = "functions.access"

Write-Host "=== Creating API App Registration ==="

# 1. Create API App Registration
$apiAppId = az ad app create `
    --display-name $apiAppName `
    --sign-in-audience "AzureADMyOrg" `
    --enable-access-token-issuance true `
    --enable-id-token-issuance true `
    --query appId -o tsv

Write-Host "API App ID: $apiAppId"


# 2. Add OAuth2 scope
Write-Host "`n=== Adding OAuth2 scope ==="

$scopeId = [guid]::NewGuid().ToString()

# Set token version
az ad app update `
  --id $apiAppId `
  --set api.requestedAccessTokenVersion=2

# Add scope
az ad app update `
  --id $apiAppId `
  --set api.oauth2PermissionScopes="[
    {
      'id': '$scopeId',
      'type': 'Admin',
      'value': '$scopeName',
      'isEnabled': true,
      'adminConsentDisplayName': 'Access Function API',
      'adminConsentDescription': 'Allows calling the Function API',
      'userConsentDisplayName': 'Access Function API',
      'userConsentDescription': 'Allows calling the Function API'
    }
  ]"

Write-Host "Scope added: api://$apiAppId/$scopeName"


# 3. Add App Roles (Manager + Member)
Write-Host "`n=== Adding App Roles ==="

$managerRoleId = [guid]::NewGuid().ToString()
$memberRoleId  = [guid]::NewGuid().ToString()

az ad app update `
  --id $apiAppId `
  --set appRoles="[
    {
      'allowedMemberTypes': ['User'],
      'displayName': 'Manager',
      'id': '$managerRoleId',
      'isEnabled': true,
      'description': 'Manager role for API',
      'value': 'Manager'
    },
    {
      'allowedMemberTypes': ['User'],
      'displayName': 'Member',
      'id': '$memberRoleId',
      'isEnabled': true,
      'description': 'Member role for API',
      'value': 'Member'
    }
  ]"

Write-Host "Added roles: Manager + Member"


# 4. Create Client App
Write-Host "`n=== Creating Client App Registration ==="

$clientAppId = az ad app create `
    --display-name $clientAppName `
    --sign-in-audience "AzureADMyOrg" `
    --public-client-redirect-uris "http://localhost" `
    --query appId -o tsv

Write-Host "Client App ID: $clientAppId"


# 5. Grant delegated API permission
Write-Host "`n=== Granting delegated permission (Client → API) ==="

az ad app permission add `
  --id $clientAppId `
  --api $apiAppId `
  --api-permissions "$scopeId=Scope"

az ad app permission grant `
  --id $clientAppId `
  --api $apiAppId

Write-Host "Permission granted."


# 6. Final output summary
Write-Host "`n============================================"
Write-Host " API App Registration"
Write-Host "   App ID:         $apiAppId"
Write-Host "   Scope ID:       $scopeId"
Write-Host "   Scope Value:    api://$apiAppId/$scopeName"
Write-Host ""
Write-Host " App Roles"
Write-Host "   Manager Role ID: $managerRoleId"
Write-Host "   Member Role ID:  $memberRoleId"
Write-Host ""
Write-Host " Client App Registration"
Write-Host "   App ID:        $clientAppId"
Write-Host "============================================`n"