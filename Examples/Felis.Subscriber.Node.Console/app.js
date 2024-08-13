'use strict';

const EventSource = require('eventsource');

const sseTestUrl = 'https://localhost:7110/Test';
const sseTestAsyncUrl = 'https://localhost:7110/TestAsync';
const sseTestErrorUrl = 'https://localhost:7110/TestError';

const credentials = Buffer.from("username:password").toString('base64');

const eventSourceTest = new EventSource(sseTestUrl, {
    headers: {
        Authorization: `Basic ${credentials}`
    },
    rejectUnauthorized: false
});

eventSourceTest.onmessage = (event) => {
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

const eventSourceTestAsync = new EventSource(sseTestAsyncUrl, {
    headers: {
        Authorization: `Basic ${credentials}`
    },
    rejectUnauthorized: false
});

eventSourceTestAsync.onmessage = (event) => {
    console.debug(`Received: ${event.data}`);

    if (event.data) {
        const messageDeserialized = JSON.parse(event.data);

        if (messageDeserialized) {
            var messageFormat =
                `Received message - ${messageDeserialized.Id} with topic - ${messageDeserialized.Topic} with payload - ${messageDeserialized.Payload}`;

            (async () => {
                console.info(messageFormat);
                await sleep(1000);

            })();
        }
    }
};

const eventSourceTestError = new EventSource(sseTestErrorUrl, {
    headers: {
        Authorization: `Basic ${credentials}`
    },
    rejectUnauthorized: false
});

eventSourceTestError.onmessage = (event) => {
    console.debug(`Received: ${event.data}`);

    if (event.data) {
        const messageDeserialized = JSON.parse(event.data);

        if (messageDeserialized) {
            var messageFormat =
                `Received message - ${messageDeserialized.Id} with topic - ${messageDeserialized.Topic} with payload - ${messageDeserialized.Payload}`;

            try {
                throw new Error(messageFormat);
            }
            catch (e) {
                console.error(`Error in Felis.Subscriber.Node.Console ${e.message}`);
            }
        }
    }
};

const sleep = (ms) => {
    return new Promise(resolve => setTimeout(resolve, ms));
}