# Restaurant System Project

This is a comprehensive full-stack restaurant management system developed to meet all course requirements.

## Features

### Backend (ASP.NET Core 10 WebAPI)
- **Architecture**: Domain-Driven Design with Repository and Unit of Work patterns.
- **Database**: Entity Framework Core with SQLite (Normalized to 3NF).
- **Authentication**: JWT-based authentication with ASP.NET Core Identity.
  - Supports **Refresh Tokens** for persistent sessions.
  - Secure password hashing provided by Identity.
- **RESTful API**: Standardized HTTP methods (GET, POST, PUT, DELETE) and status codes.
- **Real-time Updates**: **SignalR Hub** implemented for notifying clients about new orders.
- **File Management**: Image upload service with size and type validation. Static files served via Kestrel.
- **Performance**:
  - **Pagination and Filtering** for restaurant listings.
  - **Memory Caching** (IMemoryCache) for frequently accessed data.
- **Logging**: **Serilog** configured for console and daily rolling file logs.
- **Documentation**: **Swagger/OpenAPI** documentation available at `/swagger`.
- **Testing**: xUnit and Moq used for unit testing core logic and controllers.

### Frontend (React + Vite)
- **Modern UI**: Responsive design built with React and React-Bootstrap.
- **Authentication**: Secure login/registration with automatic token refresh logic.
- **Real-time**: Integrated SignalR client for instant order notifications.
- **API Integration**: All endpoints consumed via `axios` with interceptors for auth and error handling.
- **User Experience**:
  - **React-Toastify** for interactive error and success notifications.
  - Pagination on the restaurant list.
  - Asynchronous loading states and error boundaries.
- **File Handling**: Display and download functionality for restaurant/menu images.

## Project Structure
- `RestaurantSystem.Api`: Main WebAPI project containing controllers, hubs, and services.
- `RestaurantSystem.Core`: Domain models, interfaces, and shared logic.
- `RestaurantSystem.Infrastructure`: EF Core DbContext and repository implementations.
- `RestaurantSystem.Tests`: xUnit test suite.
- `RestaurantSystem.Client`: React application.

## How to Run

### 1. Backend
```bash
cd RestaurantSystem.Api
dotnet run
```
API will start on `http://localhost:5137`.

### 2. Frontend
```bash
cd RestaurantSystem.Client
npm install
npm run dev
```
Application will be available on `http://localhost:5173`.

## Database Schema (3NF)
- **AspNetUsers**: Identity user data.
- **Restaurants**: Basic info (Name, Address, Image).
- **MenuItems**: Menu details linked to Restaurants (1:N).
- **Orders**: Order metadata linked to Users (1:N).
- **OrderItems**: Link between Orders and MenuItems (N:M relation broken into 1:N).
