'use strict';
import http from 'k6/http';
import { check, sleep } from 'k6';

// Configure the test to simulate different stages of load
export let options = {
    // Configure the test to simulate different stages of load
    stages: [
        { duration: '1m', target: 100 }, // Ramp up to 100 users over 1 minute
        { duration: '5m', target: 100 }, // Maintain 100 users for 5 minutes
        { duration: '1m', target: 200 }, // Ramp up to 200 users over 1 minute
        { duration: '5m', target: 200 }, // Maintain 200 users for 5 minutes
        { duration: '1m', target: 0 },   // Ramp down to 0 users over 1 minute
    ],
    thresholds: {
        // Define thresholds for performance metrics
        'http_req_duration': ['p(95)<500'], // 95% of requests must complete below 500ms
        'http_req_failed': ['rate<0.01'], // Error rate should be less than 1%
    }
};

export default function () {
    // Define the URL for the POST request
    const url = 'https://localhost:7110/Test';
    const credentials = "dXNlcm5hbWU6cGFzc3dvcmQ=";

    // Define the payload for the POST request
    const payload = JSON.stringify({
        description: `Test at: ${Math.floor(new Date().getTime() / 1000)} from NodeJS publisher`
    });

    // Set the request headers
    const params = {
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Basic ${credentials}`
        },
    };

    // Send the POST request
    let res = http.post(url, payload, params);

    // Check if the response status is 200 or 201 (Created)
    check(res, {
        'is status 202': (r) => r.status === 202
    });

    // Add a short sleep to simulate realistic user interaction
    sleep(1);
}
