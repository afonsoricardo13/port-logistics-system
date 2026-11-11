# Dockerfile for Prolog Scheduling Service
FROM swipl:latest

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Set working directory
WORKDIR /app

# Copy Prolog files
COPY *.pl .
COPY openapi.yaml .

# Expose port (Azure will set PORT env variable)
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8080/api/scheduling/health || exit 1

# Start the Prolog server
# Note: Azure sets PORT env variable, but our Prolog server uses 5003 by default
# We'll modify the startup to use environment variable
CMD ["swipl", "-s", "scheduling_server.pl", "-g", "start_scheduling_server"]
