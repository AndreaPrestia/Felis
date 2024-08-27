'use strict';

const fs = require('fs');
const path = require('path');
const http2 = require('http2');

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
    ':method': 'GET',
    ':path': `/Test`
});

req.on('response', (headers, flags) => {
    console.debug('Response headers:', headers);
});

req.on('data', (data) => {
    try {
        const messageDeserialized = JSON.parse(data);

        if (messageDeserialized) {
            const messageFormat =
                `Received message - ${messageDeserialized.Id} with topic - ${messageDeserialized.Topic} with payload - ${messageDeserialized.Payload} with expiration - ${messageDeserialized.Expiration}`;

            try {
                console.info(messageFormat);

                const ackReq = client.request({
                    ':method': 'GET',
                    ':path': `/messages/${messageDeserialized.Id}/ack`
                });

                ackReq.on('response', (headers, flags) => {
                    console.debug('ACK Response headers:', headers);
                });
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