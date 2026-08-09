# Mars — Open-Source Visual Programming Platform

Mars is a self-hosted, open-source platform for visual programming and content management. Build websites, automate tasks, connect APIs, manage data — all through a visual node-based editor, without writing code.

Inspired by Node-RED and WordPress, Mars combines flow-based visual programming with a flexible CMS engine, giving you full control over your projects.

## Key Features

### Visual Programming (Mars.Nodes)
- **55+ node types** for building workflows visually
- **Categories**: HTTP/API, MQTT/IoT, file I/O, C# code execution, templates, loops, events, SQL queries, email, debugging
- **Flow-based editor** with drag-and-drop, zoom/pan, and real-time debugging
- **Subflows and microscheme** for reusable logic blocks
- **No-code and low-code** — write C# when you need full power, or use visual blocks for everything else

### Flexible Content Model
- **PostTypes** — create any content type (articles, products, orders, custom entities)
- **MetaFields** — 15 field types including nested groups, lists, relations, files, images
- **Categories and taxonomies** with their own custom fields
- **Multi-language support** for content

### Data Sources
- Connect to **PostgreSQL, MsSQL, MySQL** databases
- Query and introspect remote databases visually
- Use SQL nodes to work with any connected database
- Backup and restore utilities for PostgreSQL

### Plugin System
- Extend Mars with .NET assemblies loaded at runtime
- Backend plugins (server-side logic) and frontend plugins (Blazor WebAssembly)
- Upload plugins as ZIP files through the admin panel
- Plugin marketplace-ready architecture

### Docker Integration
- Manage containers, images, and volumes from the admin panel
- Run external tools and services via Docker
- Deploy Mars itself as a Docker container

### Job Scheduler
- Built-in **Quartz.NET** scheduler
- Cron expressions, intervals, daily tasks
- Visual job management (pause, resume, trigger, delete)

### AI & Semantic Kernel
- Integration with **Semantic Kernel** for AI capabilities
- AI-powered tools for database schema introspection
- LLM functions available in visual flows

### Template Engines
- **Handlebars** and **Scriban** template engines
- Render templates from visual flows or serve as web pages
- Database-driven templates (stored and edited in the admin panel)

### Multi-Front Architecture
- Serve content as **SPA, static HTML, or template-based pages**
- Use any frontend framework (React, Vue, Angular) via API
- Mobile apps and IoT devices can connect directly

### Admin Panel
- **Blazor Server** single-page application
- Manage posts, types, users, media, plugins, navigation, settings
- Visual node editor integrated into the admin interface
- Role-based access control with ASP.NET Identity

### Observability
- **OpenTelemetry** integration for tracing and metrics
- Prometheus endpoint for monitoring
- Structured logging with configurable levels

## Use Cases

- **Websites and web apps** — CMS with visual logic
- **API backends** — build and serve REST APIs visually
- **Automation** — scheduled tasks, data processing, integrations
- **IoT and smart home** — MQTT nodes for device communication
- **Data pipelines** — connect databases, transform data, export results
- **Internal tools** — admin panels, dashboards, workflows

## Tech Stack

- **.NET 10** / ASP.NET Core
- **Blazor** (Server + WebAssembly)
- **Entity Framework Core** with PostgreSQL/MsSQL/MySQL
- **Quartz.NET** for job scheduling
- **Semantic Kernel** for AI integration
- **Docker.DotNet** for container management
- **OpenTelemetry** for observability

## Deployment

- **Docker**: `docker run` or `docker-compose`
- **Self-hosted**: Windows, Linux, macOS
- **Cloud**: AWS, Azure, Google Cloud, DigitalOcean
- **Database**: PostgreSQL (recommended), MsSQL, MySQL, SQLite (dev)

## Links

- Website: https://Mars-dotnet.org
- Documentation: https://mdimai666.github.io/Mars/
- GitHub: https://github.com/mdimai666/Mars
- NuGet: https://www.nuget.org/packages/mdimai666.Mars.Core
- Docker Hub: https://hub.docker.com/r/mdimai666/mars/

## License

MIT License — see [LICENSE](./LICENSE)
