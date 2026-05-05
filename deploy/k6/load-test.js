import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

// Custom metrics
const apiErrors = new Rate('api_errors');
const responseTime = new Trend('response_time');

export const options = {
  stages: [
    { duration: '2m', target: 100 },  // Ramp up
    { duration: '5m', target: 100 },  // Steady state
    { duration: '2m', target: 200 },  // Ramp up more
    { duration: '5m', target: 200 },  // Steady state
    { duration: '2m', target: 0 },    // Ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'],  // 95% of requests under 500ms
    http_req_failed: ['rate<0.01'],     // Less than 1% errors
    api_errors: ['rate<0.01'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';

export default function () {
  // Health check
  let res = http.get(`${BASE_URL}/health/live`);
  check(res, {
    'health is 200': (r) => r.status === 200,
  });
  
  // API endpoints
  const endpoints = [
    '/api/v1/tasks?page=1&pageSize=20',
    '/api/v1/tasks/categories',
    '/api/v1/tasks/search?query=test',
  ];
  
  for (const endpoint of endpoints) {
    res = http.get(`${BASE_URL}${endpoint}`);
    
    check(res, {
      'status is 200': (r) => r.status === 200,
      'response time < 500ms': (r) => r.timings.duration < 500,
    });
    
    apiErrors.add(res.status !== 200);
    responseTime.add(res.timings.duration);
  }
  
  sleep(1);
}
