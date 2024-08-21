'use strict';

const fs = require('fs');
const path = require('path');
const https = require('https');
const EventSource = require('eventsource');

const sseUrl = 'https://localhost:7110/subscribe?topics=Test,TestAsync,TestError';

const pfxPath = path.join(__dirname, '../Output.pfx');
const password = 'Password.1';

const agent = new https.Agent({
    pfx: fs.readFileSync(pfxPath),
    passphrase: password,
    rejectUnauthorized: false 
});

const eventSource = new EventSource(sseUrl, {
    https: { agent }
});

eventSource.onmessage = (event) => {
    console.debug(`Received: ${event.data}`);

    if (event.data) {
        const messageDeserialized = JSON.parse(event.data);

        if (messageDeserialized) {
            var messageFormat =
                `Received message - ${messageDeserialized.Id} with topic - ${messageDeserialized.Topic} with payload - ${messageDeserialized.Payload}`;

            try {
                if ("Test" === messageDeserialized.Topic) {
                    console.info(messageFormat);
                }
                else if ("TestAsync" === messageDeserialized.Topic) {
                    (async () => {
                        console.info(messageFormat);
                        await sleep(1000);

                    })();
                }
                else {
                    throw new Error(messageFormat);
                }
            }
            catch (e) {
                console.error(`Error in Felis.Subscriber.Node.Console ${e.message}`);
            }

        }
    }
};

eventSource.onerror = (err) => {
    console.error('SSE error:', err);
    // Optionally close the connection if there are errors
    //eventSource.close();
};

// Optional: handle when the connection opens
eventSource.onopen = () => {
    console.debug('SSE connection opened.');
};

const sleep = (ms) => {
    return new Promise(resolve => setTimeout(resolve, ms));
}