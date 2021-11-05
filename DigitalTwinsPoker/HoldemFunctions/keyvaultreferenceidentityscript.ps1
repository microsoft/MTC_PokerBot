$userAssignedIdentityResourceId=$(az identity show -g dtpoker -n ui-poker --query id -o tsv)
$appResourceId=$(az webapp show -g dtpoker -n dtpokerfunctions --query id -o tsv)
az rest --method PATCH --uri "${appResourceId}?api-version=2021-01-01" --body "{'properties':{'keyVaultReferenceIdentity':'${userAssignedIdentityResourceId}'}}"