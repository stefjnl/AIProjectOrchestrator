"""
NanoGPT Flask Proxy Service
Provides a local HTTP endpoint that bypasses SSL/TLS compatibility issues
between .NET HttpClient and NanoGPT's API infrastructure.
"""

from flask import Flask, request, jsonify
from flask_cors import CORS
import requests
import logging
import json
import os
import urllib3
import ssl
from datetime import datetime

# Disable SSL warnings
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

# Create a custom session with SSL verification disabled and custom SSL context
session = requests.Session()
session.verify = False

# Create a custom SSL context that bypasses certificate verification
try:
    # Create an SSL context that doesn't verify certificates
    ssl_context = ssl.create_default_context()
    ssl_context.check_hostname = False
    ssl_context.verify_mode = ssl.CERT_NONE
    
    # Mount a custom adapter with the SSL context
    from requests.adapters import HTTPAdapter
    from urllib3.poolmanager import PoolManager
    
    class SSLAdapter(HTTPAdapter):
        def init_poolmanager(self, *args, **kwargs):
            kwargs['ssl_context'] = ssl_context
            return super().init_poolmanager(*args, **kwargs)
    
    session.mount('https://', SSLAdapter())
    logger = logging.getLogger(__name__)
    logger.info("Custom SSL adapter mounted successfully")
except Exception as e:
    logger = logging.getLogger(__name__)
    logger.warning(f"Failed to create custom SSL adapter: {e}")

app = Flask(__name__)
CORS(app)  # Enable CORS for .NET API calls

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Configuration - Using official NanoGPT API format
# Based on: https://docs.nano-gpt.com/api-reference/endpoint/chat-completion
NANOGPT_BASE_URL = os.environ.get('NANOGPT_BASE_URL', 'https://nano-gpt.com/api/v1')
NANOGPT_API_KEY = os.environ.get('NANOGPT_API_KEY', '')

# Alternative configurations to try
ALTERNATIVE_BASE_URLS = [
    'https://nano-gpt.com/api/v1',     # Official API format (primary)
    'http://nano-gpt.com/api/v1',      # HTTP fallback
    'https://api.nanogpt.com',         # Legacy format
    'http://api.nanogpt.com',          # Legacy HTTP
    'https://nanogpt.com/api/v1',      # Alternative path
    'http://nanogpt.com/api/v1',       # Alternative HTTP
]

# Validate configuration
if not NANOGPT_API_KEY:
    logger.error("NANOGPT_API_KEY environment variable not set")
    exit(1)

def create_nanogpt_headers():
    """Create standard headers for NanoGPT API calls"""
    return {
        'Authorization': f'Bearer {NANOGPT_API_KEY}',
        'Content-Type': 'application/json',
        'Accept': 'text/event-stream',
        'User-Agent': 'AI-Project-Orchestrator-Proxy/1.0'
    }

def log_request(method, url, status_code, response_time):
    """Log API request details for debugging"""
    logger.info(f"[{datetime.now().isoformat()}] {method} {url} -> {status_code} ({response_time:.3f}s)")

@app.route('/health', methods=['GET'])
def health_check():
    """Health check endpoint for monitoring proxy availability"""
    return jsonify({
        'status': 'healthy',
        'service': 'nanogpt-proxy',
        'version': '1.0',
        'nanogpt_configured': bool(NANOGPT_API_KEY),
        'timestamp': datetime.now().isoformat()
    })

