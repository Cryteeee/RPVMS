# RPVMS (Residential Property Village Management System)

## Description
A modern Blazor WebAssembly application for managing residential properties and village communities.

## Technologies Used
- .NET 7.0
- Blazor WebAssembly
- Entity Framework Core
- SQL Server
- SignalR for real-time communications
- Azure Static Web Apps for hosting

## Features
- User Authentication & Authorization
- Real-time Notifications
- Property Management
- Resident Management
- Event Management
- Billing System
- Contact Management
- Admin Dashboard

## Prerequisites
- .NET 7.0 SDK
- Visual Studio 2022 or VS Code
- SQL Server (LocalDB or higher)

## Getting Started

1. Clone the repository
```bash
git clone https://github.com/Cryteeee/RPVMS.git
cd RPVMS
```

2. Install dependencies
```bash
dotnet restore
```

3. Update database connection string in `Server/appsettings.json`

4. Apply database migrations
```bash
cd Server
dotnet ef database update
```

5. Run the application
```bash
dotnet run
```

## Project Structure
- `/Client` - Blazor WebAssembly frontend
- `/Server` - ASP.NET Core backend API
- `/Shared` - Shared models and resources
- `/wwwroot` - Static web assets

## Development
- Main branch: Production-ready code
- Development branch: Active development

## Contributing
1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License
This project is licensed under the MIT License - see the LICENSE file for details.

## Contact
Your Name - [@YourTwitter](https://twitter.com/yourtwitter)
Project Link: [https://github.com/Cryteeee/RPVMS](https://github.com/Cryteeee/RPVMS) 