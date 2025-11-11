# ⚙️ XYZ HR Console – Backend API (ASP.NET Core + Azure)


A scalable and secure backend for HR management built using **ASP.NET Core MVC**, **Entity Framework Core**, and **Azure SQL Database**.
It provides REST APIs for job listings, candidate applications, HR dashboards, and AI-based shortlisting.


---


## 🌍 Live Deployment
🔗 **Backend (Azure App Service):** [[https://your-backend.azurewebsites.net]](https://hrm-app-ten.vercel.app/)(#)
🔗 **Frontend (Vercel):** [[https://your-frontend-url.vercel.app]](https://hrapp-bgerb7f5ezfjb6ar.canadacentral-01.azurewebsites.net/hr/)(#)


---


## 🧩 Features
- HR authentication and role management
- Job CRUD operations
- Candidate application API
- Resume file uploads via Azure Blob Storage
- AI keyword-based shortlisting engine
- RESTful API for React frontend integration


---


## 🛠️ Tech Stack
| Category | Technology |
|-----------|-------------|
| Framework | ASP.NET Core 8 MVC |
| Language | C# |
| Database | Azure SQL Database |
| ORM | Entity Framework Core |
| Authentication | JWT + ASP.NET Identity |
| File Storage | Azure Blob Storage |
| Deployment | Azure App Service |


---


## 🔧 Environment Configuration
In **Azure App Service → Configuration → Application Settings**, add:


```
ConnectionStrings__Default=Server=tcp:your-sql-server.database.windows.net,1433;Database=HRM;User ID=...;Password=...;
JWT__Key=[YourSecretKey]
JWT__Issuer=https://your-backend.azurewebsites.net
Blob__ConnectionString=[YourAzureBlobConnection]
Blob__ContainerName=resumes
```


---
## 🧑‍💻 Local Setup
```bash
# Clone repo
git clone https://github.com/YourUser/backend.git
cd backend


# Update appsettings.json
# Run migrations
dotnet ef database update


# Start project
dotnet run
```


Runs on: **https://localhost:7130**


---


## 🧠 AI Shortlist Scorer
Located in:
`/Services/KeywordShortlistScorer.cs`


It reads weighted keywords from configuration and scores candidates’ cover letters to assist HR in ranking applications.


---


## 🌐 API Endpoints (Samples)
| Method | Endpoint | Description |
|---------|-----------|-------------|
| GET | `/api/jobs` | List all jobs |
| POST | `/api/jobs` | Create a new job |
| GET | `/api/applications/{id}` | Get application details |
| POST | `/api/applications` | Submit new application |
| POST | `/api/account/login` | HR login |


---


## 🚀 Azure Deployment Steps
1. Open in Visual Studio → Right-click → **Publish → Azure → App Service**.
2. Create or select an existing Web App and SQL Database.
3. Add connection strings and JWT keys in App Settings.
4. Deploy and test the live API.


---


## 🧾 License
© 2025 XYZ HR Console Team. All rights reserved.