@app.route('/v1/chat/completions', methods=['POST'])
def chat_completions():
    """
    Proxy endpoint that forwards chat completion requests to NanoGPT
    
    Expected .NET request format:
    {
        "model": "moonshotai/Kimi-K2-Instruct-0905",
        "messages": [
            {"role": "system", "content": "System message"},
            {"role": "user", "content": "User prompt"}
        ],
        "temperature": 0.7,
        "max_tokens": 10000,
        "stream": false
    }
    """
    # Use official NanoGPT API format: https://nano-gpt.com/api/v1/chat/completions
    # Based on official documentation
    last_error = None
    try:
        start_time = datetime.now()
        
        # Get request data from .NET client
        request_data = request.get_json()
        
        if not request_data:
            return jsonify({'error': 'No JSON data provided'}), 400
            
        # Log incoming request with size information
        request_size = len(json.dumps(request_data))
        logger.info(f"Incoming request: Model={request_data.get('model', 'unknown')}, "
                   f"Temperature={request_data.get('temperature', 'unknown')}, "
                   f"Request size: {request_size} characters")
        
        # Warn about large requests
        if request_size > 10000:  # 10KB
            logger.warning(f"Large request detected ({request_size} characters), may take longer to process")
        
        # Validate required fields
        if 'messages' not in request_data:
            return jsonify({'error': 'Missing required field: messages'}), 400
            
        # Set default model if not specified
        if 'model' not in request_data:
            request_data['model'] = 'moonshotai/Kimi-K2-Instruct-0905'
            
        # Ensure non-streaming mode for simplicity
        request_data['stream'] = False
        
        # Forward request to NanoGPT
        headers = create_nanogpt_headers()
        
        # Try different base URLs and endpoints
        response = None
        successful_config = None
        
        # Try different base URLs with correct endpoint format
        for base_url in ALTERNATIVE_BASE_URLS:
            # Build the correct endpoint based on the base URL format
            if 'nano-gpt.com/api/v1' in base_url:
                endpoint = '/chat/completions'  # Official format: /api/v1/chat/completions
            else:
                endpoint = '/v1/chat/completions'  # Legacy OpenAI format: /v1/chat/completions
            
            try:
                full_url = f"{base_url}{endpoint}"
                logger.info(f"Trying: {full_url}")
                response = session.post(
                    full_url,
                    json=request_data,
                    headers=headers,
                    timeout=300  # Increased timeout for large requests
                )
                
                if response.status_code not in [404, 405]:  # If not "Not Found" or "Method Not Allowed"
                    logger.info(f"Success! Using: {full_url} (Status: {response.status_code})")
                    successful_config = (base_url, endpoint)
                    break
                else:
                    logger.warning(f"URL {full_url} returned {response.status_code}, trying next...")
            except Exception as e:
                logger.error(f"Failed to call {base_url}{endpoint}: {str(e)}")
                continue
            
            if response and response.status_code not in [404, 405]:
                break
        
        if response is None or response.status_code in [404, 405]:
            logger.error("All NanoGPT API configurations failed")
            return jsonify({
                'error': 'All NanoGPT API configurations failed',
                'message': 'Unable to find a working API endpoint. The NanoGPT service may be down or the API format has changed.'
            }), 502
        
        # Calculate response time
        response_time = (datetime.now() - start_time).total_seconds()
        log_request('POST', '/v1/chat/completions', response.status_code, response_time)
        
        if response.status_code == 200:
            # Successfully got response from NanoGPT
            response_data = response.json()
            
            # Validate response structure
            if 'choices' not in response_data or not response_data['choices']:
                logger.error("Invalid NanoGPT response: missing choices")
                return jsonify({'error': 'Invalid response from NanoGPT API'}), 502
                
            # Log successful completion
            content_length = len(response_data['choices'][0].get('message', {}).get('content', ''))
            logger.info(f"Successful completion: {content_length} characters generated")
            
            return jsonify(response_data)
            
        elif response.status_code == 401:
            logger.error("NanoGPT authentication failed - check API key")
            return jsonify({
                'error': 'Authentication failed',
                'message': 'Invalid or expired NanoGPT API key'
            }), 401
            
        elif response.status_code == 429:
            logger.error("NanoGPT rate limit exceeded")
            return jsonify({
                'error': 'Rate limit exceeded',
                'message': 'Too many requests to NanoGPT API'
            }), 429
            
        else:
            # Other error responses
            logger.error(f"NanoGPT API error: {response.status_code} - {response.text}")
            return jsonify({
                'error': f'NanoGPT API error: {response.status_code}',
                'message': response.text[:200]  # Truncate long error messages
            }), response.status_code
            
    except requests.exceptions.Timeout:
        logger.error("Timeout calling NanoGPT API")
        return jsonify({'error': 'Request timeout', 'message': 'NanoGPT API did not respond within 60 seconds'}), 504
        
    except requests.exceptions.ConnectionError:
        logger.error("Connection error to NanoGPT API")
        return jsonify({'error': 'Connection error', 'message': 'Unable to connect to NanoGPT API'}), 503
        
    except requests.exceptions.RequestException as e:
        logger.error(f"Request exception: {str(e)}")
        return jsonify({'error': 'Request failed', 'message': str(e)}), 500
        
    except json.JSONDecodeError:
        logger.error("Invalid JSON in NanoGPT response")
        return jsonify({'error': 'Invalid response', 'message': 'NanoGPT returned invalid JSON'}), 502
        
    except Exception as e:
        logger.error(f"Unexpected error: {str(e)}")
        return jsonify({'error': 'Internal server error', 'message': 'An unexpected error occurred'}), 500

@app.route('/status', methods=['GET'])
def proxy_status():
    """
    Detailed status endpoint for debugging
    Tests actual connectivity to NanoGPT API
    """
    try:
        # Test NanoGPT connectivity with a minimal request
        headers = create_nanogpt_headers()
        test_request = {
            "model": "moonshotai/Kimi-K2-Instruct-0905",
            "messages": [{"role": "user", "content": "Hello"}],
            "max_tokens": 1,
            "temperature": 0
        }
        
        start_time = datetime.now()
        
        # Try the official API format first
        try:
            response = session.post(
                f"{NANOGPT_BASE_URL}/chat/completions",
                json=test_request,
                headers=headers,
                timeout=10
            )
            response_time = (datetime.now() - start_time).total_seconds()
        except Exception as e:
            # Fallback to legacy format
            try:
                response = session.post(
                    f"https://api.nanogpt.com/v1/chat/completions",
                    json=test_request,
                    headers=headers,
                    timeout=10
                )
                response_time = (datetime.now() - start_time).total_seconds()
            except Exception as e2:
                return jsonify({
                    'proxy_status': 'operational',
                    'nanogpt_connectivity': 'failed',
                    'error': f'Both API formats failed: {str(e)}',
                    'api_key_configured': bool(NANOGPT_API_KEY),
                    'timestamp': datetime.now().isoformat()
                })
        
        return jsonify({
            'proxy_status': 'operational',
            'nanogpt_connectivity': 'success' if response.status_code == 200 else 'failed',
            'nanogpt_status_code': response.status_code,
            'response_time_seconds': response_time,
            'api_key_configured': bool(NANOGPT_API_KEY),
            'timestamp': datetime.now().isoformat()
        })
        
    except Exception as e:
        return jsonify({
            'proxy_status': 'operational',
            'nanogpt_connectivity': 'failed',
            'error': str(e),
            'api_key_configured': bool(NANOGPT_API_KEY),
            'timestamp': datetime.now().isoformat()
        })

if __name__ == '__main__':
    logger.info("Starting NanoGPT Proxy Service...")
    logger.info(f"Target API: {NANOGPT_BASE_URL}")
    logger.info(f"API Key configured: {bool(NANOGPT_API_KEY)}")
    
    # Run Flask development server
    app.run(host='0.0.0.0', port=5000, debug=True)