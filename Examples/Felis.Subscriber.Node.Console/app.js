'use strict';

import { join } from 'path';
import { connect } from 'http2';
import { readFileSync } from 'fs';

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
    ':method': 'GET',
    ':path': `/Test`
});

req.on('response', (headers, flags) => {
    console.debug('Response headers:', headers);
});

req.on('data', (chunk) => {
    try {
        const messageDeserialized = JSON.parse(chunk.toString());

        if (messageDeserialized) {
            var messageFormat =
                `Received message - ${messageDeserialized.Id} with topic - ${messageDeserialized.Topic} with payload - ${messageDeserialized.Payload}`;

            try {
                console.info(messageFormat);
            }
            catch (e) {
                console.error(`Error in Felis.Subscriber.Node.Console ${e.message}`);
            }
        }
    }
    catch (error) {
        console.error(`Error in Felis.Subscriber.Node.Console ${error.message}`);
    }
});

req.on('end', () => {
    console.debug('Message sent and response received.');
    client.close();
});

req.end();