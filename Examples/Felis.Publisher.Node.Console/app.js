'use strict';

const http2 = require('http2');

const publishMessage = async (message) => {
    const endpoint = 'https://localhost:7110';
    const credentials = Buffer.from("username:password").toString('base64');

    // Create a client session
    const client = http2.connect(endpoint, {
        rejectUnauthorized: false
    });

    const req = client.request({
        ':method': 'POST',
        ':path': '/publish',
        'Content-Type': 'application/json',
        'Authorization': `Basic ${credentials}`
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
                topic: 'Test',
                payload: `Test at: ${Math.floor(new Date().getTime() / 1000)} from NodeJS publisher`
            });

            await sleep(5000);

            publishMessage({
                topic: 'TestAsync',
                payload: `TestAsync at: ${Math.floor(new Date().getTime() / 1000)} from NodeJS publisher`
            });

            await sleep(5000);

            publishMessage({
                topic: 'TestError',
                payload: `TestError at: ${Math.floor(new Date().getTime() / 1000)} from NodeJS publisher`
            });

            await sleep(5000);
        }
    }
    catch (e) {
        console.error(`Error in Felis.Publisher.Node.Console ${e.message}`);
    }
})();



