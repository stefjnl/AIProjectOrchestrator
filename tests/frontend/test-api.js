const http = require('http');

// Test the API endpoint directly
const options = {
    hostname: 'localhost',
    port: 8086,
    path: '/api/projectplanning/can-create/00000000-0000-0000-0000-000000000000',
    method: 'GET',
    headers: {
        'Content-Type': 'application/json',
        'Accept': 'application/json'
    }
};

const req = http.request(options, res => {
    console.log(`Status Code: ${res.statusCode}`);
    
    let data = '';
    res.on('data', d => {
        data += d;
    });
    
    res.on('end', () => {
        console.log('Response:', data);
    });
});

req.on('error', error => {
    console.error('Error:', error);
});

req.end();