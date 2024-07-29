## Capturing and storing events from Mango Voice in an Azure Function

### Environment Variables
Can be set in Function App -> Settings -> Environment variables -> App Settings. When created, check Deployment Slot Setting.
- AzureDbConnectionString - Connection string to Azure DB
- X-Mango-Remote-ID - Expected Remote ID header (set in [admin.mangovoice.com](https://admin.mangovoice.com))

### Function Attributes
![image](https://github.com/user-attachments/assets/ecd7edb3-d34e-49c2-84db-5e24936138e0)

- FunctionName - Defines the name of the function in Azure
- Route - Name of the endpoint (e.g. https://{function}.azurewebsites.net/api/mango_event)
- dbo.MangoEvents - MangoEvents is the name of the table this function will write to
