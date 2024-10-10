'use strict';

const fs = require('fs');
const path = require('path');
const http2 = require('http2');

const endpoint = 'https://localhost:7110';

const pfxPath = path.join(__dirname, 'Output.pfx');
const password = 'Password.1';

const client = http2.connect(endpoint, {
    pfx: fs.readFileSync(pfxPath),
    passphrase: password,
    rejectUnauthorized: false
});

const subscribeToTopic = (id, topic, exclusive) => {
    return new Promise((resolve, reject) => {
        // Create a client session
        const req = client.request({
            ':method': 'GET',
            ':path': `/${topic}`,
            'x-exclusive': `${exclusive}`
        });

        req.on('data', (data) => {
            try {
                const messageDeserialized = JSON.parse(typeof data === 'string' ? data : data.toString());

                if (messageDeserialized) {
                    const messageFormat =
                        `Message '${messageDeserialized.Id}' for subscriber: ${id} at '${messageDeserialized.Topic}' \n\r
                        Timestamp: ${messageDeserialized.Timestamp} \n\r
                        Payload: '${messageDeserialized.Payload}' \n\r
                        Expiration: ${messageDeserialized.Expiration}`;

                    try {
                        console.info(messageFormat);
                    }
                    catch (e) {
                        console.error(`Error in Felis.Subscriber.Node.Console: '${e.message}'`);
                    }
                }
            }
            catch (error) {
                console.error(`Error in Felis.Subscriber.Node.Console: '${error.message}'`);
            }
        });

        req.on('error', (error) => {
            console.error(`Request error: '${error}'`);
            reject(error);
        });


        req.on('end', () => {
            client.close();
            resolve();
        });

        req.end();
    });
}

async function subscribeInParallel(n, topic, exclusive) {
    const subscribers = [];

    for (let i = 0; i < n; i++) {
        subscribers.push(subscribeToTopic(i + 1, `${topic}`, exclusive));
    }

    try {
        await Promise.all(subscribers);
        console.debug('All subscribers finished.');
    } catch (error) {
        console.error('Error with Http2 subscribers:', error.message);
    }
}
function closeConnection() {
    client.close();
    console.log('HTTP/2 connection closed.');
}

const runMultipleSubscriptions = async () => {
    try {
        const subscriptionGeneric = subscribeInParallel(10, "Generic", false);
        const subscriptionTTL = subscribeInParallel(20, "TTL", false);
        const subscriptionBroadcast = subscribeInParallel(20, "Broadcast", false);
        const subscriptionExclusive = subscribeInParallel(1, "Exclusive", true);

        await Promise.all([subscriptionGeneric, subscriptionTTL, subscriptionBroadcast, subscriptionExclusive]);
    }
    catch (e) {
        console.error(`Error in Felis.Subscriber.Node.Console ${e.message}`);
    }
    finally {
        closeConnection();
    }
};

runMultipleSubscriptions();