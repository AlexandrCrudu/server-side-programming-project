
dotnet publish -c Release -o "release-folder"

Compress-Archive -Path release-folder/* -DestinationPath release-folder/publish.zip

az deployment group create --resource-group "ssp-ass-functions_group" --template-file "main.bicep"

az functionapp deployment source config-zip --resource-group "ssp-ass-functions_group" --name "crudumyfunctionapp" --src "release-folder/publish.zip"