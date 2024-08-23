'use strict';

const http2 = require('http2');
const fs = require('fs');
const path = require('path');

const publishMessage = async (message) => {
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
        ':path': '/publish',
        'Content-Type': 'application/json'
    });

    // Send the message body
    req.write(JSON.stringify(message));

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
            publishMessage({
                description: `Test at: ${Math.floor(new Date().getTime() / 1000)} from NodeJS publisher`
            });

            await sleep(5000);

            publishMessage({
                description: `TestAsync at: ${Math.floor(new Date().getTime() / 1000)} from NodeJS publisher`
            });

            await sleep(5000);

            publishMessage({
                description: `TestError at: ${Math.floor(new Date().getTime() / 1000)} from NodeJS publisher`
            });

            await sleep(5000);
        }
    }
    catch (e) {
        console.error(`Error in Felis.Publisher.Node.Console ${e.message}`);
    }
})();



