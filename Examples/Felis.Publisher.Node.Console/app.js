'use strict';

import { connect } from 'http2';
import { readFileSync } from 'fs';
import { join } from 'path';

const publishMessage = async (topic) => {
    const endpoint = 'https://localhost:7110';

    const pfxPath = join(__dirname, '../Output.pfx');
    const password = 'Password.1';

    // Create a client session
    const client = connect(endpoint, {
        pfx: readFileSync(pfxPath),
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

            await sleep(600);
        }
    }
    catch (e) {
        console.error(`Error in Felis.Publisher.Node.Console ${e.message}`);
    }
})();



