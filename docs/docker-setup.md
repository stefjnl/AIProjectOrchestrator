# Docker Setup for AI Project Orchestrator

This document explains how to run the AI Project Orchestrator application using Docker and Docker Compose.

## Prerequisites

- Docker Engine 20.10 or higher
- Docker Compose 1.29 or higher

## Services Overview

The application consists of three services:

1. **api** - The main API service (runs on port 8086)
2. **web** - The Blazor Web UI (runs on port 8087)
3. **db** - PostgreSQL database (runs on port 5432)

## Running the Application

### Start All Services

To start all services, run:

```bash
docker-compose up -d
```

This will:
- Build the API and Web services
- Start a PostgreSQL database container
- Start the API service on port 8086
- Start the Web UI on port 8087

### Access the Application

Once the services are running:

- **API**: http://localhost:8086
- **Web UI**: http://localhost:8087
- **Database**: postgresql://localhost:5432

### View Logs

To view the logs for all services:

```bash
docker-compose logs -f
```

To view logs for a specific service:

```bash
docker-compose logs -f api
docker-compose logs -f web
docker-compose logs -f db
```

### Stop the Application

To stop all services:

```bash
docker-compose down
```

To stop all services and remove volumes (including database data):

```bash
docker-compose down -v
```

## Development Workflow

### Rebuilding Services

If you make changes to the code, rebuild the services:

```bash
docker-compose build
docker-compose up -d
```

To rebuild a specific service:

```bash
docker-compose build api
docker-compose build web
```

### Running Commands in Containers

To run commands in the running containers:

```bash
# Run a shell in the API container
docker-compose exec api /bin/bash

# Run a shell in the Web container
docker-compose exec web /bin/bash

# Run database commands in the DB container
docker-compose exec db psql -U sa -d AIProjectOrchestratorDb
```

## Environment Configuration

### API Service Environment Variables

- `ASPNETCORE_ENVIRONMENT` - Set to Development for development mode
- `ConnectionStrings__DefaultConnection` - Database connection string

### Web Service Environment Variables

- `ASPNETCORE_ENVIRONMENT` - Set to Development for development mode
- `ConnectionStrings__APIBaseUrl` - URL to the API service

### Database Service Environment Variables

- `POSTGRES_USER` - Database user (default: sa)
- `POSTGRES_PASSWORD` - Database password (default: YourStrong@Passw0rd)
- `POSTGRES_DB` - Database name (default: AIProjectOrchestratorDb)

## Network and Security

All services are connected through a dedicated Docker network called `ai-project-network`. This ensures secure communication between services while keeping them isolated from the host network.

The database is not exposed to the host network directly, only accessible through the internal Docker network.

## Data Persistence

Database data is stored in a Docker volume named `postgres_data`. This ensures that data persists even when containers are stopped or removed.

To completely remove all data, use the `-v` flag with `docker-compose down`:

```bash
docker-compose down -v
```

## Troubleshooting

### Common Issues

**Services won't start**
- Check that Docker is running
- Ensure ports 8086, 8087, and 5432 are not in use by other applications
- Check the logs for error messages

**Can't connect to the database**
- Verify the database service is running: `docker-compose ps`
- Check the connection string in the API service environment
- Ensure the database is ready before starting dependent services

**Web UI can't connect to API**
- Verify the API service is running
- Check the API base URL configuration in the Web service environment
- Ensure the services can communicate through the Docker network

### Useful Commands

```bash
# List running containers
docker-compose ps

# View resource usage
docker stats

# Restart a specific service
docker-compose restart api

# Force rebuild without using cache
docker-compose build --no-cache
```