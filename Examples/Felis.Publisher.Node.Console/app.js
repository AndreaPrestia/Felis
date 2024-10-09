'use strict';

const http2 = require('http2');
const fs = require('fs');
const path = require('path');

const endpoint = 'https://localhost:7110';

const pfxPath = path.join(__dirname, 'Output.pfx');
const password = 'Password.1';
const pfxFile = fs.readFileSync(pfxPath);

const publishMessage = (topic, ttl, broadcast) => {
    return new Promise((resolve, reject) => {

        const client = http2.connect(endpoint, {
            pfx: pfxFile,
            passphrase: password,
            rejectUnauthorized: false
        });

        const req = client.request({
            ':method': 'POST',
            ':path': `/${topic}`,
            'Content-Type': 'application/json',
            'x-ttl': `${ttl}`,
            'x-broadcast': `${broadcast}`
        });

        req.write(JSON.stringify({
            description: `${topic} at: ${Math.floor(new Date().getTime() / 1000)} from NodeJS publisher`
        }));

        req.setEncoding('utf8');

        req.on('error', (error) => {
            console.error(`Request error: '${error}'`);
            reject(error);
        });

        req.on('end', () => {
            console.debug('Message sent and response received.');
            client.close();
            resolve();
        });

        req.end();
    });
}

async function makeParallelRequests(n, topic, ttl, broadcast) {
    const requests = [];

    for (let i = 0; i < n; i++) {
        requests.push(publishMessage(`${topic}`, ttl, broadcast));
    }

    try {
        await Promise.all(requests);
        console.debug('All publish finished.');
    } catch (error) {
        console.error('Error during requests:', error.message);
    }
}

const sleep = (ms) => {
    return new Promise((resolve) => setTimeout(resolve, ms));
}

(async () => {
    try {
        console.log("Started Felis.Publisher.Node.Console");
        while (true) {
            makeParallelRequests(20, "Generic", 0, false);
            makeParallelRequests(20, "TTL", 5, false);
            makeParallelRequests(20, "Broadcast", 0, true);
            makeParallelRequests(20, "Exclusive", 0, false);
            await sleep(5000);
        }
    }
    catch (e) {
        console.error(`Error in Felis.Publisher.Node.Console: '${e.message}'`);
    }
})();