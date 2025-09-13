# Dockerfile for proxy service
FROM python:3.11-slim

WORKDIR /app

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY nanogpt_proxy.py .

EXPOSE 5000

# Use gunicorn for production deployment
# Increase timeout for large requests (Requirements Analysis can be very large)
# Add worker class optimized for I/O operations
CMD ["gunicorn", "--bind", "0.0.0.0:5000", "--workers", "2", "--timeout", "300", "--keep-alive", "5", "--worker-class", "sync", "nanogpt_proxy:app"]