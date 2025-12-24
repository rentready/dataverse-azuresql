# dataverse-azuresql
An Azure Function App and Azure Service Bus based solution for one-way synchronization between the Microsoft Dynamics 365 and Azure SQL.

# Dataverse AzureSQL Synchronization

One-way data replication service from Microsoft Dataverse (Dynamics 365) to Azure SQL.

## Overview

This project provides a real-time and scheduled synchronization system that replicates Dataverse entity data to Azure SQL for improved query performance and scalability. The service supports multiple synchronization modes and handles various entity operations including creates, updates, deletes, and relationship changes.

## Purpose

- **Real-time synchronization** - Process Dataverse change events via Azure Service Bus queues
- **Scheduled synchronization** - Periodic incremental syncs via cron-triggered functions
- **Manual synchronization** - On-demand full or incremental syncs via HTTP API
- **Data replication** - Maintain a Azure SQL replica of Dataverse data for high-performance queries

## Technology Stack

- **.NET 6.0** - Application framework
- **Azure Functions v4** - Serverless compute platform
- **Azure Sql** - Target database for synchronized data
- **Azure Service Bus** - Message queue for real-time event processing
- **Azure Storage Tables** - Delta link storage for incremental syncs
- **Durable Functions** - Orchestration of long-running sync processes
- **Pulumi** - Infrastructure as Code
- **Application Insights** - Monitoring and telemetry

## Architecture

The solution consists of three main components:

1. **Azure Functions App** - Processes synchronization events and orchestrates sync jobs
2. **Service Bus Queues** - One queue per entity type for real-time change events
3. **Azure Sql** - Replicated database with collections for each synchronized entity
