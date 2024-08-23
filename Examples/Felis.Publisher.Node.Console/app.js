'use strict';

const http2 = require('http2');
const fs = require('fs');
const path = require('path');

const publishMessage = async (topic) => {
    const endpoint = 'https://localhost:7110';

    const pfxPath = path.join(__dirname, '../Output.pfx');
    const password = 'Password.1';

    // Create a client session
    const client = http2.connect(endpoint, {
        pfx: fs.readFileSync(pfxPath),
        passphrase: password,
        rejectUnauthorized: false
    });

    const req = client.request({
        ':method': 'POST',
        ':path': `/${topic}`,
        'Content-Type': 'application/json'
    });

    // Send the message body
    req.write(JSON.stringify({
        description: `${topic} at: ${Math.floor(new Date().getTime() / 1000)} from NodeJS publisher`
    }));

    req.on('response', (headers, flags) => {
        console.debug('Response headers:', headers);
    });

    req.setEncoding('utf8');
    req.on('data', (chunk) => {
        console.debug(`Response body: ${chunk}`);
    });

    req.on('end', () => {
        console.debug('Message sent and response received.');
        client.close();
    });

    req.end();
}

const sleep = (ms) => {
    return new Promise((resolve) => setTimeout(resolve, ms));
}

(async () => {
    try {
        console.log("Started Felis.Publisher.Node.Console");
        while (true) {
            publishMessage('Test');

            await sleep(5000);
        }
    }
    catch (e) {
        console.error(`Error in Felis.Publisher.Node.Console ${e.message}`);
    }
})();



